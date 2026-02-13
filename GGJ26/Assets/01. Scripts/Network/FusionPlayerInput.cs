using Fusion;
using UnityEngine;

public struct PlayerInputData : INetworkInput
{
    public Vector2 Move;
    public Vector2 Look;
    public bool Jump;
    public bool Sprint;
    public int danceIndex;
    // NPC 춤 명령을 위한 입력 값입니다.
    public bool npcDanceCommand;
}
