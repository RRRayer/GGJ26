using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class NPCController : NetworkBehaviour
{
    [Header("NPC Movement")]
    [Tooltip("Move speed of the character in m/s")]
    public float MoveSpeed = 2.0f;

    [Tooltip("Sprint speed of the character in m/s")]
    public float SprintSpeed = 5.335f;

    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;

    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Header("SFX Channel (optional)")]
    [SerializeField] private AudioCueEventChannelSO sfxEventChannel;
    [SerializeField] private AudioConfigurationSO sfxConfiguration;
    [SerializeField] private AudioCueSO landingSfxCue;
    [SerializeField] private AudioCueSO[] footstepSfxCues;

    [Space(10)]
    [Tooltip("The height the NPC can jump")]
    public float JumpHeight = 1.2f;

    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    public float JumpTimeout = 0.50f;

    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    [Header("NPC Grounded Check")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;

    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.28f;

    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;
    [Header("Death Ground Snap")]
    [SerializeField] private float deathGroundSnapDuration = 2.5f;
    [SerializeField] private float deathGroundSnapInterval = 0.08f;
    [SerializeField] private float deathGroundOffset = 0.02f;

    
    // movement
    private float speed;
    private float animationBlend;
    private float targetRotation;
    private float rotationVelocity;
    private float verticalVelocity;
    private float terminalVelocity = 53.0f;
    private Vector3 moveDirection = Vector3.zero;
    private bool isSprinting;
    private bool shouldJump;

    // timeout
    private float jumpTimeoutDelta;
    private float fallTimeoutDelta;

    // animation IDs
    private int animIDSpeed;
    private int animIDGrounded;
    private int animIDJump;
    private int animIDFreeFall;
    private int animIDMotionSpeed;
    private int animIDStartDance;
    private int animIDStopDance;
    private int animIDDanceIndex;
    private int animIDDead;

    // components
    private Animator animator;
    private CharacterController controller;
    private UnityEngine.AI.NavMeshAgent agent;
    private BaseNPC baseNpc;
    
    private int lastJumpCounter;
    private Vector3 lastValidPosition;
    private bool hasValidPosition;
    private const float MaxValidPositionDistance = 5000f;
    private bool CanUseAgent
    {
        get
        {
            return agent != null && agent.enabled && agent.isOnNavMesh;
        }
    }

    [Networked] private Vector3 NetDestination { get; set; }
    [Networked] private NetworkBool NetHasDestination { get; set; }
    [Networked] private NetworkBool NetIsStopped { get; set; }
    [Networked] private NetworkBool NetIsSprinting { get; set; }
    [Networked] private int NetJumpCounter { get; set; }
    [Networked] private float NetAnimSpeed { get; set; }
    [Networked] private float NetAnimMotionSpeed { get; set; }
    [Networked] private NetworkBool NetAnimGrounded { get; set; }
    [Networked] private float NetVerticalVelocity { get; set; }
    [Networked] public NetworkBool IsDead { get; private set; }
    [Networked] private NetworkBool IsDancing { get; set; }
    private bool snappedToGroundOnDeath;
    private bool hasSpawned;
    private float deathGroundSnapUntilTime;
    private float nextDeathGroundSnapTime;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        baseNpc = GetComponent<BaseNPC>();

        AssignAnimationIDs();

        jumpTimeoutDelta = JumpTimeout;
        fallTimeoutDelta = FallTimeout;
    }

    public override void FixedUpdateNetwork()
    {
        if (hasSpawned == false)
        {
            return;
        }

        if (IsDead)
        {
            if (agent != null)
            {
                agent.isStopped = true;
            }

            NetIsStopped = true;
            NetHasDestination = false;
            moveDirection = Vector3.zero;
            verticalVelocity = 0f;
            IsDancing = false;
            if (Object != null && Object.HasStateAuthority)
            {
                NetAnimSpeed = 0f;
                NetAnimMotionSpeed = 0f;
                NetAnimGrounded = Grounded;
                NetVerticalVelocity = 0f;
            }
            return;
        }

        float deltaTime = GetDeltaTime();
        // Only the state authority should drive movement/physics.
        if (Object == null || Object.HasStateAuthority == false)
        {
            return;
        }

        if (EnsureValidPosition() == false)
        {
            return;
        }

        if (EnsureOnNavMesh() == false)
        {
            return;
        }

        if (baseNpc != null)
        {
            baseNpc.NetworkTick();
        }

        ApplyNetworkCommands();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            return;
        }

        JumpAndGravity(deltaTime);
        GroundedCheck();
        Move(deltaTime);


        if (Object != null && Object.HasStateAuthority)
        {
            NetAnimSpeed = animationBlend;
            NetAnimMotionSpeed = moveDirection.magnitude;
            NetAnimGrounded = Grounded;
            NetVerticalVelocity = verticalVelocity;
        }
    }

    

    private void LateUpdate()
    {
        if (hasSpawned == false)
        {
            return;
        }

        if (IsDead == false)
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
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 60f, GroundLayers, QueryTriggerInteraction.Ignore);
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

            // Ignore self-colliders so we never snap to our own capsule/body.
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

    #region Public Methods for AI Control

    public void SetMovement(Vector3 direction, bool sprinting = false)
    {
        moveDirection = direction;
        isSprinting = sprinting;
    }

    public bool TryQueueJump()
    {
        if (Object == null || Object.HasStateAuthority == false)
        {
            return false;
        }

        if (Grounded && jumpTimeoutDelta <= 0.0f)
        {
            NetJumpCounter++;
            return true;
        }

        return false;
    }

    public void SetCommandDestination(Vector3 destination)
    {
        if (Object == null || Object.HasStateAuthority == false)
        {
            return;
        }

        if (IsDead || IsDancing)
        {
            NetHasDestination = false;
            return;
        }

        if (agent != null)
        {
            if (agent.isOnNavMesh == false)
            {
                NetHasDestination = false;
                return;
            }

            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(destination, out hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                destination = hit.position;
            }
            else
            {
                NetHasDestination = false;
                return;
            }
        }

        NetDestination = destination;
        NetHasDestination = true;
        if (agent != null)
        {
            agent.SetDestination(destination);
        }
    }

    public void SetCommandStopped(bool stopped)
    {
        if (Object == null || Object.HasStateAuthority == false)
        {
            return;
        }

        NetIsStopped = stopped;
        if (agent != null)
        {
            if (agent.enabled == false || agent.isOnNavMesh == false)
            {
                return;
            }
            agent.isStopped = stopped;
        }
    }

    public void SetCommandSprinting(bool sprinting)
    {
        if (Object == null || Object.HasStateAuthority == false)
        {
            return;
        }

        NetIsSprinting = sprinting;
    }

    public float GetDeltaTime()
    {
        if (Runner != null && Runner.IsRunning)
        {
            return Runner.DeltaTime;
        }

        return Time.deltaTime;
    }

    public bool TriggerJump()
    {
        if (Grounded && jumpTimeoutDelta <= 0.0f)
        {
            shouldJump = true;
            return true;
        }
        return false;
    }

    public void StartDance(int danceIndex)
    {
        if (IsDead)
        {
            return;
        }

        if (Object != null && Object.HasStateAuthority)
        {
            IsDancing = true;
            NetIsStopped = true;
            NetHasDestination = false;
        }
        SetMovement(Vector3.zero, false);

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (animator == null)
        {
            return;
        }

        // Reset the other trigger to ensure a clean transition
        animator.ResetTrigger(animIDStopDance);

        animator.SetInteger(animIDDanceIndex, danceIndex);
        animator.SetTrigger(animIDStartDance);
    }

    public void StopDance()
    {
        if (IsDead)
        {
            return;
        }

        if (Object != null && Object.HasStateAuthority)
        {
            IsDancing = false;
            NetIsStopped = false;
        }
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (animator == null)
        {
            return;
        }

        // Reset the other trigger to ensure a clean transition
        animator.ResetTrigger(animIDStartDance);
        
        animator.SetTrigger(animIDStopDance);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcTriggerDead()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            IsDead = true;
        }
        IsDancing = false;
        snappedToGroundOnDeath = false;
        deathGroundSnapUntilTime = Time.time + deathGroundSnapDuration;
        nextDeathGroundSnapTime = 0f;
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (animator == null)
        {
            return;
        }

        animator.SetTrigger(animIDDead);
    }

    #endregion

    private void ApplyNetworkCommands()
    {
        if (IsDead)
        {
            if (CanUseAgent)
            {
                agent.isStopped = true;
            }
            moveDirection = Vector3.zero;
            return;
        }

        if (IsDancing)
        {
            if (CanUseAgent)
            {
                agent.isStopped = true;
            }
            moveDirection = Vector3.zero;
            return;
        }

        if (NetJumpCounter != lastJumpCounter)
        {
            lastJumpCounter = NetJumpCounter;
            TriggerJump();
        }

        if (CanUseAgent)
        {
            agent.isStopped = NetIsStopped;
            if (NetHasDestination)
            {
                agent.SetDestination(NetDestination);
            }
            if (agent.updatePosition == false)
            {
                agent.nextPosition = transform.position;
            }
        }

        if (CanUseAgent && agent.hasPath && agent.isStopped == false)
        {
            SetMovement(agent.desiredVelocity.normalized, NetIsSprinting);
        }
        else
        {
            SetMovement(Vector3.zero, false);
        }
    }

    private void AssignAnimationIDs()
    {
        animIDSpeed = Animator.StringToHash("Speed");
        animIDGrounded = Animator.StringToHash("Grounded");
        animIDJump = Animator.StringToHash("Jump");
        animIDFreeFall = Animator.StringToHash("FreeFall");
        animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        animIDStartDance = Animator.StringToHash("StartDance");
        animIDStopDance = Animator.StringToHash("StopDance");
        animIDDanceIndex = Animator.StringToHash("DanceIndex");
        animIDDead = Animator.StringToHash("Dead");
    }

    public override void Spawned()
    {
        hasSpawned = true;
        lastValidPosition = transform.position;
        hasValidPosition = true;
        ConfigureNavMeshAgent();
    }

    private void ConfigureNavMeshAgent()
    {
        if (agent == null)
        {
            return;
        }

        bool isStateAuthority = Object != null && Object.HasStateAuthority;
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.enabled = isStateAuthority;

        if (isStateAuthority == false)
        {
            return;
        }

        if (agent.isOnNavMesh == false)
        {
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }
        else
        {
            agent.Warp(transform.position);
        }
    }

    private bool EnsureValidPosition()
    {
        Vector3 position = transform.position;
        float maxSqrDistance = MaxValidPositionDistance * MaxValidPositionDistance;
        if (IsFinite(position) && position.sqrMagnitude <= maxSqrDistance)
        {
            lastValidPosition = position;
            hasValidPosition = true;
            return true;
        }

        if (hasValidPosition)
        {
            transform.position = lastValidPosition;
        }
        else
        {
            transform.position = Vector3.zero;
        }

        if (agent != null)
        {
            if (CanUseAgent)
            {
                agent.ResetPath();
            }
            else if (agent.enabled)
            {
                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                }
            }

            if (agent.enabled)
            {
                agent.Warp(transform.position);
            }
        }

        NetHasDestination = false;
        NetIsStopped = true;
        moveDirection = Vector3.zero;
        verticalVelocity = 0f;
        return false;
    }

    private bool EnsureOnNavMesh()
    {
        if (agent == null || agent.enabled == false)
        {
            return true;
        }

        if (agent.isOnNavMesh)
        {
            return true;
        }

        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return true;
        }

        NetHasDestination = false;
        NetIsStopped = true;
        moveDirection = Vector3.zero;
        verticalVelocity = 0f;
        return false;
    }

    private static bool IsFinite(Vector3 value)
    {
        return float.IsNaN(value.x) == false &&
               float.IsNaN(value.y) == false &&
               float.IsNaN(value.z) == false &&
               float.IsInfinity(value.x) == false &&
               float.IsInfinity(value.y) == false &&
               float.IsInfinity(value.z) == false;
    }

    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

        animator.SetBool(animIDGrounded, Grounded);
    }

    private void Move(float deltaTime)
    {
        float targetSpeed = isSprinting ? SprintSpeed : MoveSpeed;
        if (moveDirection == Vector3.zero) targetSpeed = 0.0f;

        float currentHorizontalSpeed = speed;
        if (CanUseAgent)
        {
            currentHorizontalSpeed = agent.desiredVelocity.magnitude;
        }
        float speedOffset = 0.1f;
        float inputMagnitude = moveDirection.magnitude;

        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, deltaTime * SpeedChangeRate);
            speed = Mathf.Round(speed * 1000f) / 1000f;
        }
        else
        {
            speed = targetSpeed;
        }

        animationBlend = Mathf.Lerp(animationBlend, targetSpeed, deltaTime * SpeedChangeRate);
        if (animationBlend < 0.01f) animationBlend = 0f;

        Vector3 inputDirection = moveDirection.normalized;

        if (moveDirection != Vector3.zero)
        {
            targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, RotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;

        controller.Move(targetDirection.normalized * (speed * deltaTime) + new Vector3(0.0f, verticalVelocity, 0.0f) * deltaTime);

        animator.SetFloat(animIDSpeed, animationBlend);
        animator.SetFloat(animIDMotionSpeed, inputMagnitude);

        if (agent != null && agent.enabled && agent.isOnNavMesh == false)
        {
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
            {
                transform.position = hit.position;
                agent.Warp(hit.position);
            }
        }
    }


    

    private void JumpAndGravity(float deltaTime)
    {
        if (Grounded)
        {
            fallTimeoutDelta = FallTimeout;

            animator.SetBool(animIDJump, false);
            animator.SetBool(animIDFreeFall, false);

            if (verticalVelocity < 0.0f)
            {
                verticalVelocity = -2f;
            }

            if (shouldJump && jumpTimeoutDelta <= 0.0f)
            {
                verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                animator.SetBool(animIDJump, true);
            }

            if (jumpTimeoutDelta >= 0.0f)
            {
                jumpTimeoutDelta -= deltaTime;
            }
        }
        else
        {
            jumpTimeoutDelta = JumpTimeout;

            if (fallTimeoutDelta >= 0.0f)
            {
                fallTimeoutDelta -= deltaTime;
            }
            else
            {
                animator.SetBool(animIDFreeFall, true);
            }
        }

        if (verticalVelocity < terminalVelocity)
        {
            verticalVelocity += Gravity * deltaTime;
        }

        shouldJump = false;
    }

    public override void Render()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (animator == null)
        {
            return;
        }

        

        animator.SetBool(animIDGrounded, NetAnimGrounded);
        bool isJumping = NetAnimGrounded == false && NetVerticalVelocity > 0.1f;
        bool isFreeFall = NetAnimGrounded == false && NetVerticalVelocity < -0.1f;
        animator.SetBool(animIDJump, isJumping);
        animator.SetBool(animIDFreeFall, isFreeFall);
        animator.SetFloat(animIDSpeed, NetAnimSpeed);
        animator.SetFloat(animIDMotionSpeed, NetAnimMotionSpeed);
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (TryPlayFootstepSfx())
            {
                return;
            }

            if (FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(controller.center), FootstepAudioVolume);
            }
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (TryPlayLandingSfx())
            {
                return;
            }

            AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(controller.center), FootstepAudioVolume);
        }
    }

    private bool TryPlayFootstepSfx()
    {
        if (sfxEventChannel == null || sfxConfiguration == null || footstepSfxCues == null || footstepSfxCues.Length == 0)
        {
            return false;
        }

        var index = Random.Range(0, footstepSfxCues.Length);
        var cue = footstepSfxCues[index];
        if (cue == null)
        {
            return false;
        }

        sfxEventChannel.RaisePlayEvent(cue, sfxConfiguration, transform.TransformPoint(controller.center));
        return true;
    }

    private bool TryPlayLandingSfx()
    {
        if (sfxEventChannel == null || sfxConfiguration == null || landingSfxCue == null)
        {
            return false;
        }

        sfxEventChannel.RaisePlayEvent(landingSfxCue, sfxConfiguration, transform.TransformPoint(controller.center));
        return true;
    }
}
