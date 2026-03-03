using Fusion;
using UnityEngine;

public struct PlayerInputData : INetworkInput
{
    public Vector2 Move;
    public Vector2 Look;
    public bool Jump;
    public bool Sprint;
    public int danceIndex;
    public bool npcDanceCommand;
    public bool sabotageArm1;
    public bool sabotageArm2;
    public bool sabotageArm3;
    public bool sabotageExecute;
}
