using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerElimination))]
public class DeathmatchMeleeAttack : NetworkBehaviour
{
    [SerializeField] private float attackRange = 2.2f;
    [SerializeField] private float attackRadius = 0.45f;
    [SerializeField] private float attackCooldown = 0.6f;
    [SerializeField] private float attackActiveDuration = 0.22f;
    [SerializeField] private float attackMovementLockDuration = 0.9f;
    [SerializeField] private LayerMask attackMask = -1;
    [SerializeField, Range(-1f, 1f)] private float frontDotThreshold = 0.1f;
    [SerializeField] private bool enableDebugLogs = true;

    private PlayerElimination elimination;
    private FusionThirdPersonMotor motor;
    private CharacterController characterController;
    private Animator animator;
    private int animIDAttack;
    private float nextAttackTime;
    private float localMoveLockUntil;
    private PlayerInput playerInput;

    [Networked] private NetworkBool NetAttackActive { get; set; }
    [Networked] private TickTimer NetAttackTimer { get; set; }

    private void Awake()
    {
        elimination = GetComponent<PlayerElimination>();
        motor = GetComponent<FusionThirdPersonMotor>();
        characterController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();
        animIDAttack = Animator.StringToHash("Attack");
    }

    private void Update()
    {
        if (Object == null || Object.HasInputAuthority == false)
        {
            return;
        }

        if (GameModeRuntime.IsDeathmatch == false)
        {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGroupDanceActive)
        {
            return;
        }

        if (elimination != null && elimination.Object != null && elimination.Object.IsValid && elimination.IsEliminated)
        {
            return;
        }

        if (WasAttackPressedThisFrame() == false)
        {
            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        if (IsLocalAttackMoveLocked())
        {
            return;
        }

        if (CanStartAttackNow() == false)
        {
            return;
        }

        nextAttackTime = Time.time + Mathf.Max(0.05f, attackCooldown);
        ApplyLocalMoveLock();

        if (Object.HasStateAuthority)
        {
            StartAttackState();
        }
        else
        {
            RpcRequestMeleeAttack();
        }

        if (enableDebugLogs)
        {
            Debug.Log($"[Deathmatch] Melee input accepted: player={Object.InputAuthority.RawEncoded}, hasStateAuthority={Object.HasStateAuthority}");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RpcRequestMeleeAttack()
    {
        StartAttackState();
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || Object.HasStateAuthority == false)
        {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGroupDanceActive)
        {
            NetAttackActive = false;
            return;
        }

        if (NetAttackActive == false)
        {
            return;
        }

        if (NetAttackTimer.ExpiredOrNotRunning(Runner))
        {
            NetAttackActive = false;
            return;
        }

        ExecuteAttack();
    }

    private void StartAttackState()
    {
        if (Object == null || Object.HasStateAuthority == false)
        {
            return;
        }

        if (Runner == null)
        {
            return;
        }

        if (CanStartAttackNow() == false)
        {
            return;
        }

        NetAttackActive = true;
        NetAttackTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0.05f, attackActiveDuration));
        if (motor == null)
        {
            motor = GetComponent<FusionThirdPersonMotor>();
        }
        if (motor != null)
        {
            float lockDuration = Mathf.Max(0.05f, attackMovementLockDuration, attackCooldown);
            motor.RequestMeleeMovementLock(lockDuration);
        }
        RpcPlayAttackAnimation();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcPlayAttackAnimation()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            return;
        }

