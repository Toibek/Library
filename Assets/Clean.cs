using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Clean
{
    public static void log(object message)
    {
#if UNITY_EDITOR
        Debug.Log(message);
#endif
    }
    public static void logWarning(object message)
    {
#if UNITY_EDITOR
        Debug.LogWarning(message);
#endif
    }
    public static void logError(object message)
    {
#if UNITY_EDITOR
        Debug.LogError(message);
#endif
    }

}
