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
    [SerializeField] private bool debugInput = false;
    [SerializeField] private bool updateAnimator = true;

    [Networked] private float VerticalVelocity { get; set; }
    [Networked] private float NetHorizontalSpeed { get; set; }
    [Networked] private float NetInputMagnitude { get; set; }
    [Networked] private NetworkBool NetGrounded { get; set; }
    [Networked] private NetworkBool IsDancing { get; set; }

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
    private float fallTimeout = 0.15f;
    private float jumpTimeout = 0.3f;
    private float lastJumpPressedTime = -10f;
    private float lastGroundedTime = -10f;
    private Camera mainCamera;
    private bool isGrounded;
    private PlayerRole role;
    private int lastDanceIndex = -1;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main;
        role = GetComponent<PlayerRole>();
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
    }

    public void StartDance(int danceIndex)
    {
        if (hasAnimator == false) return;

        IsDancing = true;
        animator.SetInteger(animIDDanceIndex, danceIndex);
        animator.SetTrigger(animIDStartDance);
    }

    public void StopDance()
    {
        if (hasAnimator == false) return;

        IsDancing = false;
        animator.SetTrigger(animIDStopDance);
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

        if (GetInput(out PlayerInputData input) == false)
        {
            if (debugInput)
            {
                Debug.Log("[FusionThirdPersonMotor] No input for tick", this);
            }
            ApplyGravity(Vector3.zero);
            return;
        }
        
        // Handle dance input first (hold-to-dance, edge-triggered).
        if (input.danceIndex != lastDanceIndex)
        {
            if (lastDanceIndex == -1 && input.danceIndex != -1)
            {
                StartDance(input.danceIndex);
            }
            else if (input.danceIndex == -1 && lastDanceIndex != -1)
            {
                StopDance();
            }
            else if (input.danceIndex != -1)
            {
                StartDance(input.danceIndex);
            }

            lastDanceIndex = input.danceIndex;
        }

        // If we are dancing, we should not process any movement input.
        if (IsDancing)
        {
            ApplyGravity(Vector3.zero);
            return;
        }

        Vector3 move = new Vector3(input.Move.x, 0f, input.Move.y);
        if (debugInput)
        {
            Debug.Log($"[FusionThirdPersonMotor] move={move} jump={input.Jump} sprint={input.Sprint}", this);
        }
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

        float speed = input.Sprint ? baseSprint : baseMove;
        float inputMagnitude = move == Vector3.zero ? 0f : 1f;
        float cameraYaw = mainCamera != null ? mainCamera.transform.eulerAngles.y : transform.eulerAngles.y;
        float targetRotation = transform.eulerAngles.y;
        if (move != Vector3.zero)
        {
            targetRotation = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg + cameraYaw;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, rotationSmoothTime, Mathf.Infinity, Runner.DeltaTime);
            transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0f, targetRotation, 0f) * Vector3.forward;
        Vector3 horizontal = targetDirection * speed * inputMagnitude;

        if (input.Jump)
        {
            lastJumpPressedTime = Runner.SimulationTime;
        }

        isGrounded = CheckGrounded();
        if (isGrounded)
        {
            lastGroundedTime = Runner.SimulationTime;
        }

        bool wantsJump = (Runner.SimulationTime - lastJumpPressedTime) <= jumpBufferTime;
        bool canCoyote = (Runner.SimulationTime - lastGroundedTime) <= coyoteTime;
        bool doJump = wantsJump && canCoyote;

        ApplyGravity(horizontal, doJump);
        NetGrounded = isGrounded;
        NetHorizontalSpeed = new Vector3(horizontal.x, 0f, horizontal.z).magnitude;
        NetInputMagnitude = inputMagnitude;
    }

    private void ApplyGravity(Vector3 horizontal, bool jump = false)
    {
        if (isGrounded)
        {
            if (jump)
            {
                VerticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            else if (VerticalVelocity < 0f)
            {
                VerticalVelocity = -2f;
            }
        }

        VerticalVelocity += gravity * Runner.DeltaTime;

        Vector3 motion = new Vector3(horizontal.x, VerticalVelocity, horizontal.z) * Runner.DeltaTime;
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

    public override void Render()
    {
        if (hasAnimator == false || updateAnimator == false)
        {
            return;
        }

        bool grounded = NetGrounded;
        animator.SetBool(animIDGrounded, grounded);

        bool isJumping = grounded == false && VerticalVelocity > 0.1f;
        bool isFreeFall = grounded == false && VerticalVelocity < -0.1f;

        animator.SetBool(animIDJump, isJumping);
        animator.SetBool(animIDFreeFall, isFreeFall);

        animator.SetFloat(animIDSpeed, NetHorizontalSpeed);
        animator.SetFloat(animIDMotionSpeed, NetInputMagnitude);
    }
}
