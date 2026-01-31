using System.Collections.Generic;
using UnityEngine;

// 이 스크립트가 동작하려면 DanceEventPublisher.cs에 DanceInfo 구조체와 OnNextDanceAnnounced 이벤트가 정의되어 있어야 합니다.
public class UINextDance : MonoBehaviour
{
    // 인스펙터에서 3개의 UI 셀을 순서대로 할당
    [SerializeField] private UINextDanceCell[] danceCells = new UINextDanceCell[3];

    // 다음 댄스들을 저장할 큐
    private readonly Queue<DanceInfo> danceQueue = new Queue<DanceInfo>();

    private void OnEnable()
    {
        // DanceEventPublisher의 이벤트 구독
        DanceEventPublisher.OnNextDanceAnnounced += HandleNextDanceAnnounced;
        DanceEventPublisher.OnMaskDanceEnded += ConsumeNextDance;
    }

    private void OnDisable()
    {
        // 구독 해제 (메모리 누수 방지)
        DanceEventPublisher.OnNextDanceAnnounced -= HandleNextDanceAnnounced;
        DanceEventPublisher.OnMaskDanceEnded -= ConsumeNextDance;
    }

    private void Start()
    {
        // 시작할 때 모든 셀을 숨깁니다.
        foreach (var cell in danceCells)
        {
            cell.HideCell();
        }
    }

    private void HandleNextDanceAnnounced(DanceInfo danceInfo)
    {
        // 큐에 새 댄스 정보 추가
        danceQueue.Enqueue(danceInfo);
        Debug.Log($"[UINextDance] 큐에 새 댄스 추가: {danceInfo.Color}, {danceInfo.DanceIndex}. 현재 큐 크기: {danceQueue.Count}");
        // UI 새로고침
        UpdateUI();
    }

    /// <summary>
    /// 게임 로직에서 댄스가 실제로 시작될 때 이 함수를 호출하여
    /// UI 큐에서 첫 번째 항목을 제거하고 화면을 업데이트합니다.
    /// </summary>
    public void ConsumeNextDance()
    {
        Debug.Log($"[UINextDance] ConsumeNextDance 호출됨. 현재 큐 크기: {danceQueue.Count}");
        if (danceQueue.Count > 0)
        {
            DanceInfo consumed = danceQueue.Dequeue();
            Debug.Log($"[UINextDance] 큐에서 댄스 제거: {consumed.Color}, {consumed.DanceIndex}. 남은 큐 크기: {danceQueue.Count}");
            UpdateUI();
        }
        else
        {
            Debug.LogWarning("[UINextDance] ConsumeNextDance가 호출되었지만 큐가 비어있습니다.");
        }
    }

    private void UpdateUI()
    {
        // 큐의 내용을 배열로 복사하여 쉽게 접근
        DanceInfo[] upcomingDances = danceQueue.ToArray();
        Debug.Log($"[UINextDance] UpdateUI 호출. {upcomingDances.Length}개의 셀을 업데이트합니다.");

        for (int i = 0; i < danceCells.Length; i++)
        {
            // 큐에 보여줄 댄스 정보가 있다면 셀 업데이트
            if (i < upcomingDances.Length)
            {
                danceCells[i].UpdateCell(upcomingDances[i]);
            }
            // 그렇지 않다면 셀 숨기기
            else
            {
                danceCells[i].HideCell();
            }
        }
    }
}