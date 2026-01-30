using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "VoidEventChannelSO", menuName = "Events/Void")]
public class VoidEventChannelSO : DescriptionSO
{
    public UnityAction OnEventRaised = delegate { };

    public void RaiseEvent()
    {
        OnEventRaised?.Invoke();
    }
}