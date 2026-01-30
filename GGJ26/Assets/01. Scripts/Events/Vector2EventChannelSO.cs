using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "Vector2EventChannelSO", menuName = "Events/Vector2")]
public class Vector2EventChannelSO : ScriptableObject
{
    public UnityAction<Vector2> OnEventRaised = delegate { };

    public void RaiseEvent(Vector2 value)
    {
        OnEventRaised?.Invoke(value);
    }
}
