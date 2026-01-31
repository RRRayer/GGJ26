using System.Collections;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentGameState
    {
        get => currentGameState;
        set => currentGameState = value;
    }

    private bool hasData = false;
    public bool HasData => hasData;

    [Header("Game Managing")]
    [SerializeField] private SaveLoadSystem saveLoadSystem;
    [SerializeField] private InputReader inputReader;
    [SerializeField] private TransformAnchor playerTransformAnchor;
    [SerializeField] private Transform defaultSpawnPoint;
    [SerializeField] private float totalGameSeconds = 180f;
    [SerializeField] private PlayerStateManager playerStateManager;
    [SerializeField] private StatsManager statsManager;
    [SerializeField] private UICanvasManager uiCanvasManager;
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Broadcasting on")]
    [SerializeField] private GameStateEventChannelSO onGameStateChanged;
    [SerializeField] private VoidEventChannelSO onGameEnded;
    [SerializeField] private GameResultEventChannelSO onGameResult;
    [SerializeField] private BoolEventChannelSO groupDanceActiveEvent;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI txtTimer;
    
    private GameObject player;
    private GameState currentGameState = GameState.None;
    private float remainingSeconds;
    private bool hasEnded;
    private bool isGroupDanceActive;
    private Coroutine _autoSaveRoutine;
    private bool hasSpawned;

    [Networked] private float NetRemainingSeconds { get; set; }
    [Networked] private NetworkBool NetTimerRunning { get; set; }

    public bool IsGroupDanceActive => isGroupDanceActive;

    public GameState InitialGameState = GameState.Gameplay;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (saveLoadSystem != null)
        {
            hasData = saveLoadSystem.LoadSaveDataFromDisk();
        }

        Application.targetFrameRate = 60;
        Application.runInBackground = true;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (groupDanceActiveEvent != null)
        {
            groupDanceActiveEvent.OnEventRaised += OnGroupDanceActive;
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (groupDanceActiveEvent != null)
        {
            groupDanceActiveEvent.OnEventRaised -= OnGroupDanceActive;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == gameSceneName)
        {
            StartNewGame();
        }
        else
        {
            CleanupGame();
        }
    }

    private void StartNewGame()
    {
        if (txtTimer == null)
        {
            var timerObject = GameObject.Find("txtTimer");
            if (timerObject != null)
            {
                txtTimer = timerObject.GetComponent<TextMeshProUGUI>();
            }
            Debug.LogWarning("[GameManager] txtTimer was not assigned. Found by name. Consider using a tag.");
        }
        if (defaultSpawnPoint == null)
        {
            var spawnPointObject = GameObject.FindGameObjectWithTag("DefaultSpawnPoint");
            if (spawnPointObject != null)
            {
                defaultSpawnPoint = spawnPointObject.transform;
            }
            Debug.LogWarning("[GameManager] defaultSpawnPoint was not assigned. Found by tag. Please assign the tag 'DefaultSpawnPoint'.");
        }
        if (uiCanvasManager == null)
        {
            uiCanvasManager = FindFirstObjectByType<UICanvasManager>();
            Debug.LogWarning("[GameManager] uiCanvasManager was not assigned. Found by type.");
        }

        remainingSeconds = totalGameSeconds;
        if (hasSpawned && Object != null && Object.HasStateAuthority)
        {
            NetRemainingSeconds = totalGameSeconds;
            NetTimerRunning = false;
        }
        hasEnded = false;
        UpdateGameState(InitialGameState);

        StartCoroutine(SetupPlayerAnchorRoutine());

        if (_autoSaveRoutine != null)
        {
            StopCoroutine(_autoSaveRoutine);
        }
        _autoSaveRoutine = StartCoroutine(AutoSaveRoutine());
    }

    private void CleanupGame()
    {
        if (_autoSaveRoutine != null)
        {
            StopCoroutine(_autoSaveRoutine);
            _autoSaveRoutine = null;
        }
    }

    private IEnumerator SetupPlayerAnchorRoutine()
    {
        for (int i = 0; i < 60; i++)
        {
            if (TrySetupPlayerAnchor())
            {
                // Once player is set up, determine their role and set the UI
                if (playerStateManager != null && uiCanvasManager != null)
                {
                    if (playerStateManager.TryGetLocalPlayer(out var localState))
                    {
                        if (localState.IsSeeker)
                        {
                            uiCanvasManager.EnableSeekerCanvas();
                        }
                        else
                        {
                            uiCanvasManager.EnableHiderCanvas();
                        }
                    }
                }
                yield break; 
            }
            yield return null;
        }
    }

    private IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(30f);
            if(saveLoadSystem != null)
            {
                saveLoadSystem.SaveDataToDisk();
            }
        }
    }

    private void Update()
    {
        if (hasEnded || currentGameState != GameState.Gameplay)
        {
            return;
        }

        if (hasSpawned == false)
        {
            remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);
        }
        else
        {
            remainingSeconds = NetRemainingSeconds;
        }

        if (txtTimer != null)
        {
            int minutes = Mathf.FloorToInt(remainingSeconds / 60f);
            int seconds = Mathf.FloorToInt(remainingSeconds % 60f);
            txtTimer.text = $"{minutes:00}:{seconds:00}";
        }
        if (remainingSeconds <= 0f)
        {
            if (txtTimer != null) txtTimer.text = "00:00";
            EndGame(seekerWin: false);
            return;
        }

        if (playerStateManager == null)
        {
            return;
        }

        // Condition for single-player game end (player dies)
        if (playerStateManager.GetTotalPlayerCount() == 1 && playerStateManager.GetAlivePlayerCount() == 0)
        {
            Debug.Log("[GameManager] Single-player game over: The player has died.");
            EndGame(seekerWin: true);
            return;
        }

        // Win Condition 1: All non-seekers are eliminated. Seekers win.
        if (playerStateManager.AreAllNonSeekersDead())
        {
            Debug.Log("[GameManager] Condition 1 Met: All Non-Seekers Dead. Seekers Win.");
            EndGame(seekerWin: true);
            return;
        }
        
        // Win Condition 2: Only one player remains in a multiplayer match.
        if (playerStateManager.GetTotalPlayerCount() > 1 && playerStateManager.GetAlivePlayerCount() <= 1)
        {
            Debug.Log("[GameManager] Condition 2 Met: Only one player remains.");
            var lastPlayer = playerStateManager.GetLastAlivePlayer();
            if (lastPlayer != null)
            {
                Debug.Log($"[GameManager] Last player alive is {(lastPlayer.IsSeeker ? "Seeker" : "Non-Seeker")}.");
                EndGame(seekerWin: lastPlayer.IsSeeker);
            }
            else // 0 players are alive
            {
                Debug.Log("[GameManager] 0 players alive. Checking AreAllNonSeekersDead for winner.");
                EndGame(seekerWin: playerStateManager.AreAllNonSeekersDead());
            }
            return;
        }
    }

    public void UpdateGameState(GameState newGameState)
    {
        if (newGameState == CurrentGameState)
        {
            return;
        }

        currentGameState = newGameState;

        switch (currentGameState)
        {
            case GameState.Gameplay:
                if (inputReader != null) inputReader.EnableGameplayInput();
                Time.timeScale = 1;
                break;
            case GameState.Menu:
                if (inputReader != null) inputReader.EnableUIInput();
                Time.timeScale = 1;
                break;
            case GameState.Pause:
                if (inputReader != null) inputReader.EnableUIInput();
                Time.timeScale = 0;
                break;
            case GameState.CutScene:
                if (inputReader != null) inputReader.DisableAllInput();
                break;
            case GameState.Ending:
                if (inputReader != null) inputReader.DisableAllInput();
                Time.timeScale = 1;
                break;
        }

        onGameStateChanged?.RaiseEvent(currentGameState);
    }

    private void OnApplicationPause(bool state)
    {
        if (state)
        {
            if(saveLoadSystem != null) saveLoadSystem.SaveDataToDisk();
        }
    }

    private bool TrySetupPlayerAnchor()
    {
        if (playerTransformAnchor == null)
        {
            return false;
        }

        var players = GameObject.FindGameObjectsWithTag("Player");
        if (players == null || players.Length == 0)
        {
            return false;
        }

        player = FindLocalPlayer(players);
        if (player == null && players.Length > 0)
        {
            player = players[0];
        }

        if (player == null)
        {
            return false;
        }

        if (ShouldUseDefaultSpawn() && hasData == false && defaultSpawnPoint != null)
        {
            player.transform.position = defaultSpawnPoint.position;
        }

        playerTransformAnchor.Provide(player.transform);
        return true;
    }

    private void EndGame(bool seekerWin)
    {
        if (hasEnded)
        {
            return;
        }

        hasEnded = true;
        
        if (_autoSaveRoutine != null)
        {
            StopCoroutine(_autoSaveRoutine);
            _autoSaveRoutine = null;
        }
        
        UpdateGameState(GameState.Ending);

        bool localWin = seekerWin;
        if (playerStateManager != null && playerStateManager.TryGetLocalPlayer(out var localState))
        {
            localWin = localState.IsSeeker == seekerWin;
        }

        float avgReaction = statsManager != null ? statsManager.GetAverageReactionMs() : 0f;
        List<MaskColor> history = statsManager != null ? statsManager.GetMaskHistory() : new List<MaskColor>();
        var result = new GameResultData(seekerWin, localWin, remainingSeconds, avgReaction, history);

        onGameResult?.RaiseEvent(result);
        Debug.Log("Game Ended. Seeker Win: " + seekerWin + ", Local Player Win: " + localWin);
        onGameEnded?.RaiseEvent();
    }

    public override void Spawned()
    {
        hasSpawned = true;
        if (Object != null && Object.HasStateAuthority)
        {
            NetRemainingSeconds = totalGameSeconds;
            NetTimerRunning = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || Object.HasStateAuthority == false)
        {
            return;
        }

        if (hasEnded || currentGameState != GameState.Gameplay)
        {
            return;
        }

        if (NetTimerRunning == false)
        {
            NetTimerRunning = AreAllPlayersPresent();
            if (NetTimerRunning == false)
            {
                NetRemainingSeconds = totalGameSeconds;
                return;
            }
        }

        NetRemainingSeconds = Mathf.Max(0f, NetRemainingSeconds - Runner.DeltaTime);
    }

    private bool AreAllPlayersPresent()
    {
        if (Runner == null || Runner.IsRunning == false)
        {
            return false;
        }

        int maxPlayers = Runner.SessionInfo.IsValid ? Runner.SessionInfo.MaxPlayers : 0;
        if (maxPlayers <= 1)
        {
            return true;
        }

        int activeCount = 0;
        foreach (var player in Runner.ActivePlayers)
        {
            activeCount++;
        }

        return activeCount >= maxPlayers;
    }

    private static GameObject FindLocalPlayer(GameObject[] players)
    {
        foreach (var candidate in players)
        {
            if (candidate == null)
            {
                continue;
            }

            var networkObject = candidate.GetComponent<NetworkObject>();
            if (networkObject != null && networkObject.HasInputAuthority)
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool ShouldUseDefaultSpawn()
    {
        var runner = FindFirstObjectByType<NetworkRunner>();
        return runner == null || runner.IsRunning == false;
    }

    private void OnGroupDanceActive(bool isActive)
    {
        isGroupDanceActive = isActive;
    }
}

public enum GameState
{
    None,
    Gameplay,
    Pause,
    Menu,
    CutScene,
    Ending
}
