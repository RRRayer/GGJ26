using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

// 댄스 정보를 담을 구조체. 다른 UI 스크립트에서도 접근할 수 있도록 class 바깥에 정의합니다.
public struct DanceInfo
{
    public int DanceIndex;
    public MaskColor Color;
}

public class DanceEventPublisher : MonoBehaviour
{
    // UI에게 다음 댄스 정보를 전달할 새로운 static 이벤트
    public static event Action<DanceInfo> OnNextDanceAnnounced;
    // MaskDance가 종료되었음을 UI에 알리는 static 이벤트
    public static event Action OnMaskDanceEnded;

    [Header("Mask Dance Timing")]
    [SerializeField] private float maskDanceMinInterval = 8f;
    [SerializeField] private float maskDanceMaxInterval = 12f;
    [SerializeField] private float maskDanceDuration = 3f;

    [Header("Group Dance Timing")]
    [SerializeField] private float groupDanceInterval = 30f;
    [SerializeField] private float groupDanceDuration = 10f;

    [Header("Mask Dance Events (per color)")]
    [SerializeField] private IntEventChannelSO[] maskDanceIndexEvents = new IntEventChannelSO[3];

    [Header("Group Dance Events")]
    [SerializeField] private BoolEventChannelSO groupDanceActiveEvent;
    [SerializeField] private VoidEventChannelSO startDiscoEvent;
    [SerializeField] private VoidEventChannelSO stopDiscoEvent;

    private bool isGroupDanceActive;
    private readonly bool[] maskDanceActive = new bool[3];

    private void Start()
    {
        for (int i = 0; i < 3; i++)
        {
            StartCoroutine(MaskDanceLoop((MaskColor)i));
        }

        StartCoroutine(GroupDanceLoop());
    }

    private IEnumerator MaskDanceLoop(MaskColor color)
    {
        int colorIndex = (int)color;

        // 시작 시 각 코루틴이 한번에 큐를 채우지 않도록 초반에 랜덤 딜레이를 줍니다.
        yield return new WaitForSeconds(Random.Range(1f, 3f));

        while (true)
        {
            // 1. 다음 댄스 정보를 미리 결정하고 UI에 알립니다.
            int danceIndex = Random.Range(1, 5);
            OnNextDanceAnnounced?.Invoke(new DanceInfo { DanceIndex = danceIndex, Color = color });

            // 2. 댄스가 실제로 시작하기 전까지 대기합니다. 이 시간이 UI 큐에 표시되는 시간이 됩니다.
            //    (여기서는 고정된 짧은 시간을 주거나, 기존처럼 랜덤값을 사용할 수 있습니다. 일단 3초로 고정하겠습니다.)
            yield return new WaitForSeconds(3f); 

            // 3. 그룹 댄스가 시작되면 잠시 대기합니다.
            while (isGroupDanceActive)
            {
                yield return null;
            }
            
            // 4. 댄스를 시작합니다.
            maskDanceIndexEvents[colorIndex]?.RaiseEvent(danceIndex);
            maskDanceActive[colorIndex] = true;
            Debug.Log($"[DanceEvent] MaskDance start color={color} dance={danceIndex} duration={maskDanceDuration:0.0}s", this);

            // 5. 댄스 지속 시간만큼 기다립니다.
            float elapsed = 0f;
            while (elapsed < maskDanceDuration && isGroupDanceActive == false)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 6. 댄스를 종료하고, UI가 큐에서 아이템을 소비하도록 알립니다.
            maskDanceIndexEvents[colorIndex]?.RaiseEvent(-1);
            maskDanceActive[colorIndex] = false;
            Debug.Log($"[DanceEvent] MaskDance end color={color}", this);
            OnMaskDanceEnded?.Invoke();

            // 7. --- 핵심 변경점 ---
            // 다음 댄스를 예고하기 전까지 충분한 시간(쿨다운)을 기다립니다.
            float wait = Random.Range(maskDanceMinInterval, maskDanceMaxInterval);
            yield return new WaitForSeconds(wait);
        }
    }

    private IEnumerator GroupDanceLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(groupDanceInterval);

            isGroupDanceActive = true;
            StopAllMaskDances();
            groupDanceActiveEvent?.RaiseEvent(true);
            startDiscoEvent?.RaiseEvent();
            Debug.Log($"[DanceEvent] GroupDance start duration={groupDanceDuration:0.0}s", this);

            yield return new WaitForSeconds(groupDanceDuration);

            groupDanceActiveEvent?.RaiseEvent(false);
            stopDiscoEvent?.RaiseEvent();
            Debug.Log("[DanceEvent] GroupDance end", this);
            isGroupDanceActive = false;
        }
    }

    private void StopAllMaskDances()
    {
        for (int i = 0; i < maskDanceActive.Length; i++)
        {
            if (maskDanceActive[i])
            {
                maskDanceIndexEvents[i]?.RaiseEvent(-1);
                maskDanceActive[i] = false;
            }
        }
    }

    private static string GetEventName(ScriptableObject channel)
    {
        return channel == null ? "None" : $"{channel.name}#{channel.GetInstanceID()}";
    }

    private static string GetEventNames(ScriptableObject[] channels)
    {
        if (channels == null || channels.Length == 0)
        {
            return "None";
        }

        string[] names = new string[channels.Length];
        for (int i = 0; i < channels.Length; i++)
        {
            names[i] = GetEventName(channels[i]);
        }

        return string.Join(", ", names);
    }
}

