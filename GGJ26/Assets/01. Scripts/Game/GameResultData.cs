using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameResultData
{
    public bool SeekerWin;
    public bool LocalPlayerWin;
    public float RemainingTime;
    public float AverageReactionMs;
    public List<MaskColor> MaskHistory;

    public GameResultData(bool seekerWin, bool localPlayerWin, float remainingTime, float averageReactionMs, List<MaskColor> maskHistory)
    {
        SeekerWin = seekerWin;
        LocalPlayerWin = localPlayerWin;
        RemainingTime = remainingTime;
        AverageReactionMs = averageReactionMs;
        MaskHistory = maskHistory;
    }
}
