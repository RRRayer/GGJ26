using System.Collections.Generic;
using UnityEngine;

public class GameResultController : MonoBehaviour
{
    [SerializeField] private StatsManager statsManager;
    [SerializeField] private PlayerStateManager playerStateManager;
    [SerializeField] private GameStateController gameStateController;
    [SerializeField] private GameResultEventChannelSO onGameResult;
    [SerializeField] private VoidEventChannelSO onGameEnded;

    public bool HasEnded { get; private set; }

    public void Configure(
        StatsManager statsManager,
        PlayerStateManager playerStateManager,
        GameStateController gameStateController,
        GameResultEventChannelSO onGameResult,
        VoidEventChannelSO onGameEnded)
    {
        if (statsManager != null)
        {
            this.statsManager = statsManager;
        }

        if (playerStateManager != null)
        {
            this.playerStateManager = playerStateManager;
        }

        if (gameStateController != null)
        {
            this.gameStateController = gameStateController;
        }

        if (onGameResult != null)
        {
            this.onGameResult = onGameResult;
        }

        if (onGameEnded != null)
        {
            this.onGameEnded = onGameEnded;
        }
    }

    public void ResetResult()
    {
        HasEnded = false;
    }

    public void EndGame(bool seekerWin, float remainingSeconds)
    {
        if (HasEnded)
        {
            return;
        }

        HasEnded = true;

        if (gameStateController != null)
        {
            gameStateController.SetState(GameState.Ending);
        }

        bool localWin = seekerWin;
        if (playerStateManager != null && playerStateManager.TryGetLocalPlayer(out var localState))
        {
            localWin = localState.IsSeeker == seekerWin;
        }
        else
        {
            var localRole = FindLocalPlayerRole();
            if (localRole != null)
            {
                localWin = localRole.IsSeeker == seekerWin;
            }
        }

        float avgReaction = statsManager != null ? statsManager.GetAverageReactionMs() : 0f;
        List<MaskColor> history = statsManager != null ? statsManager.GetMaskHistory() : new List<MaskColor>();
        var result = new GameResultData(seekerWin, localWin, remainingSeconds, avgReaction, history);

        onGameResult?.RaiseEvent(result);
        onGameEnded?.RaiseEvent();
    }

    public bool TryResolveWinConditions(float remainingSeconds)
    {
        if (GameModeRuntime.IsDeathmatch)
        {
            return false;
        }

        if (playerStateManager == null)
        {
            playerStateManager = FindFirstObjectByType<PlayerStateManager>();
            if (playerStateManager == null)
            {
                return false;
            }
        }

        if (remainingSeconds <= 0f)
        {
            EndGame(seekerWin: false, remainingSeconds);
            return true;
        }

        if (playerStateManager.GetTotalPlayerCount() == 1 && playerStateManager.GetAlivePlayerCount() == 0)
        {
            EndGame(seekerWin: true, remainingSeconds);
            return true;
        }

        if (playerStateManager.AreAllNonSeekersDead())
        {
            EndGame(seekerWin: true, remainingSeconds);
            return true;
        }

        if (playerStateManager.GetTotalPlayerCount() > 1 && playerStateManager.GetAlivePlayerCount() <= 1)
        {
            var lastPlayer = playerStateManager.GetLastAlivePlayer();
            if (lastPlayer != null)
            {
                EndGame(seekerWin: lastPlayer.IsSeeker, remainingSeconds);
            }
            else
            {
                EndGame(seekerWin: playerStateManager.AreAllNonSeekersDead(), remainingSeconds);
            }
            return true;
        }

        return false;
    }

    public void EndGameDeathmatch(int winnerRawPlayerId, bool drawAllLose, float remainingSeconds)
    {
        if (HasEnded)
        {
            return;
        }

        HasEnded = true;
        if (gameStateController != null)
        {
            gameStateController.SetState(GameState.Ending);
        }

        bool localWin = false;
        if (drawAllLose == false && playerStateManager != null && playerStateManager.TryGetLocalPlayer(out var localState))
        {
            localWin = string.Equals(localState.PlayerId, winnerRawPlayerId.ToString(), System.StringComparison.Ordinal);
        }

        float avgReaction = statsManager != null ? statsManager.GetAverageReactionMs() : 0f;
        List<MaskColor> history = statsManager != null ? statsManager.GetMaskHistory() : new List<MaskColor>();
        var result = new GameResultData(false, localWin, remainingSeconds, avgReaction, history);

        onGameResult?.RaiseEvent(result);
        onGameEnded?.RaiseEvent();
    }

    private PlayerRole FindLocalPlayerRole()
    {
        var roles = FindObjectsByType<PlayerRole>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var role in roles)
        {
            if (role == null)
            {
                continue;
            }

            if (role.Object != null && role.Object.HasInputAuthority)
            {
                return role;
            }
        }

        return null;
    }
}
