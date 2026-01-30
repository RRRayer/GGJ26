using System;
using UnityEngine;

public class SaveDebugger : MonoBehaviour
{
    [TextArea] public string description;
    
    [SerializeField] private SaveLoadSystem saveLoadSystem;
    public bool state = false;
    
    private void Update()
    {
        if (state)
        {
            Log.D("AudioDebugging::Update()");
            state = false;
            saveLoadSystem.SaveDataToDisk();
        }
    }
}
