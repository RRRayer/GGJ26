using Fusion;
using UnityEngine;

public class FusionRoleAssignmentService : MonoBehaviour
{
    public PlayerRef GetDeterministicSeeker(NetworkRunner runner)
    {
        if (runner == null || runner.IsRunning == false)
        {
            return default;
        }

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

        return chosen;
    }

    public bool IsSeeker(NetworkRunner runner, PlayerRef player)
    {
        if (runner == null || runner.IsRunning == false)
        {
            return false;
        }

        var seeker = GetDeterministicSeeker(runner);
        return seeker == player;
    }

    public void RegisterPlayerState(NetworkRunner runner, PlayerRef player, PlayerStateManager playerStateManager)
    {
        if (runner == null || playerStateManager == null)
        {
            return;
        }

        string playerId = player.RawEncoded.ToString();
        bool isSeeker = IsSeeker(runner, player);
        playerStateManager.RegisterPlayerNetworked(playerId, isSeeker);

        if (runner.LocalPlayer == player)
        {
            playerStateManager.SetLocalPlayer(playerId);
        }
    }

    public void RegisterSpawnedPlayer(NetworkRunner runner, PlayerRef player, PlayerStateManager playerStateManager)
    {
        RegisterPlayerState(runner, player, playerStateManager);
    }
}
