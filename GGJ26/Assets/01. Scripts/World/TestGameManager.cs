using System.Collections;
using UnityEngine;

public class TestGameManager : MonoBehaviour
{
    [SerializeField]
    private VoidEventChannelSO startDiscoEvent;
    [SerializeField]
    private VoidEventChannelSO stopDiscoEvent;
    [SerializeField]
    private float discoDuration = 10f; // 디스코볼 효과가 지속될 시간 (초)

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 디스코 시퀀스 테스트 시작
        StartCoroutine(TestDiscoSequence());
    }

    // 디스코 시작/중지 이벤트를 발행하는 코루틴
    private IEnumerator TestDiscoSequence()
    {
        // 시작 이벤트 발행
        if (startDiscoEvent != null)
        {
            startDiscoEvent.RaiseEvent();
        }
        else
        {
            Debug.LogWarning("startDiscoEvent가 할당되지 않았습니다. TestGameManager.");
        }

        // 지정된 시간만큼 대기
        yield return new WaitForSeconds(discoDuration);

        // 중지 이벤트 발행
        if (stopDiscoEvent != null)
        {
            stopDiscoEvent.RaiseEvent();
        }
        else
        {
            Debug.LogWarning("stopDiscoEvent가 할당되지 않았습니다. TestGameManager.");
        }
    }
}
