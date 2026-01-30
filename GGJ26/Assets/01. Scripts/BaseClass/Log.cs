using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class Log
{
    [Conditional("UNITY_EDITOR")]
    public static void D(object message)
    {
        Debug.Log(message);
    }
    
    [Conditional("UNITY_EDITOR")]
    public static void D(object message, Object context)
    {
        Debug.Log(message, context);
    }
    
    [Conditional("UNITY_EDITOR")]
    public static void W(object message)
    {
        Debug.LogWarning(message);
    }
    
    [Conditional("UNITY_EDITOR")]
    public static void W(object message, Object context)
    {
        Debug.LogWarning(message, context);
    }
    
    [Conditional("UNITY_EDITOR")]
    public static void E(object message)
    {
        Debug.LogError(message);
    }
    
    [Conditional("UNITY_EDITOR")]
    public static void E(object message, Object context)
    {
        Debug.LogError(message, context);
    }
}
