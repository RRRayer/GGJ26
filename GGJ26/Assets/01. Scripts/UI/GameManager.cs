using System.Collections;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
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

    public bool IsGroupDanceActive => isGroupDanceActive;

    public GameState InitialGameState = GameState.Gameplay;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        if (saveLoadSystem != null)
        {
            hasData = saveLoadSystem.LoadSaveDataFromDisk();
        }

        Application.targetFrameRate = 60;
    }

    private void OnEnable()
    {
        if (groupDanceActiveEvent != null)
        {
            groupDanceActiveEvent.OnEventRaised += OnGroupDanceActive;
        }
    }

    private void OnDisable()
    {
        if (groupDanceActiveEvent != null)
        {
            groupDanceActiveEvent.OnEventRaised -= OnGroupDanceActive;
        }
    }

    private IEnumerator Start()
    {
        remainingSeconds = totalGameSeconds;
        hasEnded = false;
        UpdateGameState(InitialGameState);

        for (int i = 0; i < 60; i++)
        {
            if (TrySetupPlayerAnchor())
            {
                break;
            }
            yield return null;
        }

        while (true)
        {
            yield return new WaitForSeconds(30f);
            saveLoadSystem.SaveDataToDisk();
        }
    }

    private void Update()
    {
        if (hasEnded || currentGameState != GameState.Gameplay)
        {
            return;
        }

        remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);
        if (txtTimer != null)
        {
            int minutes = Mathf.FloorToInt(remainingSeconds / 60f);
            int seconds = Mathf.FloorToInt(remainingSeconds % 60f);
            txtTimer.text = $"{minutes:00}:{seconds:00}";
        }
        if (remainingSeconds <= 0f)
        {
            txtTimer.text = "00:00";
            EndGame(seekerWin: false);
            return;
        }

        if (playerStateManager == null)
        {
            return;
        }

        // Win Condition 1: All non-seekers are eliminated. Seekers win.
        if (playerStateManager.AreAllNonSeekersDead())
        {
            EndGame(seekerWin: true);
            return;
        }
        
        // Win Condition 2: Only one player remains in a multiplayer match.
        if (playerStateManager.GetTotalPlayerCount() > 1 && playerStateManager.GetAlivePlayerCount() <= 1)
        {
            var lastPlayer = playerStateManager.GetLastAlivePlayer();
            if (lastPlayer != null)
            {
                // If last player is seeker, seekers win. Otherwise non-seekers win.
                EndGame(seekerWin: lastPlayer.IsSeeker);
            }
            else // 0 players are alive
            {
                // If everyone is dead, check if the non-seekers were wiped out.
                EndGame(seekerWin: playerStateManager.AreAllNonSeekersDead());
            }
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
                inputReader.EnableGameplayInput();
                Time.timeScale = 1;
                break;
            case GameState.Menu:
                inputReader.EnableUIInput();
                Time.timeScale = 1;
                break;
            case GameState.Pause:
                inputReader.EnableUIInput();
                Time.timeScale = 0;
                break;
            case GameState.CutScene:
                inputReader.DisableAllInput();
                break;
            case GameState.Ending:
                inputReader.DisableAllInput();
                Time.timeScale = 1;
                break;
        }

        onGameStateChanged?.RaiseEvent(currentGameState);
    }

    private void OnApplicationPause(bool state)
    {
        if (state)
        {
            saveLoadSystem.SaveDataToDisk();
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
        if (player == null)
        {
            player = players[0];
        }

        if (player == null)
        {
            return false;
        }

        if (ShouldUseDefaultSpawn() && hasData == false)
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
