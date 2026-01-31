using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// BaseNPC를 상속받아, '걷기'와 '뛰기'를 반복하는 가면 행동을 정의합니다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(WanderPointProvider))]
public class BlueNPC : BaseNPC
{
    [Header("BlueNPC 설정")]
    [Tooltip("목적지에 얼마나 가까워지면 다음 목적지를 찾을지 결정")]
    public float stoppingDistance = 1.5f;

    [Header("가면 행동 설정")]
    [Tooltip("걷기 상태를 유지할 시간 (최소, 최대)")]
    public float[] WalkDuration = new float[] { 2f, 5f };
    [Tooltip("뛰기 상태를 유지할 시간 (최소, 최대)")]
    public float[] RunDuration = new float[] { 2f, 5f };

    private enum MaskState { Walking, Running }
    private MaskState currentMaskState;
    private float maskStateTimer;

    protected override void Awake()
    {
        base.Awake();
        agent.stoppingDistance = stoppingDistance;
    }

    private void Start()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        // 초기 상태를 랜덤으로 설정합니다.
        if (Random.value < 0.5f)
        {
            SetState(MaskState.Running);
        }
        else
        {
            SetState(MaskState.Walking);
        }
    }

    /// <summary>
    /// 매 프레임 실행되며 '걷기'와 '뛰기' 상태에 따라 행동을 결정합니다.
    /// </summary>
    protected override void ExecuteMaskBehavior()
    {
        if (NpcController == null || agent == null) return;

        agent.nextPosition = transform.position;

        maskStateTimer -= GetDeltaTime();
        if (maskStateTimer <= 0)
        {
            // 현재 상태가 달리기였으면 걷기로, 걷기였으면 달리기로 변경
            SetState(currentMaskState == MaskState.Running ? MaskState.Walking : MaskState.Running);
            return;
        }

        // 걷기, 뛰기 상태 모두 계속 움직이므로 목적지에 도착하면 항상 새로운 목적지를 찾습니다.
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            SetNewWanderDestination();
        }

        // 상태에 따라 뛰고 있는지(isSprinting) 여부를 결정해 NpcController에 전달합니다.
        bool isSprinting = (currentMaskState == MaskState.Running);
        NpcController.SetMovement(agent.desiredVelocity.normalized, isSprinting);
    }

    /// <summary>
    /// NPC의 상태를 설정하고, 각 상태에 맞는 초기화 작업을 수행합니다.
    /// </summary>
    private void SetState(MaskState newState)
    {
        currentMaskState = newState;
        NpcController.SetCommandStopped(false); // BlueNPC는 멈추지 않고 계속 움직입니다.

        if (newState == MaskState.Running)
        {
            maskStateTimer = RandomRangePicker(RunDuration);
        }
        else // Walking
        {
            maskStateTimer = RandomRangePicker(WalkDuration);
        }
        // 새로운 상태가 되면 항상 새로운 목적지를 설정합니다.
        SetNewWanderDestination();
    }

    /// <summary>
    /// WanderPointProvider를 사용해 새로운 목적지를 찾고, NavMeshAgent에 설정합니다.
    /// </summary>
    private void SetNewWanderDestination()
    {
        if (wanderProvider.GetRandomNavMeshPoint(out Vector3 destination))
        {
            NpcController.SetCommandDestination(destination);
        }
    }

}
