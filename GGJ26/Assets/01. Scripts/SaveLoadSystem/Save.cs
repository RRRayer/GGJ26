using System.Collections.Generic;
using UnityEngine;

public class Save
{
    // Settings
    public float MasterVolume = 1.0f;
    public float MusicVolume = 0.8f;
    public float SfxVolume = 1.0f;
    public int ResolutionIndex;
    public bool IsFullScreen;
    
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    public void LoadFromJson(string json)
    {
        JsonUtility.FromJsonOverwrite(json, this);
    }

    public void SaveSettings(SettingsSO settings)
    {
        MasterVolume = settings.MasterVolume;
        MusicVolume = settings.MusicVolume;
        SfxVolume = settings.SfxVolume;
        ResolutionIndex = settings.ResolutionIndex;
        IsFullScreen = settings.IsFullScreen;
    }
}
