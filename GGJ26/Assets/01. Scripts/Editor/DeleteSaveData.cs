using UnityEditor;
using UnityEngine;

public class DeleteSaveData : MonoBehaviour
{
    [MenuItem("Tools/Delete Save Data")]
    private static void DeleteData()
    {
        if (FileManager.DeleteFile("save.drilling"))
        {
            Debug.Log($"[FileManager] Save file deleted successfully");
        }
        else
        {
            Log.E("[FileManager] Failed to delete save data.");
        }
    }
}
