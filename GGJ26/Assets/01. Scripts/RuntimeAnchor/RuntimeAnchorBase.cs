using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class RuntimeAnchorBase<T> : DescriptionSO where T : UnityEngine.Object
{
    public UnityAction OnAnchorPrivided;

    private T value;
    public T Value => value;

    [ReadOnly] public bool IsSet = false;

    public void Provide(T v)
    {
        if (v == null)
        {
            Log.D("null value가 들어왔습니다.");
            return;
        }

        value = v;
        IsSet = true;
        
        OnAnchorPrivided?.Invoke();
    }

    public void UnSet()
    {
        value = null;
        IsSet = false;
    }

    private void OnDisable()
    {
        UnSet();
    }
}
