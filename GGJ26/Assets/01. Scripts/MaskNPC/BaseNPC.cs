using Cinemachine;
using Fusion;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

/// <summary>
/// 모든 NPC 스크립트가 상속받을 부모 추상 클래스입니다.
/// NPC의 공통 기능과 행동 로직을 위한 뼈대를 제공합니다.
/// </summary>
[RequireComponent(typeof(NPCController))]
public abstract class BaseNPC : MonoBehaviour
{
    // 자식 클래스들이 접근할 수 있도록 protected로 NPCController 참조를 제공합니다.
    protected NPCController NpcController { get; private set; }
    protected NavMeshAgent agent;
    protected WanderPointProvider wanderProvider;

    [Header("이벤트 채널 - Listening to")]
    [SerializeField] private BoolEventChannelSO OnGroupDanceStart;
    [SerializeField] public IntEventChannelSO DanceIndexChannel;
    
    public enum ActionState
    {
        // 가면 행동
        MaskBehavior,

        // 가면 댄스
        MaskDance,

        // 단체 댄스
        GroupDance
    }

    private ActionState currentState;


    /// <summary>
    /// Awake는 컴포넌트 참조를 초기화하는 데 사용됩니다.
    /// 자식 클래스에서 Awake를 재정의(override)할 경우, base.Awake()를 호출해야 합니다.
    /// </summary>
    protected virtual void Awake()
    {
        // 이 스크립트가 붙은 게임 오브젝트에서 NPCController 컴포넌트를 찾습니다.
        NpcController = GetComponent<NPCController>();
        agent = GetComponent<NavMeshAgent>();
        wanderProvider = GetComponent<WanderPointProvider>();

        // NavMeshAgent가 캐릭터의 위치나 회전을 직접 제어하지 않도록 설정합니다.
        // 모든 실제 움직임은 NPCController가 담당합니다.
        agent.updatePosition = false;
        agent.updateRotation = false;
    }


    private void OnEnable()
    {
        if (OnGroupDanceStart != null)
        {
            OnGroupDanceStart.OnEventRaised += ExecuteGroupDance;
            DanceIndexChannel.OnEventRaised += ExecuteMaskDance;
        }

    }

    private void OnDisable()
    {
        if (OnGroupDanceStart != null)
        {
            OnGroupDanceStart.OnEventRaised -= ExecuteGroupDance;
        }
    }

    /// <summary>
    /// Unity의 Update 루프입니다.
    /// 매 프레임마다 자식 클래스가 구체적으로 정의한 행동 로직을 실행합니다.
    /// </summary>
    private void Update()
    {
        if (currentState == ActionState.MaskBehavior)
        {
            ExecuteMaskBehavior();
        }
    }

    /// <summary>
    /// 각 그룹의 가면 행동
    /// </summary>
    protected abstract void ExecuteMaskBehavior();


    /// <summary>
    /// 각 그룹별 가면 댄스. 그룹 별로 같은 춤
    /// </summary>
    /// <param name="isStart"></param>
    protected void ExecuteMaskDance(int danceIndex)
    {
        if (currentState == ActionState.GroupDance)
        {
            // 단체 댄스 중일 때는 가면 댄스로 전환하지 않음
            return;
        }
        if (danceIndex == -1)
        {
            currentState = ActionState.MaskBehavior;
            NpcController.StopDance();
        }
        else
        {
            currentState = ActionState.MaskDance;
            NpcController.StartDance(danceIndex);
        }
    }

    /// <summary>
    /// 단체 댄스. 모두가 함께 춤. 전부 랜덤
    /// </summary>
    /// <param name="isStart"></param>
    protected void ExecuteGroupDance(bool isStart)
    {
        if (NpcController == null)
        {
            return;
        }

        int danceIndex = Random.Range(0, 4); // 0에서 3 사이의 랜덤 인덱스 선택
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
        // For integers, Random.Range's upper bound is exclusive, so add 1 to make it inclusive.
        return Random.Range(range[0], range[1] + 1);
    }
}
