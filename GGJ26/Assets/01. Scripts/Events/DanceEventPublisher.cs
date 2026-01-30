using System.Collections;
using UnityEngine;

public class DanceEventPublisher : MonoBehaviour
{
    [Header("Mask Dance Timing")]
    [SerializeField] private float maskDanceMinInterval = 8f;
    [SerializeField] private float maskDanceMaxInterval = 12f;
    [SerializeField] private float maskDanceDuration = 3f;

    [Header("Group Dance Timing")]
    [SerializeField] private float groupDanceInterval = 30f;
    [SerializeField] private float groupDanceDuration = 10f;

    [Header("Events")]
    [SerializeField] private BoolEventChannelSO maskDanceActiveEvent;
    [SerializeField] private IntEventChannelSO maskDanceColorEvent;
    [SerializeField] private IntEventChannelSO maskDanceIndexEvent;
    [SerializeField] private BoolEventChannelSO groupDanceActiveEvent;

    private bool isGroupDanceActive;

    private void Start()
    {
        StartCoroutine(MaskDanceLoop());
        StartCoroutine(GroupDanceLoop());
    }

    private IEnumerator MaskDanceLoop()
    {
        while (true)
        {
            float wait = Random.Range(maskDanceMinInterval, maskDanceMaxInterval);
            yield return new WaitForSeconds(wait);

            if (isGroupDanceActive)
            {
                continue;
            }

            int color = Random.Range(0, 3);
            int danceIndex = Random.Range(1, 5);

            Debug.Log($"[DanceEvent] MaskDance start color={(MaskColor)color} dance={danceIndex} duration={maskDanceDuration:0.0}s", this);
            maskDanceColorEvent?.RaiseEvent(color);
            maskDanceIndexEvent?.RaiseEvent(danceIndex);
            maskDanceActiveEvent?.RaiseEvent(true);

            yield return new WaitForSeconds(maskDanceDuration);

            Debug.Log("[DanceEvent] MaskDance end", this);
            maskDanceActiveEvent?.RaiseEvent(false);
        }
    }

    private IEnumerator GroupDanceLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(groupDanceInterval);

            isGroupDanceActive = true;
            Debug.Log($"[DanceEvent] GroupDance start duration={groupDanceDuration:0.0}s", this);
            groupDanceActiveEvent?.RaiseEvent(true);

            yield return new WaitForSeconds(groupDanceDuration);

            groupDanceActiveEvent?.RaiseEvent(false);
            Debug.Log("[DanceEvent] GroupDance end", this);
            isGroupDanceActive = false;
        }
    }
}
