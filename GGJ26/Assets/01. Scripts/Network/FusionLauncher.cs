using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using Photon.Voice.Fusion;
using Photon.Realtime;
using Photon.Voice.Unity;
using StarterAssets;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class FusionLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    private static FusionLauncher instance;
    [Header("Session")]
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private int minPlayersToStart = 1;
    [SerializeField] private bool autoStartOnAwake = false;
    [SerializeField] private FusionSessionFlow sessionFlow;

    [Header("Player")]
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private NetworkObject seekerPrefab;
    [SerializeField] private NetworkObject normalPrefab;
    [SerializeField] private float fallbackSpawnRadius = 4f;
    [SerializeField] private PlayerStateManager playerStateManager;

    [Header("Spawn Area")]
    [SerializeField] private string spawnAreaTag = "SpawnArea";
    [SerializeField] private LayerMask spawnGroundLayers = -1;
    [SerializeField] private float minSpawnDistance = 1.5f;
    [SerializeField] private int spawnSeed = 1337;
    [SerializeField] private bool useGridSampling = true;
    [Range(0f, 0.45f)]
    [SerializeField] private float gridJitter = 0.35f;
    [SerializeField] private bool preferSpawnPoints = true;
    [SerializeField] private int requiredSpawnPoints = 31;
    [SerializeField] private float spawnPointWaitSeconds = 2f;

    [Header("NPC Spawning")]
    [SerializeField] private NetworkObject redNpcPrefab;
    [SerializeField] private NetworkObject blueNpcPrefab;
    [SerializeField] private NetworkObject greenNpcPrefab;
    [SerializeField] private int npcsPerColor = 9;

    [Header("Services")]
    [SerializeField] private FusionSpawnService spawnService;
    [SerializeField] private FusionRoleAssignmentService roleService;
    [SerializeField] private FusionInputBridge inputBridge;

    [Header("Voice")]
    [SerializeField] private bool enableVoiceInGameSceneOnly = true;
    [SerializeField] private bool disconnectVoiceOutsideGameScene = true;
    [SerializeField] private float proximityMinDistance = 2f;
    [SerializeField] private float proximityMaxDistance = 18f;
    [SerializeField] private AudioMixerGroup voiceOutputMixerGroup;
    [Range(0f, 2f)]
    [SerializeField] private float voiceSourceVolume = 1f;

    public event Action<bool> MatchmakingStateChanged;
    public event Action<IReadOnlyList<SessionInfo>> SessionListUpdated;
    public bool IsMatchmaking => sessionFlow != null && sessionFlow.IsMatchmaking;
    public int MaxPlayers => maxPlayers;
    public int MinPlayersToStart => minPlayersToStart;
    public bool IsHost => sessionFlow != null && sessionFlow.IsHost;
    public bool IsInRoom => sessionFlow != null && sessionFlow.IsInRoom;
    public IReadOnlyList<SessionInfo> CachedSessionList => cachedSessionList;

    private NetworkRunner runner;
    private bool callbacksRegistered;
    private string lastScenePath;
    private readonly List<SessionInfo> cachedSessionList = new List<SessionInfo>();
    private float nextVoiceProfileApplyTime;
    private bool warnedMissingVoiceAppId;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("[FusionLauncher] Duplicate instance detected. Destroying this object.");
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (sessionFlow == null)
        {
            sessionFlow = GetComponent<FusionSessionFlow>();
        }

        if (sessionFlow == null)
        {
            sessionFlow = gameObject.AddComponent<FusionSessionFlow>();
        }

        if (spawnService == null)
        {
            spawnService = GetComponent<FusionSpawnService>();
        }

        if (spawnService == null)
        {
            spawnService = gameObject.AddComponent<FusionSpawnService>();
        }

        if (roleService == null)
        {
            roleService = GetComponent<FusionRoleAssignmentService>();
        }

        if (roleService == null)
        {
            roleService = gameObject.AddComponent<FusionRoleAssignmentService>();
        }

        if (inputBridge == null)
        {
            inputBridge = GetComponent<FusionInputBridge>();
        }

        if (inputBridge == null)
        {
            inputBridge = gameObject.AddComponent<FusionInputBridge>();
        }

        sessionFlow.MatchmakingStateChanged += ForwardMatchmakingStateChanged;
        runner = sessionFlow.Runner;

        if (playerStateManager == null)
        {
            playerStateManager = FindFirstObjectByType<PlayerStateManager>();
        }

        ConfigureServices();
    }

    private void OnEnable()
    {
        TryBindRunner();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (sessionFlow != null)
        {
            sessionFlow.MatchmakingStateChanged -= ForwardMatchmakingStateChanged;
        }
    }

    private void OnValidate()
    {
        ConfigureServices();
    }

    private void ForwardMatchmakingStateChanged(bool isMatchmaking)
    {
        MatchmakingStateChanged?.Invoke(isMatchmaking);
    }

    private void Start()
    {
        if (autoStartOnAwake)
        {
            StartMatchmaking("debug", 1);
        }

        TryBindRunner();
    }

    private void Update()
    {
        TryBindRunner();

        if (spawnService != null)
        {
            spawnService.TickUpdate(IsGameScene());
        }

        if (enableVoiceInGameSceneOnly && IsGameScene() && Time.unscaledTime >= nextVoiceProfileApplyTime)
        {
            ApplyProximityProfileToSpeakers();
            nextVoiceProfileApplyTime = Time.unscaledTime + 1f;
        }
    }

    public async void StartMatchmaking(string roomName, int maxPlayers, string mode = null)
    {
        if (sessionFlow == null)
        {
            return;
        }

        string resolvedMode = string.IsNullOrWhiteSpace(mode) ? RoomSessionNameCodec.DecodeMode(roomName) : mode;
        GameModeRuntime.SetMode(resolvedMode);
        Debug.Log($"[FusionLauncher] StartMatchmaking request: room='{roomName}', maxPlayers={maxPlayers}");

        this.maxPlayers = maxPlayers;
        if (spawnService != null)
        {
            spawnService.SetMaxPlayers(maxPlayers);
        }

        bool ok = await sessionFlow.StartSessionAsync(roomName, maxPlayers);
        if (ok == false)
        {
            Debug.LogWarning($"[FusionLauncher] StartMatchmaking failed: room='{roomName}'");
            return;
        }

        runner = sessionFlow.Runner;
        if (runner != null)
        {
            RegisterCallbacks(runner);
        }
    }

    private void ConfigureServices()
    {
        if (spawnService != null)
        {
            spawnService.Configure(
                playerPrefab,
                seekerPrefab,
                normalPrefab,
                fallbackSpawnRadius,
                spawnAreaTag,
                spawnGroundLayers,
                minSpawnDistance,
                spawnSeed,
                useGridSampling,
                gridJitter,
                preferSpawnPoints,
                requiredSpawnPoints,
                spawnPointWaitSeconds,
                redNpcPrefab,
                blueNpcPrefab,
                greenNpcPrefab,
                npcsPerColor,
                maxPlayers,
                minPlayersToStart);
            spawnService.BindRoleService(roleService, playerStateManager);
        }
    }

    private void TryBindRunner()
    {
        if (sessionFlow != null && sessionFlow.Runner != null)
        {
            runner = sessionFlow.Runner;
            RegisterCallbacks(runner);
            if (runner.IsRunning && spawnService != null)
            {
                spawnService.BindRunner(runner);
            }
            if (runner.IsRunning)
            {
                return;
            }
        }

        var existing = FindFirstObjectByType<NetworkRunner>();
        if (existing != null)
        {
            runner = existing;
            RegisterCallbacks(runner);
            if (existing.IsRunning && spawnService != null)
            {
                spawnService.BindRunner(existing);
            }
        }
    }

    private void RegisterCallbacks(NetworkRunner targetRunner)
    {
        if (targetRunner == null || callbacksRegistered)
        {
            return;
        }

        targetRunner.AddCallbacks(this);
        callbacksRegistered = true;
    }

    private bool IsGameScene()
    {
        var activePath = SceneManager.GetActiveScene().path;
        if (activePath.EndsWith("GameScene.unity", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (activePath.EndsWith("DeathMatchGameScene.unity", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return activePath.EndsWith("Game.unity", StringComparison.OrdinalIgnoreCase);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (spawnService != null)
        {
            spawnService.OnPlayerJoined(player);
        }

        if (roleService != null)
        {
            roleService.RegisterPlayerState(runner, player, playerStateManager);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (spawnService != null)
        {
            spawnService.OnPlayerLeft(player);
        }
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        var activePath = SceneManager.GetActiveScene().path;
        if (string.Equals(activePath, lastScenePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        lastScenePath = activePath;
        if (runner != null && runner.SessionInfo.IsValid)
        {
            GameModeRuntime.SetMode(RoomSessionNameCodec.DecodeMode(runner.SessionInfo.Name));
        }

        bool isGameScene = IsGameScene();
        ApplyVoiceModeForScene(isGameScene);
        nextVoiceProfileApplyTime = 0f;
        if (spawnService != null)
        {
            spawnService.OnSceneLoadDone(isGameScene);
        }
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        lastScenePath = null;
        if (spawnService != null)
        {
            spawnService.ResetForScene();
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (inputBridge == null)
        {
            return;
        }

        inputBridge.BuildInput(runner, input);
    }

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        cachedSessionList.Clear();
        if (sessionList != null)
        {
            cachedSessionList.AddRange(sessionList);
        }

        Debug.Log($"[FusionLauncher] OnSessionListUpdated: {cachedSessionList.Count} sessions");
        SessionListUpdated?.Invoke(cachedSessionList);
    }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        ForceDisconnectVoice();
        callbacksRegistered = false;
        lastScenePath = null;
        if (spawnService != null)
        {
            spawnService.ResetForScene();
        }
        if (sessionFlow != null)
        {
            sessionFlow.NotifyShutdownComplete();
        }
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void CancelMatchmaking()
    {
        if (sessionFlow == null)
        {
            return;
        }

        sessionFlow.CancelMatchmaking();
    }

    public void ShutdownRunner()
    {
        if (sessionFlow != null)
        {
            sessionFlow.ShutdownSession();
        }
    }

    public void StartGameScene()
    {
        if (sessionFlow != null)
        {
            sessionFlow.StartGameScene();
        }
    }

    public async void RequestSessionListRefresh()
    {
        if (sessionFlow == null)
        {
            return;
        }

        TryBindRunner();
        Debug.Log("[FusionLauncher] Requesting session lobby join for public room list.");
        bool joined = await sessionFlow.JoinSessionLobbyAsync();
        TryBindRunner();

        if (joined == false)
        {
            Debug.Log("[FusionLauncher] Session lobby join skipped or failed. Using cached session list.");
        }
    }

    private void ApplyVoiceModeForScene(bool isGameScene)
    {
        if (enableVoiceInGameSceneOnly == false)
        {
            return;
        }

        var voiceClient = FindFirstObjectByType<FusionVoiceClient>();
        if (voiceClient == null)
        {
            return;
        }

        if (isGameScene)
        {
            if (HasVoiceAppIdConfigured(voiceClient) == false)
            {
                if (warnedMissingVoiceAppId == false)
                {
                    warnedMissingVoiceAppId = true;
                    Debug.LogError("[FusionLauncher] Voice AppId is missing. Set AppIdVoice in Fusion PhotonAppSettings.");
                }

                return;
            }

            warnedMissingVoiceAppId = false;
            if (voiceClient.ClientState == Photon.Realtime.ClientState.Disconnected || voiceClient.ClientState == Photon.Realtime.ClientState.PeerCreated)
            {
                voiceClient.ConnectAndJoinRoom();
            }

            ApplyProximityProfileToSpeakers();
            return;
        }

        if (disconnectVoiceOutsideGameScene)
        {
            if (voiceClient.ClientState != Photon.Realtime.ClientState.Disconnected && voiceClient.ClientState != Photon.Realtime.ClientState.Disconnecting)
            {
                voiceClient.Disconnect();
            }
        }
    }

    private void ForceDisconnectVoice()
    {
        var voiceClient = FindFirstObjectByType<FusionVoiceClient>();
        if (voiceClient != null)
        {
            if (voiceClient.ClientState != Photon.Realtime.ClientState.Disconnected && voiceClient.ClientState != Photon.Realtime.ClientState.Disconnecting)
            {
                voiceClient.Disconnect();
            }
        }
    }

    private bool HasVoiceAppIdConfigured(FusionVoiceClient voiceClient)
    {
        if (voiceClient != null && voiceClient.Settings != null && string.IsNullOrWhiteSpace(voiceClient.Settings.AppIdVoice) == false)
        {
            return true;
        }

        if (Fusion.Photon.Realtime.PhotonAppSettings.TryGetGlobal(out var settings) && settings != null)
        {
            return string.IsNullOrWhiteSpace(settings.AppSettings.AppIdVoice) == false;
        }

        return false;
    }

    private void ApplyProximityProfileToSpeakers()
    {
        var speakers = FindObjectsByType<Speaker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < speakers.Length; i++)
        {
            var speaker = speakers[i];
            if (speaker == null)
            {
                continue;
            }

            var source = speaker.GetComponent<AudioSource>();
            if (source == null)
            {
                source = speaker.gameObject.AddComponent<AudioSource>();
            }

            if (voiceOutputMixerGroup != null)
            {
                source.outputAudioMixerGroup = voiceOutputMixerGroup;
            }

            source.volume = voiceSourceVolume;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = proximityMinDistance;
            source.maxDistance = proximityMaxDistance;
        }
    }
}
