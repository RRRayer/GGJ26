using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerStateManager : NetworkBehaviour
{
    private readonly Dictionary<string, PlayerState> players = new Dictionary<string, PlayerState>();
    private string localPlayerId;
    [SerializeField] private PlayerStateFallback fallback;

    public event System.Action<PlayerState> OnPlayerStateChanged;
    public event System.Action OnAllNonSeekersDead;

    private void Awake()
    {
        if (fallback == null)
        {
            fallback = GetComponent<PlayerStateFallback>();
        }

        if (fallback == null)
        {
            fallback = gameObject.AddComponent<PlayerStateFallback>();
        }

        fallback.Configure(this);
    }

    public override void Spawned()
    {
        if (fallback != null)
        {
            fallback.FlushPendingNetworkOps();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            if (fallback != null)
            {
                fallback.FlushPendingNetworkOps();
            }
        }
    }

    private void Update()
    {
        if (fallback != null)
        {
            fallback.TickFallback();
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
            if (fallback != null && fallback.TryHandleWithoutNetwork(playerId, isSeeker))
            {
                return;
            }

            fallback?.QueueRegister(playerId, isSeeker);
            return;
        }

        SendRegisterRpc(playerId, isSeeker);
    }

    public void MarkDeadNetworked(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            return;
        }

        if (Object == null || Runner == null || Runner.IsRunning == false)
        {
            if (fallback != null && fallback.TryHandleWithoutNetwork(playerId, null))
            {
                return;
            }

            fallback?.QueueMarkDead(playerId);
            return;
        }

        SendMarkDeadRpc(playerId);
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
        ApplyRegisterLocal(playerId, isSeeker);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcMarkDead(string playerId)
    {
        ApplyMarkDeadLocal(playerId);
    }

    internal void ApplyRegisterLocal(string playerId, bool isSeeker)
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
        TryNotifyAllNonSeekersDead();
    }

    public void SetLocalPlayer(string playerId)
    {
        localPlayerId = playerId;
        if (players.TryGetValue(playerId, out var playerState))
        {
            OnPlayerStateChanged?.Invoke(playerState);
        }
    }

    internal void ApplyMarkDeadLocal(string playerId)
    {
        if (TryGetPlayer(playerId, out var state))
        {
            state.IsDead = true;
            Debug.Log($"[PlayerStateManager] MarkDead: player {playerId} now dead.");
            OnPlayerStateChanged?.Invoke(state);
            TryNotifyAllNonSeekersDead();
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
        return count;
    }

    public bool AreAllNonSeekersDead()
    {
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

    internal void SendRegisterRpc(string playerId, bool isSeeker)
    {
        if (Object != null && Object.HasStateAuthority)
        {
            RpcRegisterPlayer(playerId, isSeeker);
            return;
        }

        RpcRequestRegisterPlayer(playerId, isSeeker);
    }

    internal void SendMarkDeadRpc(string playerId)
    {
        if (Object != null && Object.HasStateAuthority)
        {
            RpcMarkDead(playerId);
            return;
        }

        RpcRequestMarkDead(playerId);
    }

    private void TryNotifyAllNonSeekersDead()
    {
        if (IsAuthoritative() == false)
        {
            return;
        }

        if (AreAllNonSeekersDead())
        {
            OnAllNonSeekersDead?.Invoke();
        }
    }

    private bool IsAuthoritative()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            return true;
        }

        if (Runner != null && Runner.IsRunning && Runner.IsSharedModeMasterClient)
        {
            return true;
        }

        return false;
    }
}
