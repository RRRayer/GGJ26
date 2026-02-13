using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Seeker NPC에게 춤을 명령하는 데 사용되는 데이터 구조입니다.
/// </summary>
[System.Serializable]
public struct SeekerNpcDanceCommand
{
    // 춤 명령의 중심 좌표입니다.
    public Vector3 center;
    // 춤 명령이 적용될 반경입니다.
    public float radius;
    // NPC가 실행할 춤의 인덱스입니다.
    public int danceIndex;
    // 춤의 지속 시간입니다.
    public float duration;
}

/// <summary>
/// Seeker NPC 춤 명령을 전달하는 ScriptableObject 기반 이벤트 채널입니다.
/// </summary>
[CreateAssetMenu(fileName = "SeekerNpcDanceCommandEventChannelSO", menuName = "Events/SeekerNpcDanceCommand")]
public class SeekerNpcDanceCommandEventChannelSO : ScriptableObject
{
    /// <summary>
    /// SeekerNpcDanceCommand 이벤트가 발생했을 때 호출되는 UnityAction입니다.
    /// </summary>
    public UnityAction<SeekerNpcDanceCommand> OnEventRaised = delegate { };

    /// <summary>
    /// SeekerNpcDanceCommand 이벤트를 발생시킵니다.
    /// </summary>
    /// <param name="command">발생시킬 SeekerNpcDanceCommand 데이터입니다.</param>
    public void RaiseEvent(SeekerNpcDanceCommand command)
    {
        OnEventRaised?.Invoke(command);
    }
}
