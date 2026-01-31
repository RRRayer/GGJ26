using Fusion;
using UnityEngine;

public class PlayerElimination : NetworkBehaviour
{
    [SerializeField] private FusionThirdPersonMotor motor;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private FusionThirdPersonCamera cameraController;
    [SerializeField] private SpectatorController spectatorController;
    [SerializeField] private Renderer[] bodyRenderers;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask deathGroundLayers = -1;

    [Networked]
    public NetworkBool IsEliminated { get; private set; }

    private PlayerStateManager playerStateManager;
    private bool lastEliminated;
    private int animIDDead;
    private bool snappedToGroundOnDeath;

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

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (bodyRenderers == null || bodyRenderers.Length == 0)
        {
            bodyRenderers = GetComponentsInChildren<Renderer>(true);
        }

        playerStateManager = FindFirstObjectByType<PlayerStateManager>();
        animIDDead = Animator.StringToHash("Dead");
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

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RpcPlayDeadAnimation()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            return;
        }

        animator.SetTrigger(animIDDead);
        snappedToGroundOnDeath = false;
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
                    bodyRenderers[i].enabled = true;
                }
            }
        }

        if (eliminated && animator != null)
        {
            animator.SetTrigger(animIDDead);
            snappedToGroundOnDeath = false;
        }

        if (eliminated && Object != null && Object.HasStateAuthority && playerStateManager != null)
        {
            string playerId = Object.InputAuthority.RawEncoded.ToString();
            playerStateManager.MarkDead(playerId);
        }
    }

    private void LateUpdate()
    {
        if (IsEliminated == false || snappedToGroundOnDeath)
        {
            return;
        }

        Vector3 origin = transform.position + Vector3.up * 1f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 20f, deathGroundLayers, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point;
            snappedToGroundOnDeath = true;
        }
    }
}
