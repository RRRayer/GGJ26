using System;
using System.Threading.Tasks;
using Fusion;
using Photon.Voice.Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FusionSessionFlow : MonoBehaviour
{
    [Header("Session")]
    [SerializeField] private string gameScenePath = "Assets/00. Scenes/GameScene.unity";
    [SerializeField] private string deathmatchScenePath = "Assets/00. Scenes/DeathMatchGameScene.unity";
    [SerializeField] private string waitingRoomScenePath = "Assets/00. Scenes/WaitingRoom.unity";

    [Header("Voice")]
    [SerializeField] private bool ensureFusionVoiceClient = true;
    [SerializeField] private bool voiceAutoConnectAndJoin = false;
    [SerializeField] private bool voiceUseFusionAppSettings = true;
    [SerializeField] private bool voiceUseFusionAuthValues = true;

    public event Action<bool> MatchmakingStateChanged;

    public NetworkRunner Runner => runner;
    public bool IsMatchmaking => isMatchmaking;
    public bool IsHost => runner != null && runner.IsRunning && runner.IsSharedModeMasterClient;
    public bool IsInRoom => runner != null && runner.IsRunning;

    private NetworkRunner runner;
    private GameObject runnerObject;
    private bool isStarting;
    private bool isMatchmaking;
    private bool cancelRequested;
    private TaskCompletionSource<bool> shutdownCompletion;

    private void Awake()
    {
        var existingRunner = GetComponent<NetworkRunner>();
        if (existingRunner != null)
        {
            Destroy(existingRunner);
        }

        NormalizeScenePaths();
        BindExistingRunner();
        EnsureRunner();
    }

    private void BindExistingRunner()
    {
        var existing = FindFirstObjectByType<NetworkRunner>();
        if (existing == null || existing.IsRunning == false)
        {
            return;
        }

        runner = existing;
        runnerObject = existing.gameObject;
        if (runnerObject.GetComponent<NetworkSceneManagerDefault>() == null)
        {
            runnerObject.AddComponent<NetworkSceneManagerDefault>();
        }
    }

    private void NormalizeScenePaths()
    {
        if (string.IsNullOrWhiteSpace(gameScenePath) || gameScenePath.EndsWith("Game.unity", StringComparison.OrdinalIgnoreCase))
        {
            gameScenePath = "Assets/00. Scenes/GameScene.unity";
        }

        if (string.IsNullOrWhiteSpace(waitingRoomScenePath))
        {
            waitingRoomScenePath = "Assets/00. Scenes/WaitingRoom.unity";
        }

        if (string.IsNullOrWhiteSpace(deathmatchScenePath))
        {
            deathmatchScenePath = "Assets/00. Scenes/DeathMatchGameScene.unity";
        }
    }

    public async Task<bool> StartSessionAsync(string roomName, int maxPlayers)
    {
        if (isStarting)
        {
            return false;
        }

        isStarting = true;
        cancelRequested = false;
        SetMatchmakingState(true);

        if (runner != null && runner.IsRunning)
        {
            await RequestShutdownAsync();
        }

        RecreateRunner();

        var sceneManager = GetComponent<NetworkSceneManagerDefault>();
        var sceneIndex = SceneManager.GetActiveScene().buildIndex;

        var startArgs = new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = roomName,
            PlayerCount = maxPlayers,
            Scene = SceneRef.FromIndex(sceneIndex),
            SceneManager = sceneManager
        };

        var result = await runner.StartGame(startArgs);
        if (result.Ok == false)
        {
            isStarting = false;
            SetMatchmakingState(false);
            if (result.ShutdownReason != ShutdownReason.OperationCanceled)
            {
                Debug.LogError($"Fusion StartGame failed: {result.ShutdownReason}");
            }
            return false;
        }

        if (cancelRequested)
        {
            _ = runner.Shutdown();
            isStarting = false;
            SetMatchmakingState(false);
            return false;
        }

        if (IsLobbyScene() && IsHost)
        {
            var waitingSceneIndex = SceneUtility.GetBuildIndexByScenePath(waitingRoomScenePath);
            if (waitingSceneIndex < 0)
            {
                Debug.LogError($"Waiting room scene not in Build Settings: {waitingRoomScenePath}");
                return false;
            }

            _ = runner.LoadScene(SceneRef.FromIndex(waitingSceneIndex));
        }

        return true;
    }

    public async Task<bool> JoinSessionLobbyAsync()
    {
        if (isStarting)
        {
            return false;
        }

        EnsureRunner();
        if (runner == null)
        {
            return false;
        }

        // Already inside a game session: keep current state.
        if (runner.IsRunning && runner.SessionInfo.IsValid)
        {
            return false;
        }

        try
        {
            await runner.JoinSessionLobby(SessionLobby.Shared);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FusionSessionFlow] JoinSessionLobby failed: {ex.Message}");
            return false;
        }
    }

    public void StartGameScene()
    {
        if (runner == null || runner.IsRunning == false)
        {
            return;
        }

        if (runner.IsSharedModeMasterClient == false)
        {
            return;
        }

        string targetScenePath = GameModeRuntime.IsDeathmatch ? deathmatchScenePath : gameScenePath;
        var gameSceneIndex = SceneUtility.GetBuildIndexByScenePath(targetScenePath);
        if (gameSceneIndex < 0)
        {
            Debug.LogError($"Game scene not in Build Settings: {targetScenePath}");
            return;
        }

        Debug.Log($"[FusionSessionFlow] StartGameScene mode={GameModeRuntime.CurrentMode}, scene={targetScenePath}");
        _ = runner.LoadScene(SceneRef.FromIndex(gameSceneIndex));
    }

    public async void ShutdownSession()
    {
        await RequestShutdownAsync();
    }

    public void CancelMatchmaking()
    {
        if (isMatchmaking == false)
        {
            return;
        }

        cancelRequested = true;
        isStarting = false;
        if (runner != null && runner.IsRunning)
        {
            _ = RequestShutdownAsync();
        }
        else
        {
            cancelRequested = false;
        }

        SetMatchmakingState(false);
    }

    private async Task RequestShutdownAsync()
    {
        if (runner == null || runner.IsRunning == false)
        {
            return;
        }

        shutdownCompletion ??= new TaskCompletionSource<bool>();
        _ = runner.Shutdown();

        await shutdownCompletion.Task;
        shutdownCompletion = null;

        if (runnerObject != null)
        {
            Destroy(runnerObject);
            runnerObject = null;
        }

        runner = null;
    }

    private void EnsureRunner()
    {
        if (runner != null)
        {
            return;
        }

        runnerObject = new GameObject("FusionRunner");
        DontDestroyOnLoad(runnerObject);
        runner = runnerObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;
        runnerObject.AddComponent<NetworkSceneManagerDefault>();
        EnsureVoiceClient();
    }

    private void EnsureVoiceClient()
    {
        if (ensureFusionVoiceClient == false || runnerObject == null)
        {
            return;
        }

        var voiceClient = runnerObject.GetComponent<FusionVoiceClient>();
        if (voiceClient == null)
        {
            voiceClient = runnerObject.AddComponent<FusionVoiceClient>();
        }

        voiceClient.AutoConnectAndJoin = voiceAutoConnectAndJoin;
        voiceClient.UseFusionAppSettings = voiceUseFusionAppSettings;
        voiceClient.UseFusionAuthValues = voiceUseFusionAuthValues;
    }

    private void RecreateRunner()
    {
        if (runnerObject != null)
        {
            Destroy(runnerObject);
            runnerObject = null;
        }

        runner = null;
        EnsureRunner();
    }

    private void SetMatchmakingState(bool value)
    {
        if (isMatchmaking == value)
        {
            return;
        }

        isMatchmaking = value;
        MatchmakingStateChanged?.Invoke(isMatchmaking);
    }

    private bool IsLobbyScene()
    {
        var activePath = SceneManager.GetActiveScene().path;
        return string.Equals(activePath, gameScenePath, StringComparison.OrdinalIgnoreCase) == false;
    }

    public void NotifyShutdownComplete()
    {
        if (shutdownCompletion != null)
        {
            shutdownCompletion.TrySetResult(true);
        }
        isStarting = false;
        cancelRequested = false;
        SetMatchmakingState(false);
    }
}
