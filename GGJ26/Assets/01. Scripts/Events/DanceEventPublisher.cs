using System;
using System.Collections;
using Fusion;
using UnityEngine;
using Random = UnityEngine.Random;

public struct DanceInfo
{
    public int DanceIndex;
    public MaskColor Color;
}

public class DanceEventPublisher : NetworkBehaviour
{
    // 다음 춤 이벤트를 UI에 알립니다. (춤 종류, 색상 등)
    public static event Action<DanceInfo> OnNextDanceAnnounced;
    // 마스크 댄스 종료 시 호출됩니다.
    public static event Action OnMaskDanceEnded;
    // Seeker NPC의 춤 명령 이벤트를 발생시킵니다.
    public static event Action<SeekerNpcDanceCommand> OnSeekerNpcDanceCommand;

    public static bool IsAnyMaskDanceActive { get; private set; }
    public static bool IsGroupDanceActive { get; private set; }

    [Header("Mask Dance Timing")]
    // 마스크 댄스 루프 활성화 여부를 결정합니다. 개발/테스트용으로 사용될 수 있습니다.
    [SerializeField] private bool enableMaskDanceLoop = false;
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
    // Seeker NPC 춤 명령 이벤트를 발행하는 ScriptableObject입니다.
    [SerializeField] private SeekerNpcDanceCommandEventChannelSO seekerNpcDanceCommandEvent;

    private bool isGroupDanceActive;
    private readonly bool[] maskDanceActive = new bool[3];

    public override void Spawned()
    {
        if (Object.HasStateAuthority == false)
        {
            return;
        }

        if (enableMaskDanceLoop)
        {
            for (int i = 0; i < 3; i++)
            {
                StartCoroutine(MaskDanceLoop((MaskColor)i));
            }
        }

        StartCoroutine(GroupDanceLoop());
    }

    /// <summary>
    /// Seeker NPC 춤을 요청하는 메서드.
    /// 클라이언트에서 호출되어 State Authority로 RPC를 보냅니다.
    /// </summary>
    public void RequestSeekerNpcDance(Vector3 center, float radius, float duration)
    {
        if (Runner == null || Runner.IsRunning == false)
        {
            return;
        }

        if (Object != null && Object.HasStateAuthority)
        {
            TriggerSeekerNpcDance(center, radius, duration);
            return;
        }

        RpcRequestSeekerNpcDance(center, radius, duration);
    }

    /// <summary>
    /// Seeker NPC 춤 요청을 State Authority에 전달하는 RPC.
    /// 모든 클라이언트에서 호출 가능하며, 서버에서 TriggerSeekerNpcDance를 호출합니다.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RpcRequestSeekerNpcDance(Vector3 center, float radius, float duration)
    {
        TriggerSeekerNpcDance(center, radius, duration);
    }

    /// <summary>
    /// Seeker NPC 춤을 실제로 발생시키는 로직.
    /// 그룹 댄스가 활성화 중이면 실행되지 않습니다.
    /// </summary>
    private void TriggerSeekerNpcDance(Vector3 center, float radius, float duration)
    {
        if (isGroupDanceActive)
        {
            return;
        }

        int danceIndex = Random.Range(0, 4);
        RpcDispatchSeekerNpcDance(center, radius, danceIndex, duration);
    }

    /// <summary>
    /// Seeker NPC 춤 명령을 모든 클라이언트에 전파하는 RPC.
    /// ScriptableObject 이벤트를 발생시키고, 정적 C# 이벤트도 호출합니다.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcDispatchSeekerNpcDance(Vector3 center, float radius, int danceIndex, float duration)
    {
        SeekerNpcDanceCommand command = new SeekerNpcDanceCommand
        {
            center = center,
            radius = radius,
            danceIndex = danceIndex,
            duration = duration
        };

        if (seekerNpcDanceCommandEvent != null)
        {
            seekerNpcDanceCommandEvent.RaiseEvent(command);
        }

        OnSeekerNpcDanceCommand?.Invoke(command);
    }

    private IEnumerator MaskDanceLoop(MaskColor color)
    {
        int colorIndex = (int)color;

        yield return new WaitForSeconds(Random.Range(1f, 3f));

        while (true)
        {
            int danceIndex = Random.Range(0, 4);
            OnNextDanceAnnounced?.Invoke(new DanceInfo { DanceIndex = danceIndex, Color = color });

            yield return new WaitForSeconds(3f);

            while (isGroupDanceActive)
            {
                yield return null;
            }

            RpcStartMaskDance(colorIndex, danceIndex, maskDanceDuration);

            float elapsed = 0f;
            while (elapsed < maskDanceDuration && isGroupDanceActive == false)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            RpcStopMaskDance(colorIndex);

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
    private void RpcStartMaskDance(int colorIndex, int danceIndex, float duration)
    {
        if (colorIndex < 0 || colorIndex >= maskDanceIndexEvents.Length)
        {
            return;
        }

        maskDanceIndexEvents[colorIndex]?.RaiseEvent(danceIndex);
        maskDanceActive[colorIndex] = true;
        IsAnyMaskDanceActive = true;
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
        IsAnyMaskDanceActive = maskDanceActive[0] || maskDanceActive[1] || maskDanceActive[2];
        OnMaskDanceEnded?.Invoke();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcStartGroupDance(float duration)
    {
        isGroupDanceActive = true;
        IsGroupDanceActive = true;
        StopAllMaskDances();
        groupDanceActiveEvent?.RaiseEvent(true);
        startDiscoEvent?.RaiseEvent();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcStopGroupDance()
    {
        groupDanceActiveEvent?.RaiseEvent(false);
        stopDiscoEvent?.RaiseEvent();
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
}
