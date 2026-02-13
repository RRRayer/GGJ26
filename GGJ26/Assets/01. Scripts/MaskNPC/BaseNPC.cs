using Fusion;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NPCController))]
public abstract class BaseNPC : MonoBehaviour
{
    protected NPCController NpcController { get; private set; }
    protected NavMeshAgent agent;
    protected WanderPointProvider wanderProvider;

    protected bool HasStateAuthority
    {
        get { return NpcController != null && NpcController.Object != null && NpcController.Object.HasStateAuthority; }
    }

    protected bool IsAgentReady
    {
        get { return agent != null && agent.enabled && agent.isOnNavMesh; }
    }

    [Header("Event Channels - Listening to")]
    [SerializeField] private BoolEventChannelSO OnGroupDanceStart;
    [SerializeField] public IntEventChannelSO DanceIndexChannel;
    // Seeker NPC 춤 명령 이벤트를 수신하는 ScriptableObject입니다.
    [SerializeField] private SeekerNpcDanceCommandEventChannelSO seekerNpcDanceCommandEvent;

    public enum ActionState
    {
        MaskBehavior,
        MaskDance,
        GroupDance
    }

    private ActionState currentState;
    // Seeker NPC 춤을 멈추기 위한 코루틴 참조.
    private Coroutine seekerDanceStopRoutine;

    protected virtual void Awake()
    {
        NpcController = GetComponent<NPCController>();
        agent = GetComponent<NavMeshAgent>();
        wanderProvider = GetComponent<WanderPointProvider>();

        if (agent != null)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
        }
    }

    private void OnEnable()
    {
        if (OnGroupDanceStart != null)
        {
            OnGroupDanceStart.OnEventRaised += ExecuteGroupDance;
        }

        if (DanceIndexChannel != null)
        {
            DanceIndexChannel.OnEventRaised += ExecuteMaskDance;
        }

        // Seeker NPC 춤 명령 이벤트를 구독합니다.
        if (seekerNpcDanceCommandEvent != null)
        {
            seekerNpcDanceCommandEvent.OnEventRaised += ExecuteSeekerDanceCommand;
        }
        else
        {
            DanceEventPublisher.OnSeekerNpcDanceCommand += ExecuteSeekerDanceCommand;
        }
    }

    private void OnDisable()
    {
        if (OnGroupDanceStart != null)
        {
            OnGroupDanceStart.OnEventRaised -= ExecuteGroupDance;
        }

        if (DanceIndexChannel != null)
        {
            DanceIndexChannel.OnEventRaised -= ExecuteMaskDance;
        }

        // Seeker NPC 춤 명령 이벤트를 구독 해제합니다.
        if (seekerNpcDanceCommandEvent != null)
        {
            seekerNpcDanceCommandEvent.OnEventRaised -= ExecuteSeekerDanceCommand;
        }
        else
        {
            DanceEventPublisher.OnSeekerNpcDanceCommand -= ExecuteSeekerDanceCommand;
        }

        // Seeker NPC 춤 중이었다면 코루틴을 중지합니다.
        if (seekerDanceStopRoutine != null)
        {
            StopCoroutine(seekerDanceStopRoutine);
            seekerDanceStopRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (OnGroupDanceStart != null)
        {
            OnGroupDanceStart.OnEventRaised -= ExecuteGroupDance;
        }

        if (DanceIndexChannel != null)
        {
            DanceIndexChannel.OnEventRaised -= ExecuteMaskDance;
        }

        // Seeker NPC 춤 명령 이벤트를 구독 해제합니다.
        if (seekerNpcDanceCommandEvent != null)
        {
            seekerNpcDanceCommandEvent.OnEventRaised -= ExecuteSeekerDanceCommand;
        }
        else
        {
            DanceEventPublisher.OnSeekerNpcDanceCommand -= ExecuteSeekerDanceCommand;
        }
    }

    private void Update()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        if (NpcController != null && NpcController.Runner != null && NpcController.Runner.IsRunning)
        {
            return;
        }

        if (currentState == ActionState.MaskBehavior)
        {
            ExecuteMaskBehavior();
        }
    }

    public void NetworkTick()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        if (currentState == ActionState.MaskBehavior)
        {
            ExecuteMaskBehavior();
        }
    }

    protected float GetDeltaTime()
    {
        if (NpcController != null)
        {
            return NpcController.GetDeltaTime();
        }

        return Time.deltaTime;
    }

    protected abstract void ExecuteMaskBehavior();

    protected void ExecuteMaskDance(int danceIndex)
    {
        if (NpcController == null)
        {
            return;
        }

        if (currentState == ActionState.GroupDance)
        {
            return;
        }

        if (danceIndex == -1)
        {
            currentState = ActionState.MaskBehavior;
            NpcController.StopDance();
            return;
        }

        currentState = ActionState.MaskDance;
        NpcController.StartDance(danceIndex);
    }

    protected void ExecuteGroupDance(bool isStart)
    {
        if (NpcController == null)
        {
            return;
        }

        int danceIndex = Random.Range(0, 5);
        if (isStart)
        {
            currentState = ActionState.GroupDance;
            NpcController.StartDance(danceIndex);
        }
        else
        {
            currentState = ActionState.MaskBehavior;
            NpcController.StopDance();
        }
    }

    /// <summary>
    /// Seeker NPC 춤 명령을 실행합니다.
    /// NPC의 현재 위치가 명령의 반경 내에 있을 경우 춤을 시작하고, 지정된 시간 후 멈춥니다.
    /// </summary>
    private void ExecuteSeekerDanceCommand(SeekerNpcDanceCommand command)
    {
        if (NpcController == null || currentState == ActionState.GroupDance)
        {
            return;
        }

        Vector3 delta = transform.position - command.center;
        delta.y = 0f;
        if (delta.sqrMagnitude > command.radius * command.radius)
        {
            return;
        }

        currentState = ActionState.MaskDance;
        NpcController.StartDance(command.danceIndex);

        if (seekerDanceStopRoutine != null)
        {
            StopCoroutine(seekerDanceStopRoutine);
        }

        seekerDanceStopRoutine = StartCoroutine(StopSeekerDanceAfter(command.duration));
    }

    /// <summary>
    /// 지정된 시간 후 Seeker NPC의 춤을 멈춥니다.
    /// </summary>
    private IEnumerator StopSeekerDanceAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        seekerDanceStopRoutine = null;

        if (NpcController == null || currentState == ActionState.GroupDance)
        {
            yield break;
        }

        currentState = ActionState.MaskBehavior;
        NpcController.StopDance();
    }

    public float RandomRangePicker(float[] range)
    {
        if (range.Length != 2)
        {
            return 0f;
        }

        return Random.Range(range[0], range[1]);
    }

    public int RandomRangePicker(int[] range)
    {
        if (range.Length != 2)
        {
            return 0;
        }

        return Random.Range(range[0], range[1] + 1);
    }
}
