using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GroupDanceMaskChanger : MonoBehaviour
{
    [SerializeField] private VoidEventChannelSO stopDiscoEvent;

    private void OnEnable()
    {
        if (stopDiscoEvent != null)
        {
            stopDiscoEvent.OnEventRaised += OnGroupDanceEnded;
        }
    }

    private void OnDisable()
    {
        if (stopDiscoEvent != null)
        {
            stopDiscoEvent.OnEventRaised -= OnGroupDanceEnded;
        }
    }

    private void OnGroupDanceEnded()
    {
        Debug.Log("[GroupDanceMaskChanger] StopDiscoEvent received.", this);
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null || runner.IsRunning == false || runner.IsSharedModeMasterClient == false)
        {
            Debug.Log("[GroupDanceMaskChanger] Skip: not master or runner not ready.", this);
            return;
        }

        ApplyMaskChanges();
    }

    private void ApplyMaskChanges()
    {
        var roles = FindObjectsByType<PlayerRole>(FindObjectsSortMode.None);
        var aliveNonSeekers = new List<PlayerRole>();
        foreach (var role in roles)
        {
            if (role == null || role.IsSeeker)
            {
                continue;
            }

            var elimination = role.GetComponent<PlayerElimination>();
            if (elimination != null && elimination.IsEliminated)
            {
                continue;
            }

            aliveNonSeekers.Add(role);
        }

        if (aliveNonSeekers.Count == 0)
        {
            Debug.Log("[GroupDanceMaskChanger] No alive non-seekers found.", this);
            return;
        }

        Debug.Log($"[GroupDanceMaskChanger] Applying mask changes to {aliveNonSeekers.Count} players.", this);
        AssignUniqueMasks(aliveNonSeekers);
    }

    private void AssignUniqueMasks(List<PlayerRole> players)
    {
        int count = players.Count;

        if (count == 1)
        {
            var role = players[0];
            int current = role.GetMaskColorIndex();
            int next = (current + Random.Range(1, 3)) % 3;
            Debug.Log($"[GroupDanceMaskChanger] Single player mask {current} -> {next}", role);
            role.RequestMaskColorChange(next);
            return;
        }

        var colors = new int[] { 0, 1, 2 };
        var permutations = GetPermutations(colors, count);
        var currentColors = new int[count];
        for (int i = 0; i < count; i++)
        {
            currentColors[i] = players[i].GetMaskColorIndex();
        }

        List<int[]> valid = new List<int[]>();
        foreach (var perm in permutations)
        {
            bool ok = true;
            for (int i = 0; i < count; i++)
            {
                if (perm[i] == currentColors[i])
                {
                    ok = false;
                    break;
                }
            }

            if (ok)
            {
                valid.Add(perm);
            }
        }

        if (valid.Count == 0)
        {
            // Relax "all unique" constraint but keep:
            // 1) each player must change color
            // 2) not everyone ends up in the same color
            var relaxedCandidates = GetReassignmentCandidates(currentColors);
            if (relaxedCandidates.Count > 0)
            {
                var chosenRelaxed = relaxedCandidates[Random.Range(0, relaxedCandidates.Count)];
                Debug.Log($"[GroupDanceMaskChanger] Using relaxed candidate: {string.Join(",", chosenRelaxed)}", this);
                for (int i = 0; i < count; i++)
                {
                    Debug.Log($"[GroupDanceMaskChanger] Relaxed mask {currentColors[i]} -> {chosenRelaxed[i]}", players[i]);
                    players[i].RequestMaskColorChange(chosenRelaxed[i]);
                }
                return;
            }

            Debug.Log("[GroupDanceMaskChanger] No relaxed candidate found; using +1 fallback.", this);
            for (int i = 0; i < count; i++)
            {
                int current = currentColors[i];
                int next = (current + 1) % 3;
                Debug.Log($"[GroupDanceMaskChanger] Fallback mask {current} -> {next}", players[i]);
                players[i].RequestMaskColorChange(next);
            }
            return;
        }

        var chosen = valid[Random.Range(0, valid.Count)];
        Debug.Log($"[GroupDanceMaskChanger] Chosen permutation: {string.Join(",", chosen)}", this);
        for (int i = 0; i < count; i++)
        {
            Debug.Log($"[GroupDanceMaskChanger] Player {i} mask {currentColors[i]} -> {chosen[i]}", players[i]);
            players[i].RequestMaskColorChange(chosen[i]);
        }
    }

    private List<int[]> GetReassignmentCandidates(int[] currentColors)
    {
        var results = new List<int[]>();
        int count = currentColors.Length;
        var current = new int[count];

        void Dfs(int depth)
        {
            if (depth == count)
            {
                if (count > 1 && IsAllSame(current))
                {
                    return;
                }

                var arr = new int[count];
                for (int i = 0; i < count; i++)
                {
                    arr[i] = current[i];
                }
                results.Add(arr);
                return;
            }

            for (int color = 0; color < 3; color++)
            {
                if (color == currentColors[depth])
                {
                    continue;
                }

                current[depth] = color;
                Dfs(depth + 1);
            }
        }

        Dfs(0);
        return results;
    }

    private static bool IsAllSame(int[] values)
    {
        if (values == null || values.Length <= 1)
        {
            return true;
        }

        int first = values[0];
        for (int i = 1; i < values.Length; i++)
        {
            if (values[i] != first)
            {
                return false;
            }
        }

        return true;
    }

    private List<int[]> GetPermutations(int[] colors, int length)
    {
        var results = new List<int[]>();
        var used = new bool[colors.Length];
        var current = new int[length];
        void Dfs(int depth)
        {
            if (depth == length)
            {
                var arr = new int[length];
                for (int i = 0; i < length; i++)
                {
                    arr[i] = current[i];
                }
                results.Add(arr);
                return;
            }

            for (int i = 0; i < colors.Length; i++)
            {
                if (used[i])
                {
                    continue;
                }

                used[i] = true;
                current[depth] = colors[i];
                Dfs(depth + 1);
                used[i] = false;
            }
        }

        Dfs(0);
        return results;
    }
}
