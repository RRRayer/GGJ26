using System.Collections.Generic;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class DeathmatchMatchController : NetworkBehaviour
{
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private float matchDurationSeconds = 180f;
    [SerializeField] private float initialSafeZoneRadius = 45f;
    [SerializeField] private Vector3 safeZoneCenter = Vector3.zero;
    [SerializeField] private float zoneTickInterval = 3f;
    [SerializeField] private int zoneLifeLossPerTick = 1;
    [SerializeField] private DeathmatchOutsideZoneWorldTint outsideZoneWorldTint;

    [Networked] public float NetRemainingSeconds { get; private set; }
    [Networked] public float NetSafeZoneRadius { get; private set; }
    [Networked] public NetworkBool NetIsFinished { get; private set; }

    private readonly Dictionary<int, DeathmatchRuntimeState> states = new Dictionary<int, DeathmatchRuntimeState>();
    private TickTimer zoneTickTimer;

    private sealed class DeathmatchRuntimeState
    {
        public int Lives = 3;
        public int Kills;
        public int LifeLostTotal;
        public bool IsEliminated;
    }

    public bool IsEnabled => GameModeRuntime.IsDeathmatch;
    public Vector3 SafeZoneCenter => safeZoneCenter;

    public override void Spawned()
    {
        if (GameModeRuntime.IsDeathmatch == false)
        {
            enabled = false;
            return;
        }

        if (outsideZoneWorldTint == null)
        {
            outsideZoneWorldTint = GetComponent<DeathmatchOutsideZoneWorldTint>();
        }

        if (outsideZoneWorldTint == null)
        {
            outsideZoneWorldTint = gameObject.AddComponent<DeathmatchOutsideZoneWorldTint>();
        }

        outsideZoneWorldTint.Bind(this);

        var oldVisualizer = GetComponent<DeathmatchSafeZoneVisualizer>();
        if (oldVisualizer != null)
        {
            oldVisualizer.enabled = false;
        }

        var oldScreenFx = GetComponent<DeathmatchOutsideZoneScreenEffect>();
        if (oldScreenFx != null)
        {
            oldScreenFx.enabled = false;
        }

        if (Object != null && Object.HasStateAuthority)
        {
            NetRemainingSeconds = matchDurationSeconds;
            NetSafeZoneRadius = initialSafeZoneRadius;
            NetIsFinished = false;
            zoneTickTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0.1f, zoneTickInterval));
            RegisterAllCurrentPlayers();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GameModeRuntime.IsDeathmatch == false || NetIsFinished)
        {
            return;
        }

        if (Object == null || Object.HasStateAuthority == false)
        {
            return;
        }

        RegisterAllCurrentPlayers();
        bool groupDanceActive = IsGroupDanceActive();
        if (groupDanceActive == false)
        {
            UpdateMatchTimerAndZone();
            TickZoneDamage();
        }
        TryResolveWinner();
    }

    public bool IsPlayerEliminated(PlayerRef player)
    {
        if (states.TryGetValue(player.RawEncoded, out var state) == false)
        {
            return false;
        }

        return state.IsEliminated;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcRegisterPlayerKill(int attackerRaw, int victimRaw)
    {
        if (GameModeRuntime.IsDeathmatch == false || NetIsFinished)
        {
            return;
        }

        EnsurePlayer(attackerRaw);
        EnsurePlayer(victimRaw);

        if (attackerRaw == victimRaw)
        {
            return;
        }

        ApplyLifeSetToZero(victimRaw);

        var attacker = states[attackerRaw];
        if (attacker.IsEliminated == false)
        {
            attacker.Kills += 1;
            attacker.Lives = 3;
        }

        RpcSyncState(attackerRaw, attacker.Lives, attacker.Kills, attacker.LifeLostTotal, attacker.IsEliminated);
        TryResolveWinner();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcRegisterNpcPenalty(int attackerRaw)
    {
        if (GameModeRuntime.IsDeathmatch == false || NetIsFinished)
        {
            return;
        }

        ApplyLifeDelta(attackerRaw, -Mathf.Max(1, zoneLifeLossPerTick));
        TryResolveWinner();
    }

    private void RegisterAllCurrentPlayers()
    {
        if (Runner == null)
        {
            return;
        }

        foreach (var player in Runner.ActivePlayers)
        {
            EnsurePlayer(player.RawEncoded);
        }
    }

    private void EnsurePlayer(int raw)
    {
        if (states.ContainsKey(raw))
        {
            return;
        }

        states[raw] = new DeathmatchRuntimeState();
        RpcSyncState(raw, 3, 0, 0, false);
    }

    private void UpdateMatchTimerAndZone()
    {
        NetRemainingSeconds = Mathf.Max(0f, NetRemainingSeconds - Runner.DeltaTime);
        float elapsed = Mathf.Max(0f, matchDurationSeconds - NetRemainingSeconds);
        float t = matchDurationSeconds > 0.0001f ? Mathf.Clamp01(elapsed / matchDurationSeconds) : 1f;
        NetSafeZoneRadius = Mathf.Max(0f, initialSafeZoneRadius * (1f - t));
    }

    private void TickZoneDamage()
    {
        if (zoneTickTimer.ExpiredOrNotRunning(Runner) == false)
        {
            return;
        }

        zoneTickTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0.1f, zoneTickInterval));
        var outsidePlayers = new List<int>();

        foreach (var kv in states)
        {
            int raw = kv.Key;
            var state = kv.Value;
            if (state.IsEliminated)
            {
                continue;
            }

            if (TryGetPlayerPosition(raw, out var pos) == false)
            {
                continue;
            }

            float sqr = (new Vector2(pos.x - safeZoneCenter.x, pos.z - safeZoneCenter.z)).sqrMagnitude;
            if (sqr > NetSafeZoneRadius * NetSafeZoneRadius)
            {
                outsidePlayers.Add(raw);
            }
        }

        for (int i = 0; i < outsidePlayers.Count; i++)
        {
            ApplyLifeDelta(outsidePlayers[i], -Mathf.Max(1, zoneLifeLossPerTick));
        }
    }

    private bool TryGetPlayerPosition(int raw, out Vector3 position)
    {
        position = Vector3.zero;
        if (Runner == null)
        {
            return false;
        }

        foreach (var player in Runner.ActivePlayers)
        {
            if (player.RawEncoded != raw)
            {
                continue;
            }

            if (Runner.TryGetPlayerObject(player, out var obj) == false || obj == null)
            {
                return false;
            }

            position = obj.transform.position;
            return true;
        }

        return false;
    }

    private void ApplyLifeDelta(int raw, int delta)
    {
        EnsurePlayer(raw);
        var state = states[raw];
        if (state.IsEliminated)
        {
            return;
        }

        if (delta < 0)
        {
            int loss = Mathf.Min(state.Lives, Mathf.Abs(delta));
            state.LifeLostTotal += loss;
            state.Lives = Mathf.Max(0, state.Lives - loss);
        }
        else if (delta > 0)
        {
            state.Lives = Mathf.Min(3, state.Lives + delta);
        }

        if (state.Lives <= 0)
        {
            ApplyLifeSetToZero(raw);
            return;
        }

        RpcSyncState(raw, state.Lives, state.Kills, state.LifeLostTotal, false);
    }

    private void ApplyLifeSetToZero(int raw)
    {
        EnsurePlayer(raw);
        var state = states[raw];
        if (state.IsEliminated)
        {
            return;
        }

        if (state.Lives > 0)
        {
            state.LifeLostTotal += state.Lives;
        }

        state.Lives = 0;
        state.IsEliminated = true;
        RpcSyncState(raw, state.Lives, state.Kills, state.LifeLostTotal, true);
        RpcEliminatePlayer(raw);
    }

    private void TryResolveWinner()
    {
        if (NetIsFinished)
        {
            return;
        }

        int aliveCount = 0;
        int aliveRaw = 0;
        foreach (var kv in states)
        {
            if (kv.Value.IsEliminated == false)
            {
                aliveCount++;
                aliveRaw = kv.Key;
            }
        }

        if (aliveCount == 1)
        {
            FinishMatch(aliveRaw, false);
            return;
        }

        if (aliveCount > 1)
        {
            if (NetRemainingSeconds <= 0f)
            {
                ResolveTieBreakerAtTimeout();
            }
            return;
        }

        ResolveTieBreakerAtTimeout();
    }

    private void ResolveTieBreakerAtTimeout()
    {
        int bestKills = int.MinValue;
        int bestLifeLost = int.MaxValue;
        int winnerRaw = 0;
        int winnerCount = 0;

        foreach (var kv in states)
        {
            int raw = kv.Key;
            var state = kv.Value;

            bool better = state.Kills > bestKills || (state.Kills == bestKills && state.LifeLostTotal < bestLifeLost);
            bool same = state.Kills == bestKills && state.LifeLostTotal == bestLifeLost;

            if (better)
            {
                bestKills = state.Kills;
                bestLifeLost = state.LifeLostTotal;
                winnerRaw = raw;
                winnerCount = 1;
            }
            else if (same)
            {
                winnerCount++;
            }
        }

        if (winnerCount == 1)
        {
            FinishMatch(winnerRaw, false);
            return;
        }

        FinishMatch(0, true);
    }

    private void FinishMatch(int winnerRaw, bool drawAllLose)
    {
        if (NetIsFinished)
        {
            return;
        }

        NetIsFinished = true;
        RpcFinishMatch(winnerRaw, drawAllLose, NetRemainingSeconds);
    }

    private static bool IsGroupDanceActive()
    {
        return GameManager.Instance != null && GameManager.Instance.IsGroupDanceActive;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcSyncState(int raw, int lives, int kills, int lifeLostTotal, bool isEliminated)
    {
        EnsurePlayer(raw);
        var state = states[raw];
        state.Lives = lives;
        state.Kills = kills;
        state.LifeLostTotal = lifeLostTotal;
        state.IsEliminated = isEliminated;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcEliminatePlayer(int raw)
    {
        if (Runner == null)
        {
            return;
        }

        foreach (var player in Runner.ActivePlayers)
        {
            if (player.RawEncoded != raw)
            {
                continue;
            }

            if (Runner.TryGetPlayerObject(player, out var obj) == false || obj == null)
            {
                return;
            }

            var elimination = obj.GetComponent<PlayerElimination>();
            if (elimination == null || elimination.IsEliminated)
            {
                return;
            }

            elimination.RpcRequestPlayDeadAnimation();
            elimination.RpcRequestEliminate();
            return;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcFinishMatch(int winnerRaw, bool drawAllLose, float remainingSeconds)
    {
        var result = FindFirstObjectByType<GameResultController>();
        if (result != null)
        {
            result.EndGameDeathmatch(winnerRaw, drawAllLose, remainingSeconds);
        }

        if (enableDebugLogs)
        {
            Debug.Log($"[Deathmatch] Finish. winnerRaw={winnerRaw}, drawAllLose={drawAllLose}, remaining={remainingSeconds:F1}");
        }
    }
}
