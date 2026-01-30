using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "PopupTypeEventChannelSO", menuName = "Events/PopupType")]
public class PopupTypeEventChannelSO : ScriptableObject
{
    public UnityAction<PopupType> OnEventRaised = delegate { };

    public void RaiseEvent(PopupType value)
    {
        OnEventRaised?.Invoke(value);
    }
}