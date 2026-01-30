using UnityEngine;
using UnityEngine.AI;

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
    [SerializeField] private BoolEventChannelSO OnMaskDanceStart;
    [SerializeField] private BoolEventChannelSO OnGroupDanceStart;
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


    private void Start()
    {
        GameStart();
    }

    private void GameStart()
    {
        OnGroupDanceStart.OnEventRaised += ExecuteGroupDance;
        OnMaskDanceStart.OnEventRaised += ExecuteMaskDance;
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
    /// 자식 NPC 클래스가 반드시 구현해야 하는 추상 메서드입니다.
    /// 각 NPC의 고유한 행동 로직이 이 메서드 안에 작성됩니다.
    /// </summary>
    protected abstract void ExecuteMaskBehavior();

    protected void ExecuteMaskDance(bool isStart)
    {
        if (isStart)
        {
            currentState = ActionState.MaskDance;

            NpcController.StartDance(0);    
        }
        else
        {
            currentState = ActionState.MaskBehavior;
            NpcController.StopDance();
        }
    }
    protected void ExecuteGroupDance(bool isStart)
    {
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
            Debug.LogError("범위 배열의 길이는 2여야 합니다.");
            return 0f;
        }
        return Random.Range(range[0], range[1]);
    }

    public int RandomRangePicker(int[] range)
    {
        if (range.Length != 2)
        {
            Debug.LogError("범위 배열의 길이는 2여야 합니다.");
            return 0;
        }
        // For integers, Random.Range's upper bound is exclusive, so add 1 to make it inclusive.
        return Random.Range(range[0], range[1] + 1);
    }
}
