using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(WanderPointProvider))]
public class GreenNPC : BaseNPC
{
    [Header("가면 행동 설정")]
    [Tooltip("걷기 상태를 유지할 시간 (최소, 최대)")]
    public float[] WalkDuration = { 3f, 6f };
    [Tooltip("점프 횟수 (최소, 최대)")]
    public int[] JumpCount = { 1, 4 };

    private enum MaskState { Walking, Jumping }
    private MaskState currentMaskState;

    private float walkTimer;
    private int jumpsRemaining;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        // Start with a random state
        if (Random.value < 0.5f)
        {
            SetState(MaskState.Walking);
        }
        else
        {
            SetState(MaskState.Jumping);
        }
    }

    protected override void ExecuteMaskBehavior()
    {
        if (NpcController == null || agent == null) return;

        agent.nextPosition = transform.position;

        if (currentMaskState == MaskState.Walking)
        {
            walkTimer -= Time.deltaTime;
            if (walkTimer <= 0)
            {
                SetState(MaskState.Jumping);
                return; // State changed, exit for this frame
            }
            
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (wanderProvider.GetRandomNavMeshPoint(out Vector3 dest))
                {
                    agent.SetDestination(dest);
                }
            }
            NpcController.SetMovement(agent.desiredVelocity.normalized, false); // false for walking
        }
        else // Jumping state
        {
            // Stand still while in the jumping state
            NpcController.SetMovement(Vector3.zero, false);
            
            // Try to jump if there are jumps remaining
            if (jumpsRemaining > 0)
            {
                if (NpcController.TriggerJump())
                {
                    jumpsRemaining--;
                }
            }
            // Once all jumps are done, switch back to walking
            else
            {
                SetState(MaskState.Walking);
            }
        }
    }

    private void SetState(MaskState newState)
    {
        currentMaskState = newState;
        if (newState == MaskState.Walking)
        {
            walkTimer = RandomRangePicker(WalkDuration);
            agent.isStopped = false;
            if (wanderProvider.GetRandomNavMeshPoint(out Vector3 dest))
            {
                agent.SetDestination(dest);
            }
        }
        else // Jumping
        {
            jumpsRemaining = RandomRangePicker(JumpCount);
            agent.isStopped = true;
            agent.ResetPath();
        }
    }


}