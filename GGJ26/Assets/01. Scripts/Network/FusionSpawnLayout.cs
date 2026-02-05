using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class FusionSpawnLayout
{
    private readonly List<NetworkSpawnPoint> spawnPoints = new List<NetworkSpawnPoint>();
    private readonly Dictionary<PlayerRef, Vector3> playerSpawnPositions = new Dictionary<PlayerRef, Vector3>();
    private readonly List<Vector3> allSpawnPositions = new List<Vector3>();

    private bool spawnLayoutBuilt;
    private bool hasSpawnBounds;
    private Vector3 spawnBoundsCenter;
    private Vector3 spawnBoundsSize;

    private BoxCollider spawnArea;

    public bool SpawnLayoutBuilt => spawnLayoutBuilt;
    public IReadOnlyDictionary<PlayerRef, Vector3> PlayerSpawnPositions => playerSpawnPositions;

    public void Reset()
    {
        spawnLayoutBuilt = false;
        hasSpawnBounds = false;
        spawnArea = null;
        playerSpawnPositions.Clear();
        allSpawnPositions.Clear();
    }

    public void RefreshSpawnPoints()
    {
        spawnPoints.Clear();
        var found = Object.FindObjectsByType<NetworkSpawnPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (found == null)
        {
            return;
        }

        spawnPoints.AddRange(found);
        spawnPoints.Sort((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            int nameCompare = string.CompareOrdinal(a.name, b.name);
            if (nameCompare != 0) return nameCompare;
            Vector3 pa = a.transform.position;
            Vector3 pb = b.transform.position;
            int x = pa.x.CompareTo(pb.x);
            if (x != 0) return x;
            int y = pa.y.CompareTo(pb.y);
            if (y != 0) return y;
            return pa.z.CompareTo(pb.z);
        });
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[FusionSpawnLayout] RefreshSpawnPoints count={spawnPoints.Count}");
        }
    }

    public int GetSpawnPointCount()
    {
        return spawnPoints.Count;
    }

    public Vector3 GetSpawnPosition(PlayerRef player, int maxPlayers, float fallbackSpawnRadius)
    {
        if (playerSpawnPositions.TryGetValue(player, out var position))
        {
            return position;
        }

        var angle = (Mathf.Abs(player.RawEncoded) % maxPlayers) / (float)maxPlayers * Mathf.PI * 2f;
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * fallbackSpawnRadius;
    }

    public void BuildSpawnLayout(
        NetworkRunner runner,
        int npcsPerColor,
        int maxPlayers,
        float minSpawnDistance,
        int spawnSeed,
        bool preferSpawnPoints,
        int requiredSpawnPoints,
        float gridJitter,
        bool useGridSampling,
        string spawnAreaTag,
        LayerMask spawnGroundLayers,
        float fallbackSpawnRadius)
    {
        if (runner == null || runner.IsRunning == false)
        {
            return;
        }

        ResolveSpawnArea(preferSpawnPoints, spawnAreaTag);
        bool useSpawnPoints = preferSpawnPoints && spawnPoints.Count > 0;
        if (spawnArea == null && hasSpawnBounds == false && (preferSpawnPoints == false || spawnPoints.Count == 0))
        {
            Debug.LogWarning("[FusionSpawnLayout] SpawnArea not found. Using fallback radius.");
        }

        int playerCount = runner.ActivePlayers.Count();
        int npcCount = Mathf.Max(0, npcsPerColor) * 3;
        int totalCount = playerCount + npcCount;
        if (totalCount <= 0)
        {
            return;
        }

        var random = new System.Random(GetSpawnSeed(runner, spawnSeed));
        var positions = new List<Vector3>(totalCount);
        if (useSpawnPoints)
        {
            positions.AddRange(GetSpawnPointPositions(totalCount, random));
        }
        else if ((spawnArea != null || hasSpawnBounds) && useGridSampling)
        {
            positions.AddRange(GenerateGridPositions(totalCount, random, gridJitter, spawnGroundLayers));
        }
        else
        {
            int attempts = 0;
            int maxAttempts = totalCount * 200;
            float minDistSqr = minSpawnDistance * minSpawnDistance;

            while (positions.Count < totalCount && attempts < maxAttempts)
            {
                attempts++;
                Vector3 candidate = SampleSpawnPosition(random, spawnGroundLayers, fallbackSpawnRadius);
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
            Debug.LogWarning($"[FusionSpawnLayout] Spawn positions 부족: {positions.Count}/{totalCount}. 최소 거리 완화로 채웁니다.");
            FillMissingPositions(positions, totalCount, random, minSpawnDistance, spawnGroundLayers, fallbackSpawnRadius);
        }

        playerSpawnPositions.Clear();
        allSpawnPositions.Clear();
        allSpawnPositions.AddRange(positions);
        var players = runner.ActivePlayers.OrderBy(p => p.RawEncoded).ToList();
        int index = 0;
        for (int i = 0; i < players.Count && index < positions.Count; i++)
        {
            playerSpawnPositions[players[i]] = positions[index++];
        }

        spawnLayoutBuilt = true;
        if (spawnArea != null)
        {
            Debug.Log($"[FusionSpawnLayout] SpawnArea bounds size={spawnArea.bounds.size} center={spawnArea.bounds.center}");
        }
        else if (hasSpawnBounds)
        {
            Debug.Log($"[FusionSpawnLayout] SpawnBounds size={spawnBoundsSize} center={spawnBoundsCenter}");
        }
        Debug.Log($"[FusionSpawnLayout] Spawn layout built: total={totalCount} players={playerCount} npcs={npcCount} positions={positions.Count}");
        LogPositionSample(positions, playerCount, npcCount);
    }

    public List<Vector3> GetNpcSpawnPositions(int npcsPerColor)
    {
        int totalNpc = Mathf.Max(0, npcsPerColor) * 3;
        var results = new List<Vector3>(totalNpc);
        if (totalNpc <= 0)
        {
            return results;
        }

        int index = playerSpawnPositions.Count;
        if (index < 0)
        {
            index = 0;
        }

        for (int i = index; i < allSpawnPositions.Count && results.Count < totalNpc; i++)
        {
            results.Add(allSpawnPositions[i]);
        }

        return results;
    }

    private void ResolveSpawnArea(bool preferSpawnPoints, string spawnAreaTag)
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

        bounds.Expand(1f);
        spawnBoundsCenter = bounds.center;
        spawnBoundsSize = bounds.size;
        hasSpawnBounds = true;
    }

    private Vector3 SampleSpawnPosition(System.Random random, LayerMask spawnGroundLayers, float fallbackSpawnRadius)
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

    private List<Vector3> GenerateGridPositions(int count, System.Random random, float gridJitter, LayerMask spawnGroundLayers)
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

    private void FillMissingPositions(List<Vector3> positions, int totalCount, System.Random random, float minSpawnDistance, LayerMask spawnGroundLayers, float fallbackSpawnRadius)
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
            Vector3 candidate = SampleSpawnPosition(random, spawnGroundLayers, fallbackSpawnRadius);
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
        int skippedZero = 0;
        List<string> zeroNames = null;
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            var pos = spawnPoints[i].transform.position;
            if (pos.sqrMagnitude < 0.001f)
            {
                skippedZero++;
                zeroNames ??= new List<string>();
                zeroNames.Add($"{spawnPoints[i].name} ({spawnPoints[i].gameObject.scene.name})");
                continue;
            }
            candidates.Add(pos);
        }
        if (skippedZero > 0)
        {
            string detail = zeroNames != null ? string.Join(", ", zeroNames) : "";
            Debug.LogWarning($"[FusionSpawnLayout] Ignored {skippedZero} spawn points near (0,0,0). {detail}");
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
            Debug.LogWarning($"[FusionSpawnLayout] SpawnPoint 부족: {results.Count}/{count}. SpawnPoint를 더 배치하세요.");
        }

        return results;
    }

    private static int GetSpawnSeed(NetworkRunner runner, int spawnSeed)
    {
        if (runner != null && runner.SessionInfo.IsValid && string.IsNullOrEmpty(runner.SessionInfo.Name) == false)
        {
            return runner.SessionInfo.Name.GetHashCode();
        }

        return spawnSeed;
    }

    private void LogPositionSample(List<Vector3> positions, int playerCount, int npcCount)
    {
        if (positions == null || positions.Count == 0)
        {
            return;
        }

        int sampleCount = Mathf.Min(5, positions.Count);
        string sample = "";
        for (int i = 0; i < sampleCount; i++)
        {
            if (i > 0)
            {
                sample += ", ";
            }
            sample += $"({positions[i].x:0.##},{positions[i].y:0.##},{positions[i].z:0.##})";
        }

        int unique = CountUniquePositions(positions, 0.01f);
        Debug.Log($"[FusionSpawnLayout] Sample positions: {sample} | unique={unique}/{positions.Count} (players={playerCount}, npcs={npcCount})");
    }

    private int CountUniquePositions(List<Vector3> positions, float tolerance)
    {
        if (positions == null || positions.Count == 0)
        {
            return 0;
        }

        float tolSqr = tolerance * tolerance;
        var uniques = new List<Vector3>();
        for (int i = 0; i < positions.Count; i++)
        {
            bool found = false;
            for (int j = 0; j < uniques.Count; j++)
            {
                if ((uniques[j] - positions[i]).sqrMagnitude <= tolSqr)
                {
                    found = true;
                    break;
                }
            }
            if (found == false)
            {
                uniques.Add(positions[i]);
            }
        }

        return uniques.Count;
    }
}
