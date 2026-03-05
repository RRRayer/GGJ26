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
    private int windowModeIndex;
    private bool isFullScreen;
    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;
    public int ResolutionIndex => resolutionIndex;
    public int WindowModeIndex => windowModeIndex;
    public bool IsFullScreen => isFullScreen;

    public void SaveAudioSettings(float masterVolume, float musicVolume, float sfxVolume)
    {
        this.masterVolume = masterVolume;
        this.musicVolume = musicVolume;
        this.sfxVolume = sfxVolume;
    }

    public void SaveGraphicsSettings(int resolutionIndex, bool isFullScreen)
    {
        SaveGraphicsSettings(resolutionIndex, isFullScreen ? 0 : 1);
    }

    public void SaveGraphicsSettings(int resolutionIndex, int windowModeIndex)
    {
        this.resolutionIndex = Mathf.Max(0, resolutionIndex);
        this.windowModeIndex = Mathf.Clamp(windowModeIndex, 0, 2);

        // TODO: Remove legacy bool after old save compatibility is no longer required.
        this.isFullScreen = this.windowModeIndex != 1;
    }

    public void LoadSavedSettings(Save saveFile)
    {
        if (saveFile == null)
        {
            return;
        }

        this.masterVolume = saveFile.MasterVolume;
        this.musicVolume = saveFile.MusicVolume;
        this.sfxVolume = saveFile.SfxVolume;
        this.resolutionIndex = Mathf.Max(0, saveFile.ResolutionIndex);

        int loadedWindowModeIndex = saveFile.WindowModeIndex;
        if (saveFile.IsFullScreen == false && loadedWindowModeIndex == 0)
        {
            loadedWindowModeIndex = 1;
        }

        this.windowModeIndex = Mathf.Clamp(loadedWindowModeIndex, 0, 2);

        // TODO: Remove legacy bool after old save compatibility is no longer required.
        this.isFullScreen = this.windowModeIndex != 1;
    }
}
