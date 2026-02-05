using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerRole : NetworkBehaviour
{
    [Networked] public NetworkBool IsSeeker { get; private set; }
    [Networked] private NetworkBool RoleAssigned { get; set; }
    [Networked] private int MaskColorIndex { get; set; }
    [Networked] private NetworkBool MaskAssigned { get; set; }
    [Networked] private int MaskSeed { get; set; }


    private PlayerStateManager playerStateManager;
    private bool lastIsSeeker;
    private bool lastRoleAssigned;
    private int lastMaskColorIndex = -1;

    private void Awake()
    {
        playerStateManager = FindFirstObjectByType<PlayerStateManager>();
    }

    public override void Spawned()
    {
        TryAssignRole();
    }

    public override void FixedUpdateNetwork()
    {
        if (RoleAssigned == false)
        {
            TryAssignRole();
        }
    }

    public override void Render()
    {
        if (RoleAssigned && MaskAssigned == false)
        {
            return;
        }

        bool changed = lastRoleAssigned != RoleAssigned || lastIsSeeker != IsSeeker || lastMaskColorIndex != MaskColorIndex;
        if (changed)
        {
            lastRoleAssigned = RoleAssigned;
            lastIsSeeker = IsSeeker;
            lastMaskColorIndex = MaskColorIndex;
        }
    }

    private void TryAssignRole()
    {
        if (Object == null || Object.HasStateAuthority == false)
        {
            return;
        }

        if (RoleAssigned)
        {
            return;
        }

        if (Runner == null || GetActivePlayerCount() == 0)
        {
            return;
        }

        if (playerStateManager == null)
        {
            playerStateManager = FindFirstObjectByType<PlayerStateManager>();
        }

        PlayerRef seeker = GetDeterministicSeeker();
        IsSeeker = Object.InputAuthority == seeker;
        RoleAssigned = true;

        if (playerStateManager != null)
        {
            string playerId = Object.InputAuthority.RawEncoded.ToString();
            playerStateManager.RegisterPlayerNetworked(playerId, IsSeeker);
            if (Object.HasInputAuthority)
            {
                playerStateManager.SetLocalPlayer(playerId);
            }
        }

        if (IsSeeker)
        {
            MaskColorIndex = -1;
            if (MaskSeed == 0)
            {
                MaskSeed = Random.Range(1, int.MaxValue);
            }

            MaskAssigned = true;
            return;
        }

        int seed = GetMaskSeed();
        if (seed == 0)
        {
            return;
        }

        MaskColorIndex = GetDeterministicMaskIndex(Object.InputAuthority, seed);
        MaskAssigned = true;
    }

    private int GetActivePlayerCount()
    {
        int count = 0;
        foreach (var _ in Runner.ActivePlayers)
        {
            count++;
        }

        return count;
    }

    private int GetDeterministicMaskIndex(PlayerRef player, int seed)
    {
        var players = new List<PlayerRef>();
        foreach (var item in Runner.ActivePlayers)
        {
            players.Add(item);
        }

        players.Sort((a, b) => a.RawEncoded.CompareTo(b.RawEncoded));

        if (players.Count == 0)
        {
            return 0;
        }

        PlayerRef seeker = players[0];
        players.Remove(seeker);

        Shuffle(players, seed);

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == player)
            {
                return i % 3;
            }
        }

        return 0;
    }

    private PlayerRef GetDeterministicSeeker()
    {
        PlayerRef chosen = default;
        bool hasValue = false;
        foreach (var player in Runner.ActivePlayers)
        {
            if (hasValue == false || player.RawEncoded < chosen.RawEncoded)
            {
                chosen = player;
                hasValue = true;
            }
        }

        return chosen;
    }

    private int GetMaskSeed()
    {
        if (Runner == null)
        {
            return 0;
        }

        PlayerRef seeker = GetDeterministicSeeker();
        if (Runner.TryGetPlayerObject(seeker, out var seekerObject) == false || seekerObject == null)
        {
            return 0;
        }

        var role = seekerObject.GetComponent<PlayerRole>();
        if (role == null)
        {
            return 0;
        }

        return role.MaskSeed;
    }

    private void Shuffle(List<PlayerRef> list, int seed)
    {
        var rng = new System.Random(seed);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    public bool HasRoleAssigned()
    {
        return RoleAssigned;
    }

    public int GetMaskColorIndex()
    {
        return MaskColorIndex;
    }

    public bool TrySetMaskColorIndex(int newIndex)
    {
        if (Object == null || Object.HasStateAuthority == false)
        {
            return false;
        }

        if (IsSeeker)
        {
            return false;
        }

        MaskColorIndex = newIndex;
        MaskAssigned = true;
        return true;
    }

    public void RequestMaskColorChange(int newIndex)
    {
        if (Object == null)
        {
            return;
        }

        if (Object.HasStateAuthority)
        {
            TrySetMaskColorIndex(newIndex);
            return;
        }

        RpcRequestSetMaskColorIndex(newIndex);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RpcRequestSetMaskColorIndex(int newIndex)
    {
        if (IsSeeker)
        {
            return;
        }

        MaskColorIndex = newIndex;
        MaskAssigned = true;
    }
}
