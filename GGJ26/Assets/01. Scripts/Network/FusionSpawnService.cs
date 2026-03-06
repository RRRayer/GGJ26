using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class FusionSpawnService : MonoBehaviour
{
    private static FusionSpawnService instance;

    private readonly FusionSpawnLayout spawnLayout = new FusionSpawnLayout();
    private readonly FusionNpcSpawner npcSpawner = new FusionNpcSpawner();
    private readonly Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    private NetworkRunner runner;
    private FusionRoleAssignmentService roleService;
    private PlayerStateManager playerStateManager;

    private NetworkObject playerPrefab;
    private NetworkObject seekerPrefab;
    private NetworkObject normalPrefab;
    private float fallbackSpawnRadius;
    private string spawnAreaTag;
    private LayerMask spawnGroundLayers;
    private float minSpawnDistance;
    private int spawnSeed;
    private bool useGridSampling;
    private float gridJitter;
    private bool preferSpawnPoints;
    private int requiredSpawnPoints;
    private float spawnPointWaitSeconds;
    private NetworkObject redNpcPrefab;
    private NetworkObject blueNpcPrefab;
    private NetworkObject greenNpcPrefab;
    private int npcsPerColor;
    private int maxPlayers;
    private int minPlayersToAssignRoles = 1;

    private bool spawnLayoutRoutineRunning;
    private bool pendingLocalSpawn;
    private bool npcSpawned;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("[FusionSpawnService] Duplicate instance detected. Disabling this component.");
            Destroy(this);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void Configure(
        NetworkObject playerPrefab,
        NetworkObject seekerPrefab,
        NetworkObject normalPrefab,
        float fallbackSpawnRadius,
        string spawnAreaTag,
        LayerMask spawnGroundLayers,
        float minSpawnDistance,
        int spawnSeed,
        bool useGridSampling,
        float gridJitter,
        bool preferSpawnPoints,
        int requiredSpawnPoints,
        float spawnPointWaitSeconds,
        NetworkObject redNpcPrefab,
        NetworkObject blueNpcPrefab,
        NetworkObject greenNpcPrefab,
        int npcsPerColor,
        int maxPlayers,
        int minPlayersToAssignRoles)
    {
        this.playerPrefab = playerPrefab;
        this.seekerPrefab = seekerPrefab;
        this.normalPrefab = normalPrefab;
        this.fallbackSpawnRadius = fallbackSpawnRadius;
        this.spawnAreaTag = spawnAreaTag;
        this.spawnGroundLayers = spawnGroundLayers;
        this.minSpawnDistance = minSpawnDistance;
        this.spawnSeed = spawnSeed;
        this.useGridSampling = useGridSampling;
        this.gridJitter = gridJitter;
        this.preferSpawnPoints = preferSpawnPoints;
        this.requiredSpawnPoints = requiredSpawnPoints;
        this.spawnPointWaitSeconds = spawnPointWaitSeconds;
        this.redNpcPrefab = redNpcPrefab;
        this.blueNpcPrefab = blueNpcPrefab;
        this.greenNpcPrefab = greenNpcPrefab;
        this.npcsPerColor = npcsPerColor;
        this.maxPlayers = Mathf.Max(1, maxPlayers);
        this.minPlayersToAssignRoles = Mathf.Max(1, minPlayersToAssignRoles);
    }

    public void SetMaxPlayers(int value)
    {
        maxPlayers = Mathf.Max(1, value);
    }

    public void BindRunner(NetworkRunner targetRunner)
    {
        runner = targetRunner;
    }

    public void BindRoleService(FusionRoleAssignmentService roleService, PlayerStateManager playerStateManager)
    {
        this.roleService = roleService;
        this.playerStateManager = playerStateManager;
    }

    public void TickUpdate(bool isGameScene)
    {
        if (runner == null || runner.IsRunning == false)
        {
            return;
        }

        if (isGameScene == false)
        {
            return;
        }

        if (spawnLayout.SpawnLayoutBuilt == false && spawnLayoutRoutineRunning == false)
        {
            spawnLayoutRoutineRunning = true;
            StartCoroutine(WaitForSpawnPointsAndBuild());
        }

        if (pendingLocalSpawn)
        {
            if (TrySpawnLocalPlayer(runner.LocalPlayer))
            {
                pendingLocalSpawn = false;
            }
        }
    }

    public void OnPlayerJoined(PlayerRef player)
    {
        if (runner == null || runner.IsRunning == false)
        {
            return;
        }

        if (player == runner.LocalPlayer)
        {
            pendingLocalSpawn = true;
        }
    }

    public void OnPlayerLeft(PlayerRef player)
    {
        if (runner == null || runner.IsRunning == false)
        {
            return;
        }

        if (spawnedPlayers.TryGetValue(player, out var obj))
        {
            if (obj != null && obj.IsValid)
            {
                runner.Despawn(obj);
            }

            spawnedPlayers.Remove(player);
        }
    }

    public void OnSceneLoadDone(bool isGameScene)
    {
        if (isGameScene)
        {
            pendingLocalSpawn = true;
            spawnLayout.RefreshSpawnPoints();
        }
    }

    public void ResetForScene()
    {
        spawnLayout.Reset();
        npcSpawner.Clear();
        npcSpawned = false;
        pendingLocalSpawn = false;
        spawnLayoutRoutineRunning = false;
        spawnedPlayers.Clear();
        if (roleService != null)
        {
            roleService.ResetAssignment();
        }
    }

    private IEnumerator WaitForSpawnPointsAndBuild()
    {
        float timer = 0f;

        while (timer < spawnPointWaitSeconds)
        {
            spawnLayout.RefreshSpawnPoints();
            if (preferSpawnPoints == false || spawnLayout.GetSpawnPointCount() >= requiredSpawnPoints)
            {
                break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        TryBuildSpawnLayout();
        spawnLayoutRoutineRunning = false;
    }

    private void TryBuildSpawnLayout()
    {
        if (spawnLayout.SpawnLayoutBuilt || runner == null || runner.IsRunning == false)
        {
            return;
        }

        spawnLayout.RefreshSpawnPoints();
        spawnLayout.BuildSpawnLayout(
            runner,
            npcsPerColor,
            maxPlayers,
            minSpawnDistance,
            spawnSeed,
            preferSpawnPoints,
            requiredSpawnPoints,
            gridJitter,
            useGridSampling,
            spawnAreaTag,
            spawnGroundLayers,
            fallbackSpawnRadius);

        if (spawnLayout.SpawnLayoutBuilt)
        {
            TrySpawnNpcs();
        }
    }

    private void TrySpawnNpcs()
    {
        if (npcSpawned)
        {
            return;
        }

        if (runner == null || runner.IsRunning == false)
        {
            return;
        }

        if (runner.IsSharedModeMasterClient == false)
        {
            return;
        }

        if (spawnLayout.SpawnLayoutBuilt == false)
        {
            return;
        }

        var positions = spawnLayout.GetNpcSpawnPositions(npcsPerColor);
        npcSpawner.SpawnNpcs(runner, positions, redNpcPrefab, blueNpcPrefab, greenNpcPrefab, npcsPerColor);
        npcSpawned = true;
    }

    private bool TrySpawnLocalPlayer(PlayerRef player)
    {
        if (runner == null || runner.IsRunning == false)
        {
            return false;
        }

        if (player != runner.LocalPlayer)
        {
            return false;
        }

        int activePlayerCount = runner.ActivePlayers.Count();
        if (activePlayerCount < minPlayersToAssignRoles)
        {
            return false;
        }

        if (spawnedPlayers.ContainsKey(player))
        {
            return true;
        }

        if (runner.TryGetPlayerObject(player, out var existing) && existing != null)
        {
            spawnedPlayers[player] = existing;
            return true;
        }

        var prefab = GetPrefabForPlayer(player);
        if (prefab == null)
        {
            return false;
        }

        spawnLayout.RefreshSpawnPoints();
        Vector3 spawnPosition = spawnLayout.GetSpawnPosition(player, maxPlayers, fallbackSpawnRadius);

        var obj = runner.Spawn(prefab, spawnPosition, Quaternion.identity, player);
        if (obj == null)
        {
            return false;
        }

        runner.SetPlayerObject(player, obj);
        spawnedPlayers[player] = obj;
        Debug.Log($"[FusionSpawnService] Spawned player {player} at {spawnPosition}");

        if (roleService != null)
        {
            roleService.RegisterSpawnedPlayer(runner, player, playerStateManager);
        }

        return true;
    }

    private NetworkObject GetPrefabForPlayer(PlayerRef player)
    {
        if (GameModeRuntime.IsDeathmatch)
        {
            if (normalPrefab != null)
            {
                return normalPrefab;
            }

            return playerPrefab;
        }

        if (roleService != null && roleService.IsSeeker(runner, player))
        {
            if (seekerPrefab != null)
            {
                return seekerPrefab;
            }
        }

        if (normalPrefab != null)
        {
            return normalPrefab;
        }

        return playerPrefab;
    }
}
