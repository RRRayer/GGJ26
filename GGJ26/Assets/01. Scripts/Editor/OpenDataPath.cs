using UnityEditor;
using UnityEngine;

public class OpenDataPath
{
    [MenuItem("Tools/Open Persistent Data Path")]
    private static void OpenPersistentDataPath()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}