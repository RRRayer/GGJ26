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
    [SerializeField] private GameObject spectatorRigPrefab;
    [SerializeField] private LayerMask deathGroundLayers = -1;

    [Networked]
    public NetworkBool IsEliminated { get; private set; }

    private PlayerStateManager playerStateManager;
    private PlayerRole _playerRole;
    private bool lastEliminated;
    private int animIDDead;
    private bool snappedToGroundOnDeath;
    private GameObject spectatorInstance;
    public static System.Action<PlayerElimination> OnAnyEliminated;

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
        _playerRole = GetComponent<PlayerRole>();
        animIDDead = Animator.StringToHash("Dead");
    }

    public override void Spawned()
    {
        if (playerStateManager == null)
        {
            playerStateManager = FindFirstObjectByType<PlayerStateManager>();
        }
        lastEliminated = IsEliminated;
        ApplyEliminatedState();
    }

    public override void Render()
    {
        if (lastEliminated != IsEliminated)
        {
            bool transitionedToEliminated = lastEliminated == false && IsEliminated;
            lastEliminated = IsEliminated;
            ApplyEliminatedState();
            if (transitionedToEliminated)
            {
                OnAnyEliminated?.Invoke(this);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcRequestEliminate()
    {
        Debug.Log($"[PlayerElimination] RpcRequestEliminate on {name} stateAuth={Object != null && Object.HasStateAuthority}");
        if (IsEliminated)
        {
            return;
        }

        IsEliminated = true;
        if (playerStateManager != null)
        {
            string playerId = Object.InputAuthority.RawEncoded.ToString();
            playerStateManager.MarkDeadNetworked(playerId);
        }
        else
        {
            Debug.LogWarning("[PlayerElimination] playerStateManager is null during RpcRequestEliminate.");
        }
    }

    public void ApplyEliminatedStateImmediate()
    {
        ApplyEliminatedState();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcRequestPlayDeadAnimation()
    {
        RpcPlayDeadAnimationAll();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcPlayDeadAnimationAll()
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
            spectatorController.enabled = false;
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

        if (Object != null && Object.HasInputAuthority)
        {
            if (eliminated)
            {
                EnsureSpectatorRig();
                if (_playerRole != null && !_playerRole.IsSeeker)
                {
                    var deadUI = FindFirstObjectByType<UIDead>();
                    if (deadUI != null)
                    {
                        deadUI.ShowDeadUI();
                    }
                }
            }
            else
            {
                CleanupSpectatorRig();
            }
        }

        if (eliminated && animator != null)
        {
            animator.SetTrigger(animIDDead);
            snappedToGroundOnDeath = false;
        }

        if (eliminated && playerStateManager == null)
        {
            playerStateManager = FindFirstObjectByType<PlayerStateManager>();
        }

        if (eliminated && playerStateManager != null)
        {
            string playerId = Object.InputAuthority.RawEncoded.ToString();
            playerStateManager.MarkDeadNetworked(playerId);
        }
    }

    private void EnsureSpectatorRig()
    {
        if (spectatorRigPrefab == null)
        {
            if (spectatorController != null)
            {
                spectatorController.enabled = true;
            }
            return;
        }

        if (spectatorInstance != null)
        {
            return;
        }

        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;
        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            position = mainCamera.transform.position;
            rotation = mainCamera.transform.rotation;
        }

        spectatorInstance = Instantiate(spectatorRigPrefab, position, rotation);
        var spectator = spectatorInstance.GetComponent<SpectatorController>();
        if (spectator == null)
        {
            spectator = spectatorInstance.AddComponent<SpectatorController>();
        }
        spectator.enabled = true;
    }

    private void CleanupSpectatorRig()
    {
        if (spectatorInstance != null)
        {
            Destroy(spectatorInstance);
            spectatorInstance = null;
        }

        if (spectatorController != null)
        {
            spectatorController.enabled = false;
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