        animator.SetTrigger(animIDAttack);
        ApplyLocalMoveLock();
    }

    public bool IsLocalAttackMoveLocked()
    {
        return Time.time < localMoveLockUntil;
    }

    private void ApplyLocalMoveLock()
    {
        float lockDuration = Mathf.Max(0.05f, attackMovementLockDuration, attackCooldown);
        localMoveLockUntil = Mathf.Max(localMoveLockUntil, Time.time + lockDuration);
    }

    private bool CanStartAttackNow()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGroupDanceActive)
        {
            return false;
        }

        if (elimination != null && elimination.Object != null && elimination.Object.IsValid && elimination.IsEliminated)
        {
            return false;
        }

        if (IsGroundedNow() == false)
        {
            return false;
        }

        return true;
    }

    private bool IsGroundedNow()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (characterController != null)
        {
            return characterController.isGrounded;
        }

        return true;
    }

    private void ExecuteAttack()
    {
        var match = FindFirstObjectByType<DeathmatchMatchController>();
        if (match == null || match.IsEnabled == false)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[Deathmatch] Melee ignored: DeathmatchMatchController missing or disabled.");
            }
            return;
        }

        int localRaw = Object != null ? Object.InputAuthority.RawEncoded : 0;
        if (TryBuildAim(out Vector3 origin, out Vector3 direction) == false)
        {
            return;
        }

        if (TryFindPlayerTarget(origin, direction, localRaw, out int victimRaw))
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[Deathmatch] Melee player hit: attacker={localRaw} victim={victimRaw}");
            }

            match.RpcRegisterPlayerKill(localRaw, victimRaw);
            NetAttackActive = false;
            return;
        }

        if (TryFindNpcTarget(origin, direction, out NPCController npc))
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[Deathmatch] Melee NPC hit: attacker={localRaw}");
            }

            if (npc != null && npc.IsDead == false)
            {
                npc.RpcTriggerDead();
            }

            match.RpcRegisterNpcPenalty(localRaw);
            NetAttackActive = false;
            return;
        }

        if (enableDebugLogs)
        {
            Debug.Log($"[Deathmatch] Melee miss: attacker={localRaw}, origin={origin}, direction={direction}");
        }
    }

    private bool TryFindPlayerTarget(Vector3 origin, Vector3 direction, int selfRaw, out int victimRaw)
    {
        victimRaw = 0;
        var hits = Physics.OverlapCapsule(
            origin,
            origin + direction.normalized * attackRange,
            Mathf.Max(0.05f, attackRadius),
            attackMask,
            QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        float bestDistance = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (col == null)
            {
                continue;
            }

            var role = col.GetComponentInParent<PlayerRole>();
            if (role == null || role.Object == null)
            {
                continue;
            }

            int raw = role.Object.InputAuthority.RawEncoded;
            if (raw == selfRaw)
            {
                continue;
            }

            var targetElimination = role.GetComponent<PlayerElimination>();
            if (targetElimination != null && targetElimination.Object != null && targetElimination.Object.IsValid && targetElimination.IsEliminated)
            {
                continue;
            }

            Vector3 toTarget = role.transform.position - origin;
            float distance = toTarget.magnitude;
            if (distance < 0.05f)
            {
                continue;
            }

            float dot = Vector3.Dot(direction.normalized, toTarget / distance);
            if (dot < frontDotThreshold)
            {
                continue;
            }

            if (distance > attackRange + 0.1f)
            {
                continue;
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
                victimRaw = raw;
            }
        }

        return victimRaw != 0;
    }

    private bool TryFindNpcTarget(Vector3 origin, Vector3 direction, out NPCController targetNpc)
    {
        targetNpc = null;
        var hits = Physics.OverlapCapsule(
            origin,
            origin + direction.normalized * attackRange,
            Mathf.Max(0.05f, attackRadius),
            attackMask,
            QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        float bestDistance = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (col == null)
            {
                continue;
            }

            var npc = col.GetComponentInParent<NPCController>();
            if (npc == null || npc.IsDead)
            {
                continue;
            }

            Vector3 toTarget = npc.transform.position - origin;
            float distance = toTarget.magnitude;
            if (distance < 0.05f)
            {
                continue;
            }

            float dot = Vector3.Dot(direction.normalized, toTarget / distance);
            if (dot < frontDotThreshold)
            {
                continue;
            }

            if (distance > attackRange + 0.1f)
            {
                continue;
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
                targetNpc = npc;
            }
        }

        return targetNpc != null;
    }

    private bool TryBuildAim(out Vector3 origin, out Vector3 direction)
    {
        origin = transform.position + Vector3.up * 1.2f;
        direction = transform.forward;

        Camera cam = Camera.main;
        if (cam == null)
        {
            return true;
        }

        if (Object != null && Object.HasInputAuthority)
        {
            direction = cam.transform.forward;
        }
        return true;
    }

    private bool WasAttackPressedThisFrame()
    {
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }

        if (playerInput == null || playerInput.actions == null)
        {
            return false;
        }

        var fire = playerInput.actions.FindAction("Fire", false);
        if (fire != null && fire.WasPressedThisFrame())
        {
            return true;
        }

        var shoot = playerInput.actions.FindAction("Shoot", false);
        if (shoot != null && shoot.WasPressedThisFrame())
        {
            return true;
        }

        return false;
    }
}
