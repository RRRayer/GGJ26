using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class FusionRoleAssignmentService : MonoBehaviour
{
    private bool seekerLocked;
    private PlayerRef lockedSeeker;
    private string lockedSessionName;

    public void ResetAssignment()
    {
        seekerLocked = false;
        lockedSeeker = default;
        lockedSessionName = string.Empty;
    }

    public PlayerRef GetDeterministicSeeker(NetworkRunner runner)
    {
        if (runner == null || runner.IsRunning == false)
        {
            return default;
        }

        var players = runner.ActivePlayers.OrderBy(p => p.RawEncoded).ToList();
        if (players.Count == 0)
        {
            return default;
        }

        // Solo sessions always assign the only participant as seeker.
        if (players.Count == 1)
        {
            return players[0];
        }

        string sessionName = runner.SessionInfo.IsValid ? runner.SessionInfo.Name : string.Empty;
        if (seekerLocked && string.Equals(lockedSessionName, sessionName) && players.Contains(lockedSeeker))
        {
            return lockedSeeker;
        }

        // Stable random by session so all peers pick the same seeker.
        int seed = sessionName.GetHashCode();
        if (string.IsNullOrWhiteSpace(sessionName))
        {
            seed = SceneManager.GetActiveScene().path.GetHashCode();
        }

        int index = Mathf.Abs(seed) % players.Count;
        PlayerRef chosen = players[index];

        seekerLocked = true;
        lockedSeeker = chosen;
        lockedSessionName = sessionName;

        return chosen;
    }

    public bool IsSeeker(NetworkRunner runner, PlayerRef player)
    {
        if (runner == null || runner.IsRunning == false)
        {
            return false;
        }

        var seeker = GetDeterministicSeeker(runner);
        if (seeker == default)
        {
            return false;
        }
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
