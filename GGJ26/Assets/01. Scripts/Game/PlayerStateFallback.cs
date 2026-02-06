using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerStateFallback : MonoBehaviour
{
    private readonly List<(string playerId, bool isSeeker)> pendingRegisters = new List<(string, bool)>();
    private readonly List<string> pendingDead = new List<string>();

    private PlayerStateManager manager;

    public void Configure(PlayerStateManager manager)
    {
        this.manager = manager;
    }

    public void QueueRegister(string playerId, bool isSeeker)
    {
        pendingRegisters.Add((playerId, isSeeker));
        Debug.Log($"[PlayerStateFallback] Queue RegisterPlayer: {playerId} seeker={isSeeker}");
    }

    public void QueueMarkDead(string playerId)
    {
        pendingDead.Add(playerId);
        Debug.Log($"[PlayerStateFallback] Queue MarkDead: {playerId}");
    }

    public void FlushPendingNetworkOps()
    {
        if (manager == null)
        {
            return;
        }

        if (pendingRegisters.Count > 0)
        {
            var pending = new List<(string playerId, bool isSeeker)>(pendingRegisters);
            pendingRegisters.Clear();
            for (int i = 0; i < pending.Count; i++)
            {
                Debug.Log($"[PlayerStateFallback] Flush RegisterPlayer: {pending[i].playerId} seeker={pending[i].isSeeker}");
                manager.SendRegisterRpc(pending[i].playerId, pending[i].isSeeker);
            }
        }

        if (pendingDead.Count > 0)
        {
            var pending = new List<string>(pendingDead);
            pendingDead.Clear();
            for (int i = 0; i < pending.Count; i++)
            {
                Debug.Log($"[PlayerStateFallback] Flush MarkDead: {pending[i]}");
                manager.SendMarkDeadRpc(pending[i]);
            }
        }
    }

    public void TickFallback()
    {
        if (manager == null)
        {
            return;
        }

        if (pendingRegisters.Count == 0 && pendingDead.Count == 0)
        {
            return;
        }

        var runner = manager.Runner;
        if (runner != null && runner.IsRunning && runner.IsSharedModeMasterClient)
        {
            FlushPendingLocalOnly();
        }
    }

    public bool TryHandleWithoutNetwork(string playerId, bool? isSeeker)
    {
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null || runner.IsRunning == false)
        {
            return false;
        }

        if (runner.IsSharedModeMasterClient == false)
        {
            return false;
        }

        if (manager == null)
        {
            return false;
        }

        if (manager.Object == null)
        {
            Debug.LogWarning("[PlayerStateFallback] NetworkObject not spawned. Using local fallback.");
        }

        if (isSeeker.HasValue)
        {
            manager.ApplyRegisterLocal(playerId, isSeeker.Value);
            Debug.Log($"[PlayerStateFallback] Fallback RegisterPlayer (no network): {playerId} seeker={isSeeker.Value}");
        }
        else
        {
            manager.ApplyMarkDeadLocal(playerId);
            Debug.Log($"[PlayerStateFallback] Fallback MarkDead (no network): {playerId}");
        }

        return true;
    }

    private void FlushPendingLocalOnly()
    {
        if (manager == null)
        {
            return;
        }

        if (pendingRegisters.Count > 0)
        {
            var pending = new List<(string playerId, bool isSeeker)>(pendingRegisters);
            pendingRegisters.Clear();
            for (int i = 0; i < pending.Count; i++)
            {
                manager.ApplyRegisterLocal(pending[i].playerId, pending[i].isSeeker);
                Debug.Log($"[PlayerStateFallback] Flush Local RegisterPlayer: {pending[i].playerId} seeker={pending[i].isSeeker}");
            }
        }

        if (pendingDead.Count > 0)
        {
            var pending = new List<string>(pendingDead);
            pendingDead.Clear();
            for (int i = 0; i < pending.Count; i++)
            {
                manager.ApplyMarkDeadLocal(pending[i]);
                Debug.Log($"[PlayerStateFallback] Flush Local MarkDead: {pending[i]}");
            }
        }
    }
}
