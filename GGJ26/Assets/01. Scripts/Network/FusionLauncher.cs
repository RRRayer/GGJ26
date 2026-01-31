using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class FusionLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Session")]
    // [SerializeField] private string sessionName = "GGJ26";
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private string gameScenePath = "Assets/00. Scenes/Game.unity";
    [SerializeField] private bool autoStartOnAwake = false;

    [Header("Player")]
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private NetworkObject seekerPrefab;
    [SerializeField] private NetworkObject normalPrefab;
    [SerializeField] private float fallbackSpawnRadius = 4f;
    [SerializeField] private PlayerStateManager playerStateManager;
    [Header("Spawn Area")]
    [SerializeField] private BoxCollider spawnArea;
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

    private readonly Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();
    private readonly List<NetworkSpawnPoint> spawnPoints = new List<NetworkSpawnPoint>();
    private readonly Dictionary<PlayerRef, Vector3> playerSpawnPositions = new Dictionary<PlayerRef, Vector3>();
    private readonly List<NetworkObject> spawnedNpcs = new List<NetworkObject>();
    private bool spawnLayoutBuilt;
    private bool hasSpawnBounds;
    private Vector3 spawnBoundsCenter;
    private Vector3 spawnBoundsSize;
    private bool pendingLocalSpawn;

    public event Action<bool> MatchmakingStateChanged;
    public bool IsMatchmaking => isMatchmaking;
    public int MaxPlayers => maxPlayers;

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
            StartMatchmaking("debug", 1);
        }
    }

    public async void StartMatchmaking(string roomName, int maxPlayers)
    {
        if (isStarting)
        {
            return;
        }

        await StartMatchmakingAsync(roomName, maxPlayers);
    }

    private async Task StartMatchmakingAsync(string _roomName, int _maxPlayers)
    {
        this.maxPlayers = _maxPlayers;
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
        DontDestroyOnLoad(gameObject);

        var sceneManager = GetComponent<NetworkSceneManagerDefault>();
        var sceneIndex = SceneManager.GetActiveScene().buildIndex;

        var startArgs = new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = _roomName,
            PlayerCount = _maxPlayers,
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
        if (spawnLayoutBuilt == false)
        {
            BuildSpawnLayout();
        }

        if (playerSpawnPositions.TryGetValue(player, out var position))
        {
            return position;
        }

        var angle = (Mathf.Abs(player.RawEncoded) % maxPlayers) / (float)maxPlayers * Mathf.PI * 2f;
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * fallbackSpawnRadius;
    }

    private void TrySpawnLocalPlayer(PlayerRef player)
    {
        if (spawnLayoutBuilt == false)
        {
            if (preferSpawnPoints && spawnPoints.Count < requiredSpawnPoints)
            {
                pendingLocalSpawn = true;
                return;
            }
        }

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

        // Update the PlayerStateManager with the correct role
        if (playerStateManager != null)
        {
            var seeker = GetDeterministicSeeker();
            bool isSeeker = player == seeker;
            playerStateManager.RegisterPlayerNetworked(player.RawEncoded.ToString(), isSeeker);
            Debug.Log($"[FusionLauncher] TrySpawnLocalPlayer: Player {player.RawEncoded} determined as seeker: {isSeeker}.");
        }
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

        if (spawnLayoutBuilt == false && preferSpawnPoints && spawnPoints.Count < requiredSpawnPoints)
        {
            pendingLocalSpawn = player == runner.LocalPlayer;
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
        if (this == null || gameObject == null || isActiveAndEnabled == false)
        {
            return;
        }

        if (IsGameScene())
        {
            ResetSpawnLayoutForScene();
            SetMatchmakingState(false);
            StartCoroutine(WaitForSpawnPointsAndBuild());
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

        var playerInput = playerObject.GetComponent<PlayerInput>();

        PlayerInputData data = default;
        data.Move = starterInputs.move;
        data.Look = starterInputs.look;
        data.Jump = starterInputs.jump;
        data.Sprint = starterInputs.sprint;

        data.danceIndex = -1;
        if (playerInput != null && playerInput.actions != null)
        {
            if (IsDancePressed(playerInput, "Dance1")) data.danceIndex = 0;
            else if (IsDancePressed(playerInput, "Dance2")) data.danceIndex = 1;
            else if (IsDancePressed(playerInput, "Dance3")) data.danceIndex = 2;
            else if (IsDancePressed(playerInput, "Dance4")) data.danceIndex = 3;
            if (data.danceIndex == -1 && Keyboard.current != null && Keyboard.current.fKey.isPressed)
            {
                data.danceIndex = 4;
            }
        }
        else
        {
            if (starterInputs.dance1) data.danceIndex = 0;
            else if (starterInputs.dance2) data.danceIndex = 1;
            else if (starterInputs.dance3) data.danceIndex = 2;
            else if (starterInputs.dance4) data.danceIndex = 3;
            else if (Keyboard.current != null && Keyboard.current.fKey.isPressed) data.danceIndex = 4;
        }

        input.Set(data);

        // Consume one-shot inputs so they don't latch across ticks.
        starterInputs.jump = false;
    }

    private static bool IsDancePressed(PlayerInput playerInput, string actionName)
    {
        if (playerInput == null || playerInput.actions == null)
        {
            return false;
        }

        var action = playerInput.actions[actionName];
        return action != null && action.IsPressed();
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

    public async void ShutdownRunner()
    {
        await RequestShutdownAsync();
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
        playerStateManager.RegisterPlayerNetworked(playerId, false);

        if (runner != null && player == runner.LocalPlayer)
        {
            playerStateManager.SetLocalPlayer(playerId);
        }
    }

    private void BuildSpawnLayout()
    {
        if (runner == null || runner.IsRunning == false)
        {
            return;
        }

        ResolveSpawnArea();
        if (preferSpawnPoints && spawnPoints.Count < requiredSpawnPoints)
        {
            Debug.LogWarning($"[FusionLauncher] SpawnPoints not ready ({spawnPoints.Count}/{requiredSpawnPoints}). Delaying spawn layout.", this);
            return;
        }
        if (spawnArea == null && hasSpawnBounds == false && (preferSpawnPoints == false || spawnPoints.Count == 0))
        {
            Debug.LogWarning("[FusionLauncher] SpawnArea not found. Using fallback radius.", this);
        }

        int playerCount = runner.ActivePlayers.Count();
        int npcCount = Mathf.Max(0, npcsPerColor) * 3;
        int totalCount = playerCount + npcCount;
        if (totalCount <= 0)
        {
            return;
        }

        var random = new System.Random(GetSpawnSeed());
        var positions = new List<Vector3>(totalCount);
        if (preferSpawnPoints && spawnPoints.Count > 0)
        {
            positions.AddRange(GetSpawnPointPositions(totalCount, random));
        }
        else if ((spawnArea != null || hasSpawnBounds) && useGridSampling)
        {
            positions.AddRange(GenerateGridPositions(totalCount, random));
        }
        else
        {
            int attempts = 0;
            int maxAttempts = totalCount * 200;
            float minDistSqr = minSpawnDistance * minSpawnDistance;

            while (positions.Count < totalCount && attempts < maxAttempts)
            {
                attempts++;
                Vector3 candidate = SampleSpawnPosition(random);
                bool ok = true;
                for (int i = 0; i < positions.Count; i++)
                {
                    if ((positions[i] - candidate).sqrMagnitude < minDistSqr)
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok)
                {
                    positions.Add(candidate);
                }
            }
        }

        if (positions.Count < totalCount)
        {
            Debug.LogWarning($"[FusionLauncher] Spawn positions 부족: {positions.Count}/{totalCount}. 최소 거리 완화로 채웁니다.");
            FillMissingPositions(positions, totalCount, random);
        }

        playerSpawnPositions.Clear();
        var players = runner.ActivePlayers.OrderBy(p => p.RawEncoded).ToList();
        int index = 0;
        for (int i = 0; i < players.Count && index < positions.Count; i++)
        {
            playerSpawnPositions[players[i]] = positions[index++];
        }

        if (runner.IsSharedModeMasterClient)
        {
            SpawnNpcs(positions, index);
        }

        spawnLayoutBuilt = true;
        if (spawnArea != null)
        {
            Debug.Log($"[FusionLauncher] SpawnArea bounds size={spawnArea.bounds.size} center={spawnArea.bounds.center}", this);
        }
        else if (hasSpawnBounds)
        {
            Debug.Log($"[FusionLauncher] SpawnBounds size={spawnBoundsSize} center={spawnBoundsCenter}", this);
        }
        Debug.Log($"[FusionLauncher] Spawn layout built: total={totalCount} players={playerCount} npcs={npcCount} positions={positions.Count}", this);
    }

    private void SpawnNpcs(List<Vector3> positions, int startIndex)
    {
        if (spawnedNpcs.Count > 0)
        {
            return;
        }

        int totalNpc = Mathf.Max(0, npcsPerColor) * 3;
        if (totalNpc <= 0)
        {
            return;
        }

        NetworkObject[] prefabs = { redNpcPrefab, blueNpcPrefab, greenNpcPrefab };
        int index = startIndex;
        for (int color = 0; color < prefabs.Length; color++)
        {
            var prefab = prefabs[color];
            if (prefab == null)
            {
                continue;
            }

            for (int i = 0; i < npcsPerColor && index < positions.Count; i++)
            {
                var npc = runner.Spawn(prefab, positions[index++], Quaternion.identity);
                if (npc != null)
                {
                    spawnedNpcs.Add(npc);
                }
            }
        }

        Debug.Log($"[FusionLauncher] SpawnNpcs done: spawned={spawnedNpcs.Count}/{totalNpc}", this);
    }

    private Vector3 SampleSpawnPosition(System.Random random)
    {
        if (spawnArea != null)
        {
            var bounds = spawnArea.bounds;
            float x = (float)(bounds.min.x + (bounds.size.x * random.NextDouble()));
            float z = (float)(bounds.min.z + (bounds.size.z * random.NextDouble()));
            float y = bounds.max.y + 5f;
            Vector3 origin = new Vector3(x, y, z);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1000f, spawnGroundLayers, QueryTriggerInteraction.Ignore))
            {
                return hit.point;
            }

            return new Vector3(x, bounds.center.y, z);
        }

        if (hasSpawnBounds)
        {
            var bounds = new Bounds(spawnBoundsCenter, spawnBoundsSize);
            float x = (float)(bounds.min.x + (bounds.size.x * random.NextDouble()));
            float z = (float)(bounds.min.z + (bounds.size.z * random.NextDouble()));
            float y = bounds.max.y + 5f;
            Vector3 origin = new Vector3(x, y, z);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1000f, spawnGroundLayers, QueryTriggerInteraction.Ignore))
            {
                return hit.point;
            }

            return new Vector3(x, bounds.center.y, z);
        }

        float angle = (float)random.NextDouble() * Mathf.PI * 2f;
        float radius = (float)random.NextDouble() * fallbackSpawnRadius;
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
    }

    private List<Vector3> GenerateGridPositions(int count, System.Random random)
    {
        var results = new List<Vector3>(count);
        if ((spawnArea == null && hasSpawnBounds == false) || count <= 0)
        {
            return results;
        }

        var bounds = spawnArea != null
            ? spawnArea.bounds
            : new Bounds(spawnBoundsCenter, spawnBoundsSize);
        float sizeX = bounds.size.x;
        float sizeZ = bounds.size.z;
        float aspect = sizeX / Mathf.Max(0.001f, sizeZ);
        int cellsX = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(count * aspect)));
        int cellsZ = Mathf.Max(1, Mathf.CeilToInt((float)count / cellsX));

        float cellSizeX = sizeX / cellsX;
        float cellSizeZ = sizeZ / cellsZ;
        float jitterX = cellSizeX * Mathf.Clamp01(gridJitter);
        float jitterZ = cellSizeZ * Mathf.Clamp01(gridJitter);

        var candidates = new List<Vector3>(cellsX * cellsZ);
        for (int z = 0; z < cellsZ; z++)
        {
            for (int x = 0; x < cellsX; x++)
            {
                float cx = bounds.min.x + (x + 0.5f) * cellSizeX;
                float cz = bounds.min.z + (z + 0.5f) * cellSizeZ;
                float jx = (float)(random.NextDouble() * 2 - 1) * jitterX;
                float jz = (float)(random.NextDouble() * 2 - 1) * jitterZ;
                Vector3 origin = new Vector3(cx + jx, bounds.max.y + 5f, cz + jz);
                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1000f, spawnGroundLayers, QueryTriggerInteraction.Ignore))
                {
                    candidates.Add(hit.point);
                }
                else
                {
                    candidates.Add(new Vector3(cx + jx, bounds.center.y, cz + jz));
                }
            }
        }

        // Shuffle candidates
        for (int i = 0; i < candidates.Count; i++)
        {
            int swap = random.Next(i, candidates.Count);
            var temp = candidates[i];
            candidates[i] = candidates[swap];
            candidates[swap] = temp;
        }

        for (int i = 0; i < candidates.Count && results.Count < count; i++)
        {
            results.Add(candidates[i]);
        }

        return results;
    }

    private void ResolveSpawnArea()
    {
        if (spawnArea != null)
        {
            return;
        }

        if (preferSpawnPoints && spawnPoints.Count > 0)
        {
            return;
        }

        GameObject spawnObject = null;
        if (string.IsNullOrWhiteSpace(spawnAreaTag) == false)
        {
            spawnObject = GameObject.FindGameObjectWithTag(spawnAreaTag);
        }

        if (spawnObject == null)
        {
            spawnObject = GameObject.FindGameObjectWithTag("SpawnArea");
        }

        if (spawnObject == null)
        {
            try
            {
                spawnObject = GameObject.FindGameObjectWithTag("SpawnPoint");
            }
            catch (UnityException)
            {
                // Tag doesn't exist, ignore.
            }
        }

        if (spawnObject == null)
        {
            spawnObject = GameObject.Find("SpawnArea");
        }

        if (spawnObject == null)
        {
            return;
        }

        spawnArea = spawnObject.GetComponent<BoxCollider>();

        if (spawnArea == null)
        {
            BuildBoundsFromSpawnPoints();
        }
    }

    private void BuildBoundsFromSpawnPoints()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            return;
        }

        Bounds bounds = new Bounds(spawnPoints[0].transform.position, Vector3.zero);
        for (int i = 1; i < spawnPoints.Count; i++)
        {
            bounds.Encapsulate(spawnPoints[i].transform.position);
        }

        bounds.Expand(Mathf.Max(1f, minSpawnDistance));
        spawnBoundsCenter = bounds.center;
        spawnBoundsSize = bounds.size;
        hasSpawnBounds = true;
    }

    private System.Collections.IEnumerator WaitForSpawnPointsAndBuild()
    {
        float startTime = Time.time;
        int lastCount = -1;

        while (Time.time - startTime < spawnPointWaitSeconds)
        {
            RefreshSpawnPoints();
            int count = spawnPoints.Count;
            if (count != lastCount)
            {
                Debug.Log($"[FusionLauncher] Waiting spawn points: {count}/{requiredSpawnPoints}", this);
                lastCount = count;
            }

            if (preferSpawnPoints == false || count >= requiredSpawnPoints)
            {
                break;
            }

            yield return null;
        }

        RefreshSpawnPoints();
        Debug.Log($"[FusionLauncher] Spawn points ready: {spawnPoints.Count}/{requiredSpawnPoints}", this);
        if (preferSpawnPoints && spawnPoints.Count < requiredSpawnPoints)
        {
            Debug.LogWarning("[FusionLauncher] Spawn points not ready. Skipping spawn this pass.", this);
            yield break;
        }

        // Let transforms settle for a couple of frames before sampling positions.
        yield return null;
        yield return null;

        RefreshSpawnPoints();
        ResolveSpawnArea();
        BuildSpawnLayout();
        if (runner != null && runner.IsRunning)
        {
            if (pendingLocalSpawn && spawnLayoutBuilt)
            {
                pendingLocalSpawn = false;
            }
            TrySpawnLocalPlayer(runner.LocalPlayer);
        }
    }


    private void ResetSpawnLayoutForScene()
    {
        spawnLayoutBuilt = false;
        hasSpawnBounds = false;
        spawnArea = null;
        playerSpawnPositions.Clear();
        spawnedNpcs.Clear();
    }

    private void FillMissingPositions(List<Vector3> positions, int totalCount, System.Random random)
    {
        if (positions.Count >= totalCount)
        {
            return;
        }

        int attempts = 0;
        int maxAttempts = (totalCount - positions.Count) * 50;
        float relaxedMinDist = Mathf.Max(0.25f, minSpawnDistance * 0.25f);
        float minDistSqr = relaxedMinDist * relaxedMinDist;

        while (positions.Count < totalCount && attempts < maxAttempts)
        {
            attempts++;
            Vector3 candidate = SampleSpawnPosition(random);
            bool ok = true;
            for (int i = 0; i < positions.Count; i++)
            {
                if ((positions[i] - candidate).sqrMagnitude < minDistSqr)
                {
                    ok = false;
                    break;
                }
            }

            if (ok)
            {
                positions.Add(candidate);
            }
        }
    }

    private List<Vector3> GetSpawnPointPositions(int count, System.Random random)
    {
        var results = new List<Vector3>(count);
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            return results;
        }

        var candidates = new List<Vector3>(spawnPoints.Count);
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            candidates.Add(spawnPoints[i].transform.position);
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            int swap = random.Next(i, candidates.Count);
            var temp = candidates[i];
            candidates[i] = candidates[swap];
            candidates[swap] = temp;
        }

        for (int i = 0; i < candidates.Count && results.Count < count; i++)
        {
            results.Add(candidates[i]);
        }

        if (results.Count < count)
        {
            Debug.LogWarning($"[FusionLauncher] SpawnPoint 부족: {results.Count}/{count}. SpawnPoint를 더 배치하세요.", this);
        }

        return results;
    }

    private int GetSpawnSeed()
    {
        if (runner != null && runner.SessionInfo.IsValid && string.IsNullOrEmpty(runner.SessionInfo.Name) == false)
        {
            return runner.SessionInfo.Name.GetHashCode();
        }

        return spawnSeed;
    }
}
