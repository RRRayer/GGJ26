using System;

[Serializable]
public class PlayerState
{
    public string PlayerId;
    public bool IsSeeker;
    public bool IsDead;
    public int Eliminations;

    public PlayerState(string playerId, bool isSeeker)
    {
        PlayerId = playerId;
        IsSeeker = isSeeker;
    }
}
