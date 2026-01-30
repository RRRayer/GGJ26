using System;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "StringEventChannelSO", menuName = "Events/String")]
public class StringEventChannelSO : ScriptableObject
{
    public UnityAction<String> OnEventRaised = delegate { };

    public void RaiseEvent(String value)
    {
        OnEventRaised?.Invoke(value);
    }
}