using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerStateManager : NetworkBehaviour
{
    private readonly Dictionary<string, PlayerState> players = new Dictionary<string, PlayerState>();
    private readonly List<(string playerId, bool isSeeker)> pendingRegisters = new List<(string, bool)>();
    private readonly List<string> pendingDead = new List<string>();
    private string localPlayerId;

    public event System.Action<PlayerState> OnPlayerStateChanged;

    public override void Spawned()
    {
        FlushPendingNetworkOps();
    }

    public override void FixedUpdateNetwork()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            FlushPendingNetworkOps();
        }
    }

    private void Update()
    {
        if (pendingRegisters.Count == 0 && pendingDead.Count == 0)
        {
            return;
        }

        if (Runner != null && Runner.IsRunning && Runner.IsSharedModeMasterClient)
        {
            FlushPendingLocalOnly();
        }
    }

    public void RegisterPlayerNetworked(string playerId, bool isSeeker)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            return;
        }

        if (Object == null || Runner == null || Runner.IsRunning == false)
        {
            if (TryHandleWithoutNetwork(playerId, isSeeker))
            {
                return;
            }

            pendingRegisters.Add((playerId, isSeeker));
            Debug.Log($"[PlayerStateManager] Queue RegisterPlayer: {playerId} seeker={isSeeker} (runner={(Runner != null && Runner.IsRunning)})");
            return;
        }

        if (Object != null && Object.HasStateAuthority)
        {
            RpcRegisterPlayer(playerId, isSeeker);
            return;
        }

        RpcRequestRegisterPlayer(playerId, isSeeker);
    }

    public void MarkDeadNetworked(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            return;
        }

        if (Object == null || Runner == null || Runner.IsRunning == false)
        {
            if (TryHandleWithoutNetwork(playerId, null))
            {
                return;
            }

            pendingDead.Add(playerId);
            Debug.Log($"[PlayerStateManager] Queue MarkDead: {playerId} (runner={(Runner != null && Runner.IsRunning)})");
            return;
        }

        if (Object != null && Object.HasStateAuthority)
        {
            RpcMarkDead(playerId);
            return;
        }

        RpcRequestMarkDead(playerId);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RpcRequestRegisterPlayer(string playerId, bool isSeeker)
    {
        RpcRegisterPlayer(playerId, isSeeker);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RpcRequestMarkDead(string playerId)
    {
        RpcMarkDead(playerId);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcRegisterPlayer(string playerId, bool isSeeker)
    {
        RegisterPlayerLocal(playerId, isSeeker);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcMarkDead(string playerId)
    {
        MarkDeadLocal(playerId);
    }

    private void RegisterPlayerLocal(string playerId, bool isSeeker)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            return;
        }

        PlayerState playerState;
        if (players.ContainsKey(playerId))
        {
            playerState = players[playerId];
            playerState.IsSeeker = isSeeker;
            Debug.Log($"[PlayerStateManager] Updating player {playerId}: IsSeeker = {isSeeker}");
        }
        else
        {
            playerState = new PlayerState(playerId, isSeeker);
            players[playerId] = playerState;
            Debug.Log($"[PlayerStateManager] Registering new player {playerId}: IsSeeker = {isSeeker}");
        }

        OnPlayerStateChanged?.Invoke(playerState);
    }

    public void SetLocalPlayer(string playerId)
    {
        localPlayerId = playerId;
        // When the local player is set, we might already have their state, so invoke the event.
        if (players.TryGetValue(playerId, out var playerState))
        {
            OnPlayerStateChanged?.Invoke(playerState);
        }
    }

    private void MarkDeadLocal(string playerId)
    {
        if (TryGetPlayer(playerId, out var state))
        {
            state.IsDead = true;
            Debug.Log($"[PlayerStateManager] MarkDead: player {playerId} now dead.");
            OnPlayerStateChanged?.Invoke(state);
        }
        else
        {
            Debug.LogWarning($"[PlayerStateManager] MarkDead: player {playerId} not found.");
        }
    }

    public void AddElimination(string playerId, int amount = 1)
    {
        if (TryGetPlayer(playerId, out var state))
        {
            state.Eliminations += Mathf.Max(0, amount);
        }
    }

    public int GetAlivePlayerCount()
    {
        int count = 0;
        foreach (var entry in players.Values)
        {
            if (entry.IsDead == false)
            {
                count++;
            }
        }
        return count;
    }

    public int GetTotalPlayerCount()
    {
        return players.Count;
    }

    public PlayerState GetLastAlivePlayer()
    {
        if (GetAlivePlayerCount() != 1)
        {
            return null;
        }
        foreach (var entry in players.Values)
        {
            if (entry.IsDead == false)
            {
                return entry;
            }
        }
        return null;
    }

    public int GetAliveNonSeekersCount()
    {
        int count = 0;
        foreach (var entry in players.Values)
        {
            if (entry.IsSeeker == false && entry.IsDead == false)
            {
                count++;
            }
        }

        return count;
    }

    public int GetSeekersCount()
    {
        int count = 0;
        foreach (var entry in players.Values)
        {
            if (entry.IsSeeker)
            {
                count++;
            }
        }

        Debug.Log("Total Seekers Count: " + count);
        return count;
    }

    public int GetTotalNonSeekersCount()
    {
        int count = 0;
        foreach (var entry in players.Values)
        {
            if (entry.IsSeeker == false)
            {
                count++;
            }
        }
        Debug.Log("Total Non-Seekers Count: " + count);
        return count;
    }

    public bool AreAllNonSeekersDead()
    {
        // The game should end if there was at least one non-seeker to begin with, and now there are none left alive.
        return GetTotalNonSeekersCount() > 0 && GetAliveNonSeekersCount() == 0;
    }

    public bool TryGetLocalPlayer(out PlayerState state)
    {
        state = null;
        if (string.IsNullOrEmpty(localPlayerId))
        {
            return false;
        }

        return TryGetPlayer(localPlayerId, out state);
    }

    private bool TryGetPlayer(string playerId, out PlayerState state)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            state = null;
            return false;
        }

        return players.TryGetValue(playerId, out state);
    }

    private void FlushPendingNetworkOps()
    {
        if (pendingRegisters.Count > 0)
        {
            var pending = new List<(string playerId, bool isSeeker)>(pendingRegisters);
            pendingRegisters.Clear();
            for (int i = 0; i < pending.Count; i++)
            {
                Debug.Log($"[PlayerStateManager] Flush RegisterPlayer: {pending[i].playerId} seeker={pending[i].isSeeker}");
                RegisterPlayerNetworked(pending[i].playerId, pending[i].isSeeker);
            }
        }

        if (pendingDead.Count > 0)
        {
            var pending = new List<string>(pendingDead);
            pendingDead.Clear();
            for (int i = 0; i < pending.Count; i++)
            {
                Debug.Log($"[PlayerStateManager] Flush MarkDead: {pending[i]}");
                MarkDeadNetworked(pending[i]);
            }
        }
    }

    private bool TryHandleWithoutNetwork(string playerId, bool? isSeeker)
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

        if (Object == null)
        {
            Debug.LogWarning("[PlayerStateManager] NetworkObject not spawned. Using local fallback.");
        }

        if (isSeeker.HasValue)
        {
            RegisterPlayerLocal(playerId, isSeeker.Value);
            Debug.Log($"[PlayerStateManager] Fallback RegisterPlayer (no network): {playerId} seeker={isSeeker.Value}");
        }
        else
        {
            MarkDeadLocal(playerId);
            Debug.Log($"[PlayerStateManager] Fallback MarkDead (no network): {playerId}");
        }

        return true;
    }

    private void FlushPendingLocalOnly()
    {
        if (pendingRegisters.Count > 0)
        {
            var pending = new List<(string playerId, bool isSeeker)>(pendingRegisters);
            pendingRegisters.Clear();
            for (int i = 0; i < pending.Count; i++)
            {
                RegisterPlayerLocal(pending[i].playerId, pending[i].isSeeker);
                Debug.Log($"[PlayerStateManager] Flush Local RegisterPlayer: {pending[i].playerId} seeker={pending[i].isSeeker}");
            }
        }

        if (pendingDead.Count > 0)
        {
            var pending = new List<string>(pendingDead);
            pendingDead.Clear();
            for (int i = 0; i < pending.Count; i++)
            {
                MarkDeadLocal(pending[i]);
                Debug.Log($"[PlayerStateManager] Flush Local MarkDead: {pending[i]}");
            }
        }
    }
}
