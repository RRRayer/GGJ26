using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FusionLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Session")]
    [SerializeField] private string sessionName = "GGJ26";
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private string gameScenePath = "Assets/00. Scenes/Game.unity";
    [SerializeField] private bool autoStartOnAwake = false;

    [Header("Player")]
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private NetworkObject seekerPrefab;
    [SerializeField] private NetworkObject normalPrefab;
    [SerializeField] private float fallbackSpawnRadius = 4f;
    [SerializeField] private PlayerStateManager playerStateManager;

    private readonly Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();
    private readonly List<NetworkSpawnPoint> spawnPoints = new List<NetworkSpawnPoint>();

    public event Action<bool> MatchmakingStateChanged;
    public bool IsMatchmaking => isMatchmaking;

    private NetworkRunner runner;
    private GameObject runnerObject;
    private bool isStarting = false;
    private bool isMatchmaking = false;
    private bool cancelRequested = false;
    private TaskCompletionSource<bool> shutdownCompletion;

    private void Awake()
    {
        var existingRunner = GetComponent<NetworkRunner>();
        if (existingRunner != null)
        {
            Destroy(existingRunner);
        }

        EnsureRunner();

        if (playerStateManager == null)
        {
            playerStateManager = FindFirstObjectByType<PlayerStateManager>();
        }
    }

    private void Start()
    {
        if (autoStartOnAwake)
        {
            StartMatchmaking();
        }
    }

    public async void StartMatchmaking()
    {
        if (isStarting)
        {
            return;
        }

        await StartMatchmakingAsync();
    }

    private async Task StartMatchmakingAsync()
    {
        isStarting = true;
        cancelRequested = false;
        SetMatchmakingState(true);

        if (runner != null && runner.IsRunning)
        {
            await RequestShutdownAsync();
        }

        RecreateRunner();

        runner.AddCallbacks(this);
        if (runnerObject != null)
        {
            runner.MakeDontDestroyOnLoad(runnerObject);
        }
        else
        {
            runner.MakeDontDestroyOnLoad(gameObject);
        }

        var sceneManager = GetComponent<NetworkSceneManagerDefault>();
        var sceneIndex = SceneManager.GetActiveScene().buildIndex;

        var startArgs = new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = sessionName,
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
            return;
        }

        if (cancelRequested)
        {
            runner.Shutdown();
            isStarting = false;
            SetMatchmakingState(false);
            return;
        }
    }

    private bool IsGameScene()
    {
        var activePath = SceneManager.GetActiveScene().path;
        return string.Equals(activePath, gameScenePath, System.StringComparison.OrdinalIgnoreCase);
    }

    private bool IsLobbyScene()
    {
        return !IsGameScene();
    }

    private void TryLoadGameSceneIfReady()
    {
        if (runner == null || runner.IsRunning == false)
        {
            return;
        }

        if (runner.IsSharedModeMasterClient == false)
        {
            return;
        }

        if (runner.ActivePlayers.Count() < maxPlayers)
        {
            return;
        }

        SetMatchmakingState(false);

        var gameSceneIndex = SceneUtility.GetBuildIndexByScenePath(gameScenePath);
        if (gameSceneIndex < 0)
        {
            Debug.LogError($"Game scene not in Build Settings: {gameScenePath}");
            return;
        }

        runner.LoadScene(SceneRef.FromIndex(gameSceneIndex));
    }

    private void RefreshSpawnPoints()
    {
        spawnPoints.Clear();
        var found = UnityEngine.Object.FindObjectsByType<NetworkSpawnPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (found == null)
        {
            return;
        }

        spawnPoints.AddRange(found);
    }

    private Vector3 GetSpawnPosition(PlayerRef player)
    {
        if (spawnPoints.Count > 0)
        {
            var index = Mathf.Abs(player.RawEncoded) % spawnPoints.Count;
            return spawnPoints[index].transform.position;
        }

        var angle = (Mathf.Abs(player.RawEncoded) % maxPlayers) / (float)maxPlayers * Mathf.PI * 2f;
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * fallbackSpawnRadius;
    }

    private void TrySpawnLocalPlayer(PlayerRef player)
    {
        var prefab = GetPrefabForPlayer(player);
        if (prefab == null)
        {
            Debug.LogError("Player prefab not set on FusionLauncher.");
            return;
        }

        if (runner == null || runner.IsRunning == false)
        {
            return;
        }

        if (player != runner.LocalPlayer)
        {
            return;
        }

        if (spawnedPlayers.ContainsKey(player))
        {
            return;
        }

        var spawnPos = GetSpawnPosition(player);
        var obj = runner.Spawn(prefab, spawnPos, Quaternion.identity, player);
        if (obj != null && obj.InputAuthority != player)
        {
            obj.AssignInputAuthority(player);
        }
        if (obj != null)
        {
            runner.SetPlayerObject(player, obj);
        }
        spawnedPlayers[player] = obj;

        RegisterPlayerState(player);
    }

    private NetworkObject GetPrefabForPlayer(PlayerRef player)
    {
        if (runner == null)
        {
            return playerPrefab;
        }

        var seeker = GetDeterministicSeeker();
        bool isSeeker = player == seeker;

        if (isSeeker && seekerPrefab != null)
        {
            return seekerPrefab;
        }

        if (isSeeker == false && normalPrefab != null)
        {
            return normalPrefab;
        }

        return playerPrefab;
    }

    private PlayerRef GetDeterministicSeeker()
    {
        PlayerRef chosen = default;
        bool hasValue = false;
        foreach (var player in runner.ActivePlayers)
        {
            if (hasValue == false || player.RawEncoded < chosen.RawEncoded)
            {
                chosen = player;
                hasValue = true;
            }
        }

        if (hasValue == false)
        {
            return runner.LocalPlayer;
        }

        return chosen;
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        RegisterPlayerState(player);

        if (IsLobbyScene())
        {
            TryLoadGameSceneIfReady();
            return;
        }

        TrySpawnLocalPlayer(player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (spawnedPlayers.TryGetValue(player, out var obj))
        {
            if (obj != null)
            {
                runner.Despawn(obj);
            }
            spawnedPlayers.Remove(player);
        }
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (IsGameScene())
        {
            SetMatchmakingState(false);
            RefreshSpawnPoints();
            TrySpawnLocalPlayer(runner.LocalPlayer);
        }
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (runner == null)
        {
            return;
        }

        if (runner.TryGetPlayerObject(runner.LocalPlayer, out var playerObject) == false || playerObject == null)
        {
            playerObject = FindLocalPlayerObject(runner);
            if (playerObject == null)
            {
                return;
            }
        }

        var starterInputs = playerObject.GetComponent<StarterAssets.StarterAssetsInputs>();
        if (starterInputs == null)
        {
            return;
        }

        PlayerInputData data = default;
        data.Move = starterInputs.move;
        data.Look = starterInputs.look;
        data.Jump = starterInputs.jump;
        data.Sprint = starterInputs.sprint;
        input.Set(data);

        // Consume one-shot inputs so they don't latch across ticks.
        starterInputs.jump = false;
    }

    private NetworkObject FindLocalPlayerObject(NetworkRunner runner)
    {
        var objects = UnityEngine.Object.FindObjectsByType<NetworkObject>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        if (objects == null)
        {
            return null;
        }

        foreach (var obj in objects)
        {
            if (obj != null && obj.InputAuthority == runner.LocalPlayer)
            {
                return obj;
            }
        }

        return null;
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        isStarting = false;
        cancelRequested = false;
        SetMatchmakingState(false);
        if (shutdownCompletion != null)
        {
            shutdownCompletion.TrySetResult(true);
        }
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
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
        runner.Shutdown();

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

    private void RegisterPlayerState(PlayerRef player)
    {
        if (playerStateManager == null)
        {
            return;
        }

        string playerId = player.RawEncoded.ToString();
        playerStateManager.RegisterPlayer(playerId, false);

        if (runner != null && player == runner.LocalPlayer)
        {
            playerStateManager.SetLocalPlayer(playerId);
        }
    }
}
