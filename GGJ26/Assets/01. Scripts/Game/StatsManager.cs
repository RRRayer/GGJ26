using System.Collections.Generic;
using UnityEngine;

public class StatsManager : MonoBehaviour
{
    private readonly List<float> reactionSeconds = new List<float>();
    private readonly List<MaskColor> maskHistory = new List<MaskColor>();

    public void ResetStats()
    {
        reactionSeconds.Clear();
        maskHistory.Clear();
    }

    public void RecordReactionSeconds(float seconds)
    {
        if (seconds < 0f)
        {
            return;
        }

        reactionSeconds.Add(seconds);
    }

    public void RegisterMaskChange(MaskColor color)
    {
        maskHistory.Add(color);
    }

    public float GetAverageReactionMs()
    {
        if (reactionSeconds.Count == 0)
        {
            return 0f;
        }

        float total = 0f;
        for (int i = 0; i < reactionSeconds.Count; i++)
        {
            total += reactionSeconds[i];
        }

        return (total / reactionSeconds.Count) * 1000f;
    }

    public List<MaskColor> GetMaskHistory()
    {
        return new List<MaskColor>(maskHistory);
    }
}
