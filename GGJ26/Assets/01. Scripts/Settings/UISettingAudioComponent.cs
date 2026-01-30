using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UISettingsAudioComponent : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private UISettingsSlider masterVolumeSlider;
    [SerializeField] private UISettingsSlider musicVolumeSlider;
    [SerializeField] private UISettingsSlider sfxVolumeSlider;
    
    [Header("Broadcasting on")]
    [SerializeField] private FloatEventChannelSO changeMasterVolumeEvent;
    [SerializeField] private FloatEventChannelSO changeMusicVolumeEvent;
    [SerializeField] private FloatEventChannelSO changeSfxVolumeEvent;

    private float masterVolume;
    private float musicVolume;
    private float sfxVolume;
    
    private const int maxVolume = 100;

    private void OnEnable()
    {
        masterVolumeSlider.ValueChanged += SetMasterVolume;
        musicVolumeSlider .ValueChanged += SetMusicVolume;
        sfxVolumeSlider   .ValueChanged += SetSfxVolume;
    }

    private void OnDisable()
    {
        masterVolumeSlider.ValueChanged -= SetMasterVolume;
        musicVolumeSlider .ValueChanged -= SetMusicVolume;
        sfxVolumeSlider   .ValueChanged -= SetSfxVolume;
    }

    public void Setup(float newMasterVolume, float newMusicVolume, float newSfxVolume)
    {
        this.masterVolume = Mathf.Clamp01(newMasterVolume);
        this.musicVolume = Mathf.Clamp01(newMusicVolume);
        this.sfxVolume = Mathf.Clamp01(newSfxVolume);

        masterVolumeSlider.SetSlider(masterVolume * maxVolume);
        musicVolumeSlider .SetSlider(musicVolume * maxVolume);
        sfxVolumeSlider   .SetSlider(sfxVolume * maxVolume);
        
        SetMasterVolume();
        SetMusicVolume();
        SetSfxVolume();
    }
    
    private void SetMasterVolume()
    {
        changeMasterVolumeEvent?.OnEventRaised(masterVolume);
    }

    private void SetMasterVolume(float value)
    {
        masterVolume = value / maxVolume;
        changeMasterVolumeEvent?.OnEventRaised(masterVolume);
    }
    
    private void SetMusicVolume()
    {
        changeMusicVolumeEvent?.OnEventRaised(musicVolume);
    }

    private void SetMusicVolume(float value)
    {
        musicVolume = value / maxVolume;
        changeMusicVolumeEvent?.OnEventRaised(musicVolume);
    }

    private void SetSfxVolume()
    {
        changeSfxVolumeEvent?.OnEventRaised(sfxVolume);
    }
    
    private void SetSfxVolume(float value)
    {
        sfxVolume = value / maxVolume;
        changeSfxVolumeEvent?.OnEventRaised(sfxVolume);
    }

    public void SaveVolumes(SettingsSO currentSettings)
    {
        currentSettings.SaveAudioSettings(masterVolumeSlider.GetValue() / maxVolume, 
            musicVolumeSlider.GetValue() / maxVolume, 
            sfxVolumeSlider.GetValue() / maxVolume);
    }

    /// <summary>
    /// Default 값으로 바꾸고 설정 적용
    /// </summary>
    public void ResetVolumes(SettingsSO currentSettings)
    {
        currentSettings.SaveAudioSettings(1f, 0.8f, 1f);
        Setup(1f, 0.8f, 1f);
    }
}
