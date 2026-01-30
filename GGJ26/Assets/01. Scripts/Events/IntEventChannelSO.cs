using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "IntEventChannelSO", menuName = "Events/Int")]
public class IntEventChannelSO : ScriptableObject
{
    public UnityAction<int> OnEventRaised = delegate { };

    public void RaiseEvent(int value)
    {
        OnEventRaised?.Invoke(value);
    }
}