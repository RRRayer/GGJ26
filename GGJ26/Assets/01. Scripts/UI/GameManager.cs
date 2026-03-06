using System.Collections;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentGameState
    {
        get => gameStateController != null ? gameStateController.CurrentState : currentGameState;
        set => UpdateGameState(value);
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
    [SerializeField] private GameStateController gameStateController;
    [SerializeField] private GameTimerController timerController;
    [SerializeField] private GameResultController resultController;
    [SerializeField] private DeathmatchMatchController deathmatchController;

    [Header("Broadcasting on")]
    [SerializeField] private GameStateEventChannelSO onGameStateChanged;
    [SerializeField] private VoidEventChannelSO onGameEnded;
    [SerializeField] private GameResultEventChannelSO onGameResult;
    [SerializeField] private BoolEventChannelSO groupDanceActiveEvent;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI txtTimer;

    private GameObject player;
    private GameState currentGameState = GameState.None;
    private bool isGroupDanceActive;
    private Coroutine _autoSaveRoutine;
    private bool hasSpawned;

    public bool IsGroupDanceActive => isGroupDanceActive;

    public GameState InitialGameState = GameState.Gameplay;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (saveLoadSystem != null)
        {
            hasData = saveLoadSystem.LoadSaveDataFromDisk();
        }

        if (gameStateController == null)
        {
            gameStateController = GetComponent<GameStateController>();
        }

        if (gameStateController == null)
        {
            gameStateController = gameObject.AddComponent<GameStateController>();
        }

        if (timerController == null)
        {
            timerController = GetComponent<GameTimerController>();
        }

        if (timerController == null)
        {
            timerController = gameObject.AddComponent<GameTimerController>();
        }

        if (resultController == null)
        {
            resultController = GetComponent<GameResultController>();
        }

        if (resultController == null)
        {
            resultController = gameObject.AddComponent<GameResultController>();
        }

        if (deathmatchController == null)
        {
            deathmatchController = GetComponent<DeathmatchMatchController>();
        }

        if (gameStateController != null)
        {
            gameStateController.Configure(inputReader, onGameStateChanged);
        }

        if (timerController != null)
        {
            timerController.Configure(totalGameSeconds, txtTimer);
        }

        if (resultController != null)
        {
            resultController.Configure(statsManager, playerStateManager, gameStateController, onGameResult, onGameEnded);
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
        if (playerStateManager != null)
        {
            playerStateManager.OnPlayerStateChanged += OnPlayerStateChanged;
            playerStateManager.OnAllNonSeekersDead += HandleAllNonSeekersDead;
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (groupDanceActiveEvent != null)
        {
            groupDanceActiveEvent.OnEventRaised -= OnGroupDanceActive;
        }
        if (playerStateManager != null)
        {
            playerStateManager.OnPlayerStateChanged -= OnPlayerStateChanged;
            playerStateManager.OnAllNonSeekersDead -= HandleAllNonSeekersDead;
        }
    }

    private void OnPlayerStateChanged(PlayerState updatedState)
    {
        Debug.Log($"[GameManager] OnPlayerStateChanged received for PlayerId: {updatedState.PlayerId}, IsSeeker: {updatedState.IsSeeker}.");

        if (playerStateManager.TryGetLocalPlayer(out var localState))
        {
            Debug.Log($"[GameManager] Local player is known. Local ID: {localState.PlayerId}. Comparing with updated ID: {updatedState.PlayerId}.");
            if (updatedState.PlayerId == localState.PlayerId)
            {
                if (updatedState.IsDead)
                {
                    Debug.Log($"[GameManager] Local player death state received. IsSeeker={updatedState.IsSeeker}");
                    if (updatedState.IsSeeker == false)
                    {
                        var deadUI = FindFirstObjectByType<UIDead>();
                        if (deadUI != null)
                        {
                            deadUI.ShowDeadUI();
                        }
                    }

                    return;
                }

                if (uiCanvasManager != null)
                {
                    Debug.Log($"[GameManager] Confirmed local player state change. Role is now Seeker = {updatedState.IsSeeker}. Updating UI.");
                    if (updatedState.IsSeeker)
                    {
                        Debug.Log("enableseeker");
                        uiCanvasManager.EnableSeekerCanvas();
                    }
                    else
                    {
                        Debug.Log("enablehider");
                        uiCanvasManager.EnableHiderCanvas();
                    }
                }
                else
                {
                    Debug.LogWarning("[GameManager] uiCanvasManager is null. Cannot update UI.");
                }
            }
            else
            {
                Debug.Log($"[GameManager] State change was for another player ({updatedState.PlayerId}). Ignoring for UI purposes.");
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] Could not get local player from PlayerStateManager. UI will not be updated at this time. This is expected if the local player has not been fully registered yet.");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsGameplayScene(scene))
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

        if (timerController != null)
        {
            timerController.InitializeTimerText();
            timerController.ResetTimer(hasSpawned);
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

        LockCursorForGameplay();
        if (resultController != null)
        {
            resultController.ResetResult();
        }
        UpdateGameState(InitialGameState);

        StartCoroutine(SetupPlayerAnchorRoutine());

        if (_autoSaveRoutine != null)
        {
            StopCoroutine(_autoSaveRoutine);
        }
        _autoSaveRoutine = StartCoroutine(AutoSaveRoutine());
    }

    private void LockCursorForGameplay()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        var inputs = FindFirstObjectByType<StarterAssets.StarterAssetsInputs>();
        if (inputs != null)
        {
            inputs.ForceCursorLocked();
        }
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
            if (saveLoadSystem != null)
            {
                saveLoadSystem.SaveDataToDisk();
            }
        }
    }

    private void Update()
    {
        if (gameStateController != null && gameStateController.IsEnding())
        {
            UnlockCursorForResult();
            return;
        }

        if (resultController != null && resultController.HasEnded)
        {
            return;
        }

        if (gameStateController != null && gameStateController.IsGameplay() == false)
        {
            return;
        }

        if (timerController != null)
        {
            timerController.IsGameplayActive = gameStateController == null || gameStateController.IsGameplay();
            timerController.HasEnded = resultController != null && resultController.HasEnded;
            timerController.TickTimerUI();
        }

        if (GameModeRuntime.IsDeathmatch)
        {
            return;
        }

        if (playerStateManager == null)
        {
            playerStateManager = FindFirstObjectByType<PlayerStateManager>();
            if (playerStateManager == null)
            {
                return;
            }
        }

        if (resultController != null && timerController != null)
        {
            resultController.TryResolveWinConditions(timerController.RemainingSeconds);
        }
    }

    public void UpdateGameState(GameState newGameState)
    {
        if (gameStateController != null)
        {
            gameStateController.SetState(newGameState);
        }
    }

    private void OnApplicationPause(bool state)
    {
        if (state)
        {
            if (saveLoadSystem != null) saveLoadSystem.SaveDataToDisk();
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
        if (Object != null && Object.HasStateAuthority)
        {
            float remainingSeconds = timerController != null ? timerController.RemainingSeconds : 0f;
            RpcEndGame(seekerWin, remainingSeconds);
            return;
        }

        EndGameLocal(seekerWin, timerController != null ? timerController.RemainingSeconds : 0f);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcEndGame(bool seekerWin, float remaining)
    {
        EndGameLocal(seekerWin, remaining);
    }

    private void EndGameLocal(bool seekerWin, float remaining)
    {
        if (_autoSaveRoutine != null)
        {
            StopCoroutine(_autoSaveRoutine);
            _autoSaveRoutine = null;
        }

        if (resultController != null)
        {
            resultController.EndGame(seekerWin, remaining);
        }

        UnlockCursorForResult();
    }

    private void HandleAllNonSeekersDead()
    {
        if (IsAuthoritativeForGameEnd() == false)
        {
            return;
        }

        if (resultController != null && resultController.HasEnded)
        {
            return;
        }

        if (gameStateController != null && gameStateController.IsGameplay() == false)
        {
            return;
        }
        Debug.Log("[GameManager] Event: All Non-Seekers Dead. Seekers Win.");
        EndGame(seekerWin: true);
    }

    private bool IsAuthoritativeForGameEnd()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            return true;
        }

        if (Runner != null && Runner.IsRunning && Runner.IsSharedModeMasterClient)
        {
            return true;
        }

        return false;
    }

    private void UnlockCursorForResult()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var spectator = FindFirstObjectByType<SpectatorController>();
        if (spectator != null)
        {
            spectator.enabled = false;
        }

        var inputs = FindFirstObjectByType<StarterAssets.StarterAssetsInputs>();
        if (inputs != null)
        {
            inputs.ForceCursorUnlocked();
        }
    }

    public override void Spawned()
    {
        hasSpawned = true;
        if (timerController != null)
        {
            timerController.ResetTimer(true);
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Timer is handled by GameTimerController.
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

    private bool IsGameplayScene(Scene scene)
    {
        if (scene.name == gameSceneName)
        {
            return true;
        }

        string path = scene.path;
        if (path.EndsWith("GameScene.unity", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (path.EndsWith("DeathMatchGameScene.unity", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return path.EndsWith("Game.unity", System.StringComparison.OrdinalIgnoreCase);
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
