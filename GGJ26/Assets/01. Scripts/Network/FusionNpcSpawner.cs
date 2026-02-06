using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class FusionNpcSpawner
{
    private readonly List<NetworkObject> spawnedNpcs = new List<NetworkObject>();

    public IReadOnlyList<NetworkObject> SpawnedNpcs => spawnedNpcs;

    public void Clear()
    {
        spawnedNpcs.Clear();
    }

    public void SpawnNpcs(NetworkRunner runner, List<Vector3> positions, NetworkObject redNpcPrefab, NetworkObject blueNpcPrefab, NetworkObject greenNpcPrefab, int npcsPerColor)
    {
        if (runner == null || runner.IsRunning == false)
        {
            return;
        }

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
        int index = 0;
        for (int color = 0; color < prefabs.Length; color++)
        {
            var prefab = prefabs[color];
            if (prefab == null)
            {
                continue;
            }

            for (int i = 0; i < npcsPerColor && index < positions.Count; i++)
            {
                Vector3 spawnPosition = positions[index++];
                var npc = runner.Spawn(prefab, spawnPosition, Quaternion.identity);
                if (npc != null)
                {
                    if ((npc.transform.position - spawnPosition).sqrMagnitude > 0.01f)
                    {
                        npc.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
                        var navAgent = npc.GetComponent<UnityEngine.AI.NavMeshAgent>();
                        if (navAgent != null && navAgent.enabled)
                        {
                            navAgent.Warp(spawnPosition);
                        }
                    }
                    spawnedNpcs.Add(npc);
                }
            }
        }

        Debug.Log($"[FusionNpcSpawner] SpawnNpcs done: spawned={spawnedNpcs.Count}/{totalNpc}");
    }
}
