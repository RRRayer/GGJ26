using System;
using UnityEngine;

//[CreateAssetMenu(fileName = "SettingsSO", menuName = "Settings/Settings SO")]
[Serializable]
public class SettingsSO : ScriptableObject
{
    private float masterVolume;
    private float musicVolume;
    private float sfxVolume;
    private int resolutionIndex;
    private bool isFullScreen;
    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;
    public int ResolutionIndex => resolutionIndex;
    public bool IsFullScreen => isFullScreen;

    public void SaveAudioSettings(float masterVolume, float musicVolume, float sfxVolume)
    {
        this.masterVolume = masterVolume;
        this.musicVolume = musicVolume;
        this.sfxVolume = sfxVolume;
    }

    public void SaveGraphicsSettings(int resolutionIndex, bool isFullScreen)
    {
        this.resolutionIndex = resolutionIndex;
        this.isFullScreen = isFullScreen;
    }

    public void LoadSavedSettings(Save saveFile)
    {
        this.masterVolume = saveFile.MasterVolume;
        this.musicVolume = saveFile.MusicVolume;
        this.sfxVolume = saveFile.SfxVolume;
        this.resolutionIndex = saveFile.ResolutionIndex;
        this.isFullScreen = saveFile.IsFullScreen;
    }
}
