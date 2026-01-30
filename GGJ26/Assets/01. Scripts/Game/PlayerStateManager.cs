using System.Collections.Generic;
using UnityEngine;

public class PlayerStateManager : MonoBehaviour
{
    private readonly Dictionary<string, PlayerState> players = new Dictionary<string, PlayerState>();
    private string localPlayerId;

    public void RegisterPlayer(string playerId, bool isSeeker)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            return;
        }

        if (players.ContainsKey(playerId))
        {
            players[playerId].IsSeeker = isSeeker;
            return;
        }

        players[playerId] = new PlayerState(playerId, isSeeker);
    }

    public void SetLocalPlayer(string playerId)
    {
        localPlayerId = playerId;
    }

    public void MarkDead(string playerId)
    {
        if (TryGetPlayer(playerId, out var state))
        {
            state.IsDead = true;
        }
    }

    public void AddElimination(string playerId, int amount = 1)
    {
        if (TryGetPlayer(playerId, out var state))
        {
            state.Eliminations += Mathf.Max(0, amount);
        }
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

    public bool AreAllNonSeekersDead()
    {
        return GetAliveNonSeekersCount() == 0 && GetSeekersCount() > 0;
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
}
