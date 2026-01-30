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

    [Header("Mask Dance Events (per color)")]
    [SerializeField] private IntEventChannelSO[] maskDanceIndexEvents = new IntEventChannelSO[3];

    [Header("Group Dance Events")]
    [SerializeField] private BoolEventChannelSO groupDanceActiveEvent;

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
        while (true)
        {
            float wait = Random.Range(maskDanceMinInterval, maskDanceMaxInterval);
            yield return new WaitForSeconds(wait);

            while (isGroupDanceActive)
            {
                yield return null;
            }

            int danceIndex = Random.Range(1, 5);
            maskDanceIndexEvents[colorIndex]?.RaiseEvent(danceIndex);
            maskDanceActive[colorIndex] = true;
            Debug.Log($"[DanceEvent] MaskDance start color={color} dance={danceIndex} duration={maskDanceDuration:0.0}s", this);

            float elapsed = 0f;
            while (elapsed < maskDanceDuration && isGroupDanceActive == false)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            maskDanceIndexEvents[colorIndex]?.RaiseEvent(-1);
            maskDanceActive[colorIndex] = false;
            Debug.Log($"[DanceEvent] MaskDance end color={color}", this);
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
            Debug.Log($"[DanceEvent] GroupDance start duration={groupDanceDuration:0.0}s", this);

            yield return new WaitForSeconds(groupDanceDuration);

            groupDanceActiveEvent?.RaiseEvent(false);
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
