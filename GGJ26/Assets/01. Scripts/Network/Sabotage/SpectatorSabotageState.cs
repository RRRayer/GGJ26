public struct SpectatorSabotageState
{
    public bool CanUseShoe;
    public bool CanUseSmoke;
    public bool CanUseDance;
    public SabotageType ArmedType;

    public static SpectatorSabotageState CreateDefault()
    {
        return new SpectatorSabotageState
        {
            CanUseShoe = true,
            CanUseSmoke = true,
            CanUseDance = true,
            ArmedType = SabotageType.None
        };
    }

    public bool CanUse(SabotageType type)
    {
        return type switch
        {
            SabotageType.ShoeToss => CanUseShoe,
            SabotageType.GhostSmoke => CanUseSmoke,
            SabotageType.PhantomDance => CanUseDance,
            _ => false
        };
    }

    public void Consume(SabotageType type)
    {
        switch (type)
        {
            case SabotageType.ShoeToss:
                CanUseShoe = false;
                break;
            case SabotageType.GhostSmoke:
                CanUseSmoke = false;
                break;
            case SabotageType.PhantomDance:
                CanUseDance = false;
                break;
        }
    }
}

