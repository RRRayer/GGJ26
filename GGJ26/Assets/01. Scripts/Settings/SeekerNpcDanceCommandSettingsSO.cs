using UnityEngine;

/// <summary>
/// Seeker NPC 춤 명령에 사용되는 설정값을 담는 ScriptableObject입니다.
/// </summary>
[CreateAssetMenu(fileName = "SeekerNpcDanceCommandSettingsSO", menuName = "Settings/Seeker NPC Dance Command")]
public class SeekerNpcDanceCommandSettingsSO : ScriptableObject
{
    // NPC 춤 명령의 반경입니다. (0 이상)
    [Min(0f)] public float radius = 8f;
    // NPC 춤의 지속 시간입니다. (0 이상)
    [Min(0f)] public float duration = 2.5f;
    // NPC 춤 명령의 재사용 대기 시간입니다. (0 이상)
    [Min(0f)] public float cooldown = 0.5f;
}
