using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "GameStateEventChannelSO", menuName = "Events/GameState")]
public class GameStateEventChannelSO : DescriptionSO
{
    public UnityAction<GameState> OnEventRaised = delegate { };

    public void RaiseEvent(GameState value)
    {
        OnEventRaised?.Invoke(value);
    }
}