using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// BaseNPC를 상속받아, '달리기'와 '대기'를 반복하는 가면 행동을 정의합니다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(WanderPointProvider))]
public class RedNPC : BaseNPC
{
    [Header("RedNPC 설정")]
    [Tooltip("목적지에 얼마나 가까워지면 다음 목적지를 찾을지 결정")]
    public float stoppingDistance = 1.5f;

    [Header("가면 행동 설정")]
    [Tooltip("달리기 상태를 유지할 시간 (최소, 최대)")]
    public float[] RunDuration = new float[] { 2f, 5f };
    [Tooltip("대기 상태를 유지할 시간 (최소, 최대)")]
    public float[] IdleDuration = new float[] { 2f, 5f };

    private enum MaskState { Running, Idling }
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
        if (Random.value < 0.5f) // 50% 확률로 Running, 50% 확률로 Idling
        {
            currentMaskState = MaskState.Running;
            maskStateTimer = RandomRangePicker(RunDuration);
            agent.isStopped = false;
            SetNewWanderDestination();
            if (NpcController != null)
            {
                NpcController.SetCommandStopped(false);
                NpcController.SetCommandSprinting(true);
            }
        }
        else
        {
            currentMaskState = MaskState.Idling;
            maskStateTimer = RandomRangePicker(IdleDuration);
            agent.isStopped = true;
            agent.ResetPath(); // Ensure agent stops if starting with idling
            if (NpcController != null)
            {
                NpcController.SetCommandStopped(true);
                NpcController.SetCommandSprinting(false);
            }
        }
    }

    /// <summary>
    /// 매 프레임 실행되며 '달리기'와 '대기' 상태에 따라 행동을 결정합니다.
    /// </summary>
    protected override void ExecuteMaskBehavior()
    {
        if (NpcController == null || agent == null) return;

        // NavMeshAgent의 위치를 캐릭터의 실제 위치로 계속 업데이트합니다.
        agent.nextPosition = transform.position;

        // 현재 상태의 타이머를 감소시키고, 시간이 다 되면 상태를 변경합니다.
        maskStateTimer -= GetDeltaTime();
        if (maskStateTimer <= 0)
        {
            SwitchMaskState();
        }

        // 현재 상태에 따른 행동을 실행합니다.
        if (currentMaskState == MaskState.Running)
        {
            // 목적지에 도착하면 새로운 목적지를 설정합니다.
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                SetNewWanderDestination();
            }

            // NPCController에 '달리기'를 명령합니다.
            NpcController.SetCommandStopped(false);
            NpcController.SetCommandSprinting(true);
        }
        else // Idling 상태일 경우
        {
            // NPCController에 '정지'를 명령합니다.
            NpcController.SetCommandStopped(true);
            NpcController.SetCommandSprinting(false);
        }
    }

    /// <summary>
    /// '달리기'와 '대기' 상태를 전환합니다.
    /// </summary>
    private void SwitchMaskState()
    {
        if (currentMaskState == MaskState.Running)
        {
            // '대기' 상태로 변경
            currentMaskState = MaskState.Idling;
            maskStateTimer = RandomRangePicker(IdleDuration);
            agent.isStopped = true;
            agent.ResetPath();
            if (NpcController != null)
            {
                NpcController.SetCommandStopped(true);
                NpcController.SetCommandSprinting(false);
            }
        }
        else // Idling 상태였다면
        {
            // '달리기' 상태로 변경
            currentMaskState = MaskState.Running;
            maskStateTimer = RandomRangePicker(RunDuration);
            agent.isStopped = false;
            SetNewWanderDestination();
            if (NpcController != null)
            {
                NpcController.SetCommandStopped(false);
                NpcController.SetCommandSprinting(true);
            }
        }
    }
    
    /// <summary>
    /// WanderPointProvider를 사용해 새로운 목적지를 찾고, NavMeshAgent에 설정합니다.
    /// </summary>
    private void SetNewWanderDestination()
    {
        if (wanderProvider.GetRandomNavMeshPoint(out Vector3 destination))
        {
            agent.SetDestination(destination);
        }
    }

}

