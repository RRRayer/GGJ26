using Fusion;
using UnityEngine;

public class PlayerRole : NetworkBehaviour
{
    [Networked] public NetworkBool IsSeeker { get; private set; }
    [Networked] private NetworkBool RoleAssigned { get; set; }
    [Networked] private int MaskColorIndex { get; set; }
    [Networked] private NetworkBool MaskAssigned { get; set; }

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

        PlayerRef seeker = GetDeterministicSeeker();
        IsSeeker = Object.InputAuthority == seeker;
        RoleAssigned = true;

        if (playerStateManager != null)
        {
            string playerId = Object.InputAuthority.RawEncoded.ToString();
            playerStateManager.RegisterPlayer(playerId, IsSeeker);
            if (Object.HasInputAuthority)
            {
                playerStateManager.SetLocalPlayer(playerId);
            }
        }

        if (IsSeeker == false)
        {
            MaskColorIndex = Random.Range(0, 3);
        }
        else
        {
            MaskColorIndex = -1;
        }

        MaskAssigned = true;
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

    private int GetActivePlayerCount()
    {
        int count = 0;
        foreach (var _ in Runner.ActivePlayers)
        {
            count++;
        }

        return count;
    }

    public bool HasRoleAssigned()
    {
        return RoleAssigned;
    }

    public int GetMaskColorIndex()
    {
        return MaskColorIndex;
    }
}
