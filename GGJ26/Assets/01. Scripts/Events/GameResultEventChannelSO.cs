using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "GameResultEventChannelSO", menuName = "Events/GameResult")]
public class GameResultEventChannelSO : ScriptableObject
{
    public UnityAction<GameResultData> OnEventRaised = delegate { };

    public void RaiseEvent(GameResultData value)
    {
        OnEventRaised?.Invoke(value);
    }
}
