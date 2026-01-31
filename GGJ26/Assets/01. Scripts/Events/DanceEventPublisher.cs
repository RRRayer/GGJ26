using System;
using System.Collections;
using Fusion;
using UnityEngine;
using Random = UnityEngine.Random;

// ?ÑÏä§ ?ïÎ≥¥Î•??¥ÏùÑ Íµ¨Ï°∞Ï≤? ?§Î•∏ UI ?§ÌÅ¨Î¶ΩÌä∏?êÏÑú???ëÍ∑º?????àÎèÑÎ°?class Î∞îÍπ•???ïÏùò?©Îãà??
public struct DanceInfo
{
    public int DanceIndex;
    public MaskColor Color;
}

public class DanceEventPublisher : NetworkBehaviour
{
    // UI?êÍ≤å ?§Ïùå ?ÑÏä§ ?ïÎ≥¥Î•??ÑÎã¨???àÎ°ú??static ?¥Î≤§??
    public static event Action<DanceInfo> OnNextDanceAnnounced;
    // MaskDanceÍ∞Ä Ï¢ÖÎ£å?òÏóà?åÏùÑ UI???åÎ¶¨??static ?¥Î≤§??
    public static event Action OnMaskDanceEnded;
    public static bool IsAnyMaskDanceActive { get; private set; }
    public static bool IsGroupDanceActive { get; private set; }

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

    public override void Spawned()
    {
        if (Object.HasStateAuthority == false)
        {
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            StartCoroutine(MaskDanceLoop((MaskColor)i));
        }

        StartCoroutine(GroupDanceLoop());
    }

    private IEnumerator MaskDanceLoop(MaskColor color)
    {
        int colorIndex = (int)color;

        // ?úÏûë ??Í∞?ÏΩîÎ£®?¥Ïù¥ ?úÎ≤à???êÎ? Ï±ÑÏö∞ÏßÄ ?äÎèÑÎ°?Ï¥àÎ∞ò???úÎç§ ?úÎ†à?¥Î? Ï§çÎãà??
        yield return new WaitForSeconds(Random.Range(1f, 3f));

        while (true)
        {
            // 1. ?§Ïùå ?ÑÏä§ ?ïÎ≥¥Î•?ÎØ∏Î¶¨ Í≤∞Ï†ï?òÍ≥† UI???åÎ¶Ω?àÎã§.
            int danceIndex = Random.Range(0, 4);
            OnNextDanceAnnounced?.Invoke(new DanceInfo { DanceIndex = danceIndex, Color = color });

            // 2. ?ÑÏä§Í∞Ä ?§Ï†úÎ°??úÏûë?òÍ∏∞ ?ÑÍπåÏßÄ ?ÄÍ∏∞Ìï©?àÎã§. ???úÍ∞Ñ??UI ?êÏóê ?úÏãú?òÎäî ?úÍ∞Ñ???©Îãà??
            //    (?¨Í∏∞?úÎäî Í≥†Ï†ï??ÏßßÏ? ?úÍ∞Ñ??Ï£ºÍ±∞?? Í∏∞Ï°¥Ï≤òÎüº ?úÎç§Í∞íÏùÑ ?¨Ïö©?????àÏäµ?àÎã§. ?ºÎã® 3Ï¥àÎ°ú Í≥†Ï†ï?òÍ≤†?µÎãà??)
            yield return new WaitForSeconds(3f); 

            // 3. Í∑∏Î£π ?ÑÏä§Í∞Ä ?úÏûë?òÎ©¥ ?†Ïãú ?ÄÍ∏∞Ìï©?àÎã§.
            while (isGroupDanceActive)
            {
                yield return null;
            }
            
            // 4. ?ÑÏä§Î•??úÏûë?©Îãà??
            RpcStartMaskDance(colorIndex, danceIndex, maskDanceDuration);

            // 5. ?ÑÏä§ ÏßÄ???úÍ∞ÑÎßåÌÅº Í∏∞Îã§Î¶ΩÎãà??
            float elapsed = 0f;
            while (elapsed < maskDanceDuration && isGroupDanceActive == false)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 6. ?ÑÏä§Î•?Ï¢ÖÎ£å?òÍ≥†, UIÍ∞Ä ?êÏóê???ÑÏù¥?úÏùÑ ?åÎπÑ?òÎèÑÎ°??åÎ¶Ω?àÎã§.
            RpcStopMaskDance(colorIndex);

            // 7. --- ?µÏã¨ Î≥ÄÍ≤ΩÏ†ê ---
            // ?§Ïùå ?ÑÏä§Î•??àÍ≥†?òÍ∏∞ ?ÑÍπåÏßÄ Ï∂©Î∂Ñ???úÍ∞Ñ(Ïø®Îã§????Í∏∞Îã§Î¶ΩÎãà??
            float wait = Random.Range(maskDanceMinInterval, maskDanceMaxInterval);
            yield return new WaitForSeconds(wait);
        }
    }

    private IEnumerator GroupDanceLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(groupDanceInterval);

            RpcStartGroupDance(groupDanceDuration);

            yield return new WaitForSeconds(groupDanceDuration);

            RpcStopGroupDance();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcAnnounceNextDance(int colorIndex, int danceIndex)
    {
        if (colorIndex < 0 || colorIndex >= maskDanceIndexEvents.Length)
        {
            return;
        }

        OnNextDanceAnnounced?.Invoke(new DanceInfo { DanceIndex = danceIndex, Color = (MaskColor)colorIndex });
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcStartMaskDance(int colorIndex, int danceIndex, float duration)
    {
        if (colorIndex < 0 || colorIndex >= maskDanceIndexEvents.Length)
        {
            return;
        }

        maskDanceIndexEvents[colorIndex]?.RaiseEvent(danceIndex);
        maskDanceActive[colorIndex] = true;
        IsAnyMaskDanceActive = true;
        Debug.Log($"[DanceEvent] MaskDance start color={(MaskColor)colorIndex} dance={danceIndex} duration={duration:0.0}s", this);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcStopMaskDance(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= maskDanceIndexEvents.Length)
        {
            return;
        }

        maskDanceIndexEvents[colorIndex]?.RaiseEvent(-1);
        maskDanceActive[colorIndex] = false;
        maskDanceActive[colorIndex] = false;
        IsAnyMaskDanceActive = maskDanceActive[0] || maskDanceActive[1] || maskDanceActive[2];
        Debug.Log($"[DanceEvent] MaskDance end color={(MaskColor)colorIndex}", this);
        OnMaskDanceEnded?.Invoke();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcStartGroupDance(float duration)
    {
        isGroupDanceActive = true;
        isGroupDanceActive = true;
        IsGroupDanceActive = true;
        StopAllMaskDances();
        groupDanceActiveEvent?.RaiseEvent(true);
        startDiscoEvent?.RaiseEvent();
        Debug.Log($"[DanceEvent] GroupDance start duration={duration:0.0}s", this);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcStopGroupDance()
    {
        groupDanceActiveEvent?.RaiseEvent(false);
        stopDiscoEvent?.RaiseEvent();
        Debug.Log("[DanceEvent] GroupDance end", this);
        isGroupDanceActive = false;
        isGroupDanceActive = false;
        IsGroupDanceActive = false;
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
        IsAnyMaskDanceActive = false;
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

