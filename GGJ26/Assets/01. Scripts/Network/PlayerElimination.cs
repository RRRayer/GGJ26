using Fusion;
using UnityEngine;

public class PlayerElimination : NetworkBehaviour
{
    [SerializeField] private FusionThirdPersonMotor motor;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private FusionThirdPersonCamera cameraController;
    [SerializeField] private SpectatorController spectatorController;
    [SerializeField] private SpectatorSabotageController spectatorSabotageController;
    [SerializeField] private Renderer[] bodyRenderers;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject spectatorRigPrefab;
    [SerializeField] private LayerMask deathGroundLayers = -1;
    [SerializeField] private float deathGroundSnapDuration = 2.5f;
    [SerializeField] private float deathGroundSnapInterval = 0.08f;
    [SerializeField] private float deathGroundOffset = 0.02f;

    [Networked]
    public NetworkBool IsEliminated { get; private set; }

    private PlayerStateManager playerStateManager;
    private PlayerRole _playerRole;
    private bool lastEliminated;
    private int animIDDead;
    private bool snappedToGroundOnDeath;
    private GameObject spectatorInstance;
    private float deathGroundSnapUntilTime;
    private float nextDeathGroundSnapTime;
    private bool hasSpawned;

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

        if (spectatorSabotageController == null)
        {
            spectatorSabotageController = GetComponent<SpectatorSabotageController>();
            if (spectatorSabotageController == null)
            {
                spectatorSabotageController = gameObject.AddComponent<SpectatorSabotageController>();
            }
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
        hasSpawned = true;
        if (playerStateManager == null)
        {
            playerStateManager = FindFirstObjectByType<PlayerStateManager>();
        }
        lastEliminated = IsEliminated;
        ApplyEliminatedState();
    }

    public override void Render()
    {
        if (CanAccessNetworkedState() == false)
        {
            return;
        }

        if (lastEliminated != IsEliminated)
        {
            lastEliminated = IsEliminated;
            ApplyEliminatedState();
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
        deathGroundSnapUntilTime = Time.time + deathGroundSnapDuration;
        nextDeathGroundSnapTime = 0f;
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
        bool eliminated = CanAccessNetworkedState() && IsEliminated;

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

        if (spectatorSabotageController != null)
        {
            spectatorSabotageController.enabled = eliminated;
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
                if (spectatorSabotageController != null && spectatorRigPrefab == null && spectatorController != null)
                {
                    spectatorSabotageController.SetSpectatorAimOrigin(spectatorController.transform);
                }
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
            deathGroundSnapUntilTime = Time.time + deathGroundSnapDuration;
            nextDeathGroundSnapTime = 0f;
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
                if (spectatorSabotageController != null)
                {
                    spectatorSabotageController.SetSpectatorAimOrigin(spectatorController.transform);
                }
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
        if (spectatorSabotageController != null)
        {
            spectatorSabotageController.SetSpectatorAimOrigin(spectator.transform);
        }
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

        if (spectatorSabotageController != null)
        {
            spectatorSabotageController.SetSpectatorAimOrigin(null);
        }
    }

    private void LateUpdate()
    {
        if (CanAccessNetworkedState() == false)
        {
            return;
        }

        if (IsEliminated == false)
        {
            return;
        }

        if (Time.time < nextDeathGroundSnapTime || Time.time > deathGroundSnapUntilTime)
        {
            return;
        }

        nextDeathGroundSnapTime = Time.time + deathGroundSnapInterval;

        if (TrySnapToGround())
        {
            snappedToGroundOnDeath = true;
        }
    }

    private bool TrySnapToGround()
    {
        Vector3 origin = transform.position + Vector3.up * 2.0f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 60f, deathGroundLayers, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        bool hasValidHit = false;
        RaycastHit selectedHit = default;
        float lowestY = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit.collider == null)
            {
                continue;
            }

            // Ignore self-colliders so we never snap to our own body.
            if (hit.collider.transform != null && hit.collider.transform.root == transform.root)
            {
                continue;
            }

            if (hit.point.y < lowestY)
            {
                lowestY = hit.point.y;
                selectedHit = hit;
                hasValidHit = true;
            }
        }

        if (hasValidHit)
        {
            Vector3 snapped = selectedHit.point + Vector3.up * deathGroundOffset;
            if ((transform.position - snapped).sqrMagnitude > 0.0001f)
            {
                transform.position = snapped;
            }

            return true;
        }

        return false;
    }

    private bool CanAccessNetworkedState()
    {
        return hasSpawned && Object != null && Runner != null;
    }
}
