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
        return GetTotalNonSeekersCount() > 0 && GetAliveNonSeekersCount() == 0 || GetSeekersCount() == 0;
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

