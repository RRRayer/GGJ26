using Fusion;
using StarterAssets;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FusionThirdPersonMotor : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float sprintSpeed = 6f;
    [SerializeField] private float rotationSmoothTime = 0.12f;
    [SerializeField] private float seekerMoveSpeed = 5f;
    [SerializeField] private float seekerSprintSpeed = 7f;
    [SerializeField] private float gravity = -15f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float coyoteTime = 0.1f;
    [Header("Grounded Check")]
    [SerializeField] private float groundedOffset = -0.14f;
    [SerializeField] private float groundedRadius = 0.28f;
    [SerializeField] private LayerMask groundLayers = 1;
    [SerializeField] private bool updateAnimator = true;
    [Header("Seeker NPC 춤 명령")]
    // Seeker NPC 춤 명령 설정을 위한 ScriptableObject입니다.
    [SerializeField] private SeekerNpcDanceCommandSettingsSO npcDanceCommandSettings;
    // Seeker NPC 춤 명령 반경입니다. (ScriptableObject 설정이 없을 경우 사용)
    [SerializeField] private float npcDanceCommandRadius = 8f;
    // Seeker NPC 춤 지속 시간입니다. (ScriptableObject 설정이 없을 경우 사용)
    [SerializeField] private float npcDanceDuration = 2.5f;
    // Seeker NPC 춤 명령 재사용 대기 시간입니다. (ScriptableObject 설정이 없을 경우 사용)
    [SerializeField] private float npcDanceCommandCooldown = 0.5f;

    // --- FIX: REMOVED [Networked] attribute from physics-related properties ---
    // These properties are now local to the State Authority.
    // Animator values for other clients are calculated in Render() based on the NetworkTransform's interpolated movement.
    private float verticalVelocity;
    private float horizontalSpeed;
    private float inputMagnitude;
    
    // This property is an infrequent event, so it's fine to keep it networked.
    [Networked] private NetworkBool NetIsDancing { get; set; }
    [Networked] private int NetDanceIndex { get; set; }
    [Networked] private TickTimer SabotageStunTimer { get; set; }

    private CharacterController controller;
    private Animator animator;
    private bool hasAnimator;
    private int animIDSpeed;
    private int animIDGrounded;
    private int animIDJump;
    private int animIDFreeFall;
    private int animIDMotionSpeed;
    private int animIDStartDance;
    private int animIDStopDance;
    private int animIDDanceIndex;
    private float rotationVelocity;
    private float lastJumpPressedTime = -10f;
    private float lastGroundedTime = -10f;
    private Camera mainCamera;
    private bool isGrounded; // This is now a simple local bool for the State Authority
    private PlayerRole role;
    private int lastDanceIndex = -1;
    private PlayerElimination elimination;
    // 다음 NPC 춤 명령을 내릴 수 있는 시간입니다.
    public float NextNpcDanceCommandTime { get; private set; }
    // DanceEventPublisher 인스턴스 참조입니다.
    private DanceEventPublisher danceEventPublisher;
    
    // --- FIX: Added for local animation state calculation ---
    private Vector3 lastRenderPosition;
    private bool lastNetIsDancing;
    private int lastNetDanceIndex;
    private Vector3 sabotageKnockbackVelocity;

    // Seeker NPC 춤 명령 반경을 반환합니다. ScriptableObject 설정이 우선됩니다.
    private float NpcDanceRadius => npcDanceCommandSettings != null ? npcDanceCommandSettings.radius : npcDanceCommandRadius;
    // Seeker NPC 춤 지속 시간을 반환합니다. ScriptableObject 설정이 우선됩니다.
    private float NpcDanceDuration => npcDanceCommandSettings != null ? npcDanceCommandSettings.duration : npcDanceDuration;
    // Seeker NPC 춤 명령 재사용 대기 시간을 반환합니다. ScriptableObject 설정이 우선됩니다.
    private float NpcDanceCooldown => npcDanceCommandSettings != null ? npcDanceCommandSettings.cooldown : npcDanceCommandCooldown;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main;
        role = GetComponent<PlayerRole>();
        elimination = GetComponent<PlayerElimination>();
        if (mainCamera == null)
        {
            var cameraObject = GameObject.FindGameObjectWithTag("MainCamera");
            if (cameraObject != null)
            {
                mainCamera = cameraObject.GetComponent<Camera>();
            }
        }
        if (updateAnimator)
        {
            hasAnimator = TryGetComponent(out animator);
            animIDSpeed = Animator.StringToHash("Speed");
            animIDGrounded = Animator.StringToHash("Grounded");
            animIDJump = Animator.StringToHash("Jump");
            animIDFreeFall = Animator.StringToHash("FreeFall");
            animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            animIDStartDance = Animator.StringToHash("StartDance");
            animIDStopDance = Animator.StringToHash("StopDance");
            animIDDanceIndex = Animator.StringToHash("DanceIndex");
        }

        // 씬에서 DanceEventPublisher 인스턴스를 찾아 참조합니다.
        danceEventPublisher = FindFirstObjectByType<DanceEventPublisher>();
    }
    
    // --- FIX: Added Spawned() to initialize render position ---
    public override void Spawned()
    {
	    base.Spawned();
	    lastRenderPosition = transform.position;
        lastNetIsDancing = NetIsDancing;
        lastNetDanceIndex = NetDanceIndex;
        if (NetIsDancing)
        {
            ApplyDanceStart();
        }
    }

    public void StartDance(int danceIndex)
    {
        if (elimination != null && elimination.IsEliminated)
        {
            return;
        }

        if (hasAnimator == false) return;

        NetDanceIndex = danceIndex;
        NetIsDancing = true;
        ApplyDanceStart();
    }

    public void StopDance()
    {
        if (elimination != null && elimination.IsEliminated)
        {
            return;
        }

        if (hasAnimator == false) return;

        NetDanceIndex = -1;
        NetIsDancing = false;
        ApplyDanceStop();
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority == false)
        {
            return;
        }

        if (controller == null)
        {
            return;
        }

        if (elimination != null && elimination.IsEliminated)
        {
            if (NetIsDancing)
            {
                NetIsDancing = false;
                ApplyDanceStop();
            }
            return;
        }

        if (IsSabotageStunned())
        {
            isGrounded = CheckGrounded();
            if (isGrounded)
            {
                lastGroundedTime = Runner.SimulationTime;
            }

            Vector3 knockbackHorizontal = sabotageKnockbackVelocity;
            ApplyGravity(knockbackHorizontal, false);
            sabotageKnockbackVelocity = Vector3.Lerp(sabotageKnockbackVelocity, Vector3.zero, Runner.DeltaTime * 12f);
            inputMagnitude = 0f;
            horizontalSpeed = new Vector3(knockbackHorizontal.x, 0f, knockbackHorizontal.z).magnitude;
            return;
        }

        if (GetInput(out PlayerInputData input) == false)
        {
            if (NetIsDancing)
            {
                NetIsDancing = false;
                ApplyDanceStop();
                lastDanceIndex = -1;
            }
            ApplyGravity(Vector3.zero);
            return;
        }

        bool lockMovement = GameManager.Instance != null && GameManager.Instance.IsGroupDanceActive;
        // Seeker NPC dance command (disabled during group dance).
        if (lockMovement == false && input.npcDanceCommand && role != null && role.IsSeeker && Runner.SimulationTime >= NextNpcDanceCommandTime)
        {
            NextNpcDanceCommandTime = Runner.SimulationTime + NpcDanceCooldown;
            // DanceEventPublisher가 없으면 씬에서 찾아 할당합니다.
            if (danceEventPublisher == null)
            {
                danceEventPublisher = FindFirstObjectByType<DanceEventPublisher>();
            }

            // DanceEventPublisher를 통해 Seeker NPC 춤을 요청합니다.
            if (danceEventPublisher != null)
            {
                danceEventPublisher.RequestSeekerNpcDance(transform.position, NpcDanceRadius, NpcDanceDuration);
            }
        }

        // Hold-to-dance (edge-triggered to avoid retriggering every tick).
        if (input.danceIndex != lastDanceIndex)
        {
            if (input.danceIndex == -1)
            {
                StopDance();
            }
            else
            {
                StartDance(input.danceIndex);
            }

            lastDanceIndex = input.danceIndex;
        }
        else if (input.danceIndex == -1 && NetIsDancing)
        {
            // Safety: if input says stop but state didn't update, force stop once.
            StopDance();
        }

        if (lockMovement)
        {
            isGrounded = CheckGrounded();
            if (isGrounded)
            {
                lastGroundedTime = Runner.SimulationTime;
            }

            // Keep gravity while movement is locked so airborne players can land naturally.
            Vector3 preLockPosition = transform.position;
            ApplyGravity(Vector3.zero, false);

            // Hard-lock horizontal position during group dance to prevent tiny drift from physics/contacts.
            Vector3 postLockPosition = transform.position;
            transform.position = new Vector3(preLockPosition.x, postLockPosition.y, preLockPosition.z);
            rotationVelocity = 0f;
            inputMagnitude = 0f;
            horizontalSpeed = 0f;
            lastJumpPressedTime = -10f;

            return;
        }

        // If we are dancing, we should not process any movement input.
        if (NetIsDancing)
        {
            ApplyGravity(Vector3.zero);
            return;
        }

        Vector3 move = lockMovement ? Vector3.zero : new Vector3(input.Move.x, 0f, input.Move.y);
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        float baseMove = moveSpeed;
        float baseSprint = sprintSpeed;
        if (role != null && role.IsSeeker)
        {
            baseMove = seekerMoveSpeed;
            baseSprint = seekerSprintSpeed;
        }

        float speed = (lockMovement == false && input.Sprint) ? baseSprint : baseMove;
        this.inputMagnitude = move == Vector3.zero ? 0f : 1f; // FIX: store in local field
        float cameraYaw = mainCamera != null ? mainCamera.transform.eulerAngles.y : transform.eulerAngles.y;
        float targetRotation = transform.eulerAngles.y;
        if (move != Vector3.zero)
        {
            targetRotation = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg + cameraYaw;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, rotationSmoothTime, Mathf.Infinity, Runner.DeltaTime);
            transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0f, targetRotation, 0f) * Vector3.forward;
        Vector3 horizontal = targetDirection * speed * this.inputMagnitude; // FIX: use local field

        if (lockMovement == false && input.Jump)
        {
            lastJumpPressedTime = Runner.SimulationTime;
        }

        isGrounded = CheckGrounded();
        if (isGrounded)
        {
            lastGroundedTime = Runner.SimulationTime;
        }

        bool wantsJump = lockMovement == false && (Runner.SimulationTime - lastJumpPressedTime) <= jumpBufferTime;
        bool canCoyote = (Runner.SimulationTime - lastGroundedTime) <= coyoteTime;
        bool doJump = wantsJump && canCoyote;

        ApplyGravity(horizontal, doJump);
        
        // --- FIX: REMOVED assignment to networked properties ---
        this.horizontalSpeed = new Vector3(horizontal.x, 0f, horizontal.z).magnitude;
    }

    private void ApplyGravity(Vector3 horizontal, bool jump = false)
    {
        if (isGrounded)
        {
            if (jump)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            else if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
        }

        verticalVelocity += gravity * Runner.DeltaTime;

        Vector3 motion = new Vector3(horizontal.x, verticalVelocity, horizontal.z) * Runner.DeltaTime;
        controller.Move(motion);
    }

    private bool CheckGrounded()
    {
        Vector3 spherePosition = new Vector3(
            transform.position.x,
            transform.position.y + groundedOffset,
            transform.position.z);

        return Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    // --- FIX: Replaced Render() with logic to derive animation state locally ---
    public override void Render()
    {
        if (hasAnimator == false || updateAnimator == false)
        {
            return;
        }

        if (NetIsDancing != lastNetIsDancing || NetDanceIndex != lastNetDanceIndex)
        {
            lastNetIsDancing = NetIsDancing;
            lastNetDanceIndex = NetDanceIndex;
            if (NetIsDancing)
            {
                ApplyDanceStart();
            }
            else
            {
                ApplyDanceStop();
            }
        }

        // On proxies, we derive animation state from the NetworkTransform's movement.
        Vector3 currentVelocity = (transform.position - lastRenderPosition) / Time.deltaTime;
        lastRenderPosition = transform.position;

        float speed = new Vector3(currentVelocity.x, 0, currentVelocity.z).magnitude;
        bool grounded = CheckGrounded(); // Use local ground check for visual accuracy
        float vertVelocity = currentVelocity.y;

        // On state authority, we can use the "real" simulated values for a slightly more responsive feel.
        if (Object.HasStateAuthority)
        {
            speed = this.horizontalSpeed;
            grounded = this.isGrounded;
            vertVelocity = this.verticalVelocity;
        }

        animator.SetBool(animIDGrounded, grounded);
        bool isJumping = grounded == false && vertVelocity > 0.1f;
        bool isFreeFall = grounded == false && vertVelocity < -0.1f;
        animator.SetBool(animIDJump, isJumping);
        animator.SetBool(animIDFreeFall, isFreeFall);
        animator.SetFloat(animIDSpeed, speed);
        animator.SetFloat(animIDMotionSpeed, speed > 0.01f ? 1f : 0f);
    }

    private void ApplyDanceStart()
    {
        if (hasAnimator == false) return;

        animator.SetInteger(animIDDanceIndex, NetDanceIndex);
        animator.ResetTrigger(animIDStopDance);
        animator.SetTrigger(animIDStartDance);
    }

    private void ApplyDanceStop()
    {
        if (hasAnimator == false) return;

        animator.ResetTrigger(animIDStartDance);
        animator.SetTrigger(animIDStopDance);
    }

    public void RequestSabotageStun(float durationSeconds, Vector3 knockbackDirection, float knockbackSpeed)
    {
        if (Object == null)
        {
            return;
        }

        if (Object.HasStateAuthority)
        {
            ApplySabotageStunInternal(durationSeconds, knockbackDirection, knockbackSpeed);
            return;
        }

        RpcRequestSabotageStun(durationSeconds, knockbackDirection, knockbackSpeed);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RpcRequestSabotageStun(float durationSeconds, Vector3 knockbackDirection, float knockbackSpeed)
    {
        ApplySabotageStunInternal(durationSeconds, knockbackDirection, knockbackSpeed);
    }

    private void ApplySabotageStunInternal(float durationSeconds, Vector3 knockbackDirection, float knockbackSpeed)
    {
        if (Object == null || Object.HasStateAuthority == false)
        {
            return;
        }

        float clampedDuration = Mathf.Clamp(durationSeconds, 0.05f, 2f);
        SabotageStunTimer = TickTimer.CreateFromSeconds(Runner, clampedDuration);

        Vector3 dir = knockbackDirection;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = transform.forward;
        }

        sabotageKnockbackVelocity = dir.normalized * Mathf.Max(0f, knockbackSpeed);
        Debug.Log($"[Sabotage] Stun applied on {name}: duration={clampedDuration:F2}, knockback={sabotageKnockbackVelocity}");
    }

    private bool IsSabotageStunned()
    {
        if (Runner == null)
        {
            return false;
        }

        return SabotageStunTimer.ExpiredOrNotRunning(Runner) == false;
    }

}


