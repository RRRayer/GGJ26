using Fusion;
using UnityEngine;

public class PlayerElimination : NetworkBehaviour
{
    [SerializeField] private FusionThirdPersonMotor motor;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private FusionThirdPersonCamera cameraController;
    [SerializeField] private SpectatorController spectatorController;
    [SerializeField] private Renderer[] bodyRenderers;

    [Networked]
    public NetworkBool IsEliminated { get; private set; }

    private PlayerStateManager playerStateManager;
    private bool lastEliminated;

    private void Awake()
    {
        if (motor == null)
        {
            motor = GetComponent<FusionThirdPersonMotor>();
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (cameraController == null)
        {
            cameraController = GetComponent<FusionThirdPersonCamera>();
        }

        if (spectatorController == null)
        {
            spectatorController = GetComponent<SpectatorController>();
        }

        if (bodyRenderers == null || bodyRenderers.Length == 0)
        {
            bodyRenderers = GetComponentsInChildren<Renderer>(true);
        }

        playerStateManager = FindFirstObjectByType<PlayerStateManager>();
    }

    public override void Spawned()
    {
        lastEliminated = IsEliminated;
        ApplyEliminatedState();
    }

    public override void Render()
    {
        if (lastEliminated != IsEliminated)
        {
            lastEliminated = IsEliminated;
            ApplyEliminatedState();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcRequestEliminate()
    {
        if (IsEliminated)
        {
            return;
        }

        IsEliminated = true;
    }

    public void ResetElimination()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            IsEliminated = false;
        }
    }

    private void ApplyEliminatedState()
    {
        bool eliminated = IsEliminated;

        if (motor != null)
        {
            motor.enabled = !eliminated;
        }

        if (characterController != null)
        {
            characterController.enabled = !eliminated;
        }

        if (spectatorController != null)
        {
            spectatorController.enabled = eliminated && Object != null && Object.HasInputAuthority;
        }

        if (cameraController != null)
        {
            cameraController.enabled = !eliminated;
        }

        if (bodyRenderers != null)
        {
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                if (bodyRenderers[i] != null)
                {
                    bodyRenderers[i].enabled = !eliminated;
                }
            }
        }

        if (eliminated && Object != null && Object.HasStateAuthority && playerStateManager != null)
        {
            string playerId = Object.InputAuthority.RawEncoded.ToString();
            playerStateManager.MarkDead(playerId);
        }
    }
}
