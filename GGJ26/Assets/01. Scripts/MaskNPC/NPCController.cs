using UnityEngine;


[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
// NPC 움직임 및 애니메이션 제어

public class NPCController : MonoBehaviour
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
    
    // --- private fields ---
    
    // movement
    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;
    private Vector3 _moveDirection = Vector3.zero;
    private bool _isSprinting = false;
    private bool _shouldJump = false;

    // timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    // animation IDs
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;
    private int _animIDStartDance; 
    private int _animIDStopDance; 
    private int _animIDDanceIndex; 

    // components
    private Animator _animator;
    private CharacterController _controller;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

        AssignAnimationIDs();

        // reset our timeouts on start
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }

    private void Update()
    {
        JumpAndGravity();
        GroundedCheck();
        Move();
    }

    #region Public Methods for AI Control
    
    /// <summary>
    /// Sets the direction for the NPC to move. Called by an AI controller.
    /// </summary>
    /// <param name="direction">The desired movement direction.</param>
    /// <param name="isSprinting">Whether the NPC should sprint.</param>
    public void SetMovement(Vector3 direction, bool isSprinting = false)
    {
        _moveDirection = direction;
        _isSprinting = isSprinting;
    }

    /// <summary>
    /// Makes the NPC attempt to jump. Called by an AI controller.
    /// </summary>
    public bool TriggerJump()
    {
        if (Grounded && _jumpTimeoutDelta <= 0.0f)
        {
            _shouldJump = true;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Starts one of the dance animations.
    /// </summary>
    /// <param name="danceIndex">The index of the dance to play (e.g., 0-3).</param>
    public void StartDance(int danceIndex)
    {
        // Make the NPC stop moving
        SetMovement(Vector3.zero, false);
    
        _animator.SetInteger(_animIDDanceIndex, danceIndex);
        _animator.SetTrigger(_animIDStartDance);
    }

    /// <summary>
    /// Stops the dance animation and returns to normal locomotion.
    /// </summary>
    public void StopDance()
    {
        _animator.SetTrigger(_animIDStopDance);
    }

    #endregion

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        _animIDStartDance = Animator.StringToHash("StartDance"); 
        _animIDStopDance = Animator.StringToHash("StopDance");   
        _animIDDanceIndex = Animator.StringToHash("DanceIndex"); 
    }

    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        
        _animator.SetBool(_animIDGrounded, Grounded);
    }

    private void Move()
    {
        float targetSpeed = _isSprinting ? SprintSpeed : MoveSpeed;
        if (_moveDirection == Vector3.zero) targetSpeed = 0.0f;

        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
        float speedOffset = 0.1f;
        float inputMagnitude = _moveDirection.magnitude;

        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        Vector3 inputDirection = _moveDirection.normalized;

        if (_moveDirection != Vector3.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
        
        _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        _animator.SetFloat(_animIDSpeed, _animationBlend);
        _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            _fallTimeoutDelta = FallTimeout;
            
            _animator.SetBool(_animIDJump, false);
            _animator.SetBool(_animIDFreeFall, false);

            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            if (_shouldJump && _jumpTimeoutDelta <= 0.0f)
            {
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                _animator.SetBool(_animIDJump, true);
            }
            
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            _jumpTimeoutDelta = JumpTimeout;
            
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _animator.SetBool(_animIDFreeFall, true);
            }
        }

        // apply gravity over time
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }

        // Reset jump trigger after processing
        _shouldJump = false;
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;
        
        Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
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
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
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
            
            AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
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

        sfxEventChannel.RaisePlayEvent(cue, sfxConfiguration, transform.TransformPoint(_controller.center));
        return true;
    }

    private bool TryPlayLandingSfx()
    {
        if (sfxEventChannel == null || sfxConfiguration == null || landingSfxCue == null)
        {
            return false;
        }

        sfxEventChannel.RaisePlayEvent(landingSfxCue, sfxConfiguration, transform.TransformPoint(_controller.center));
        return true;
    }
}

