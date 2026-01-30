using UnityEngine;

public class SettingsSystem : MonoBehaviour
{
    [SerializeField] private SettingsSO currentSettings;
    [SerializeField] private SaveLoadSystem saveLoadSystem;
    
    [Header("Listening on")]
    [SerializeField] private VoidEventChannelSO saveSettingsEvent;
    
    [Header("Broadcasting on")]
    [SerializeField] private FloatEventChannelSO changeMasterVolumeEvent;
    [SerializeField] private FloatEventChannelSO changeMusicVolumeEvent;
    [SerializeField] private FloatEventChannelSO changeSfxVolumeEvent;
    [SerializeField] private IntEventChannelSO changeResolutionEvent;

    private void Awake()
    {
        saveLoadSystem.LoadSaveDataFromDisk();
        currentSettings.LoadSavedSettings(saveLoadSystem.SaveData);
    }

    private void OnEnable()
    {
        saveSettingsEvent.OnEventRaised += SaveSettings;
    }

    private void OnDisable()
    {
        saveSettingsEvent.OnEventRaised -= SaveSettings;
    }

    private void Start()
    {
        // AudioManager의 OnEnable에서 이벤트를 초기화하기 때문에 그 이후에 설정해야 함
        SetCurrentSettings();
    }

    private void SetCurrentSettings()
    {
        changeMasterVolumeEvent.RaiseEvent(currentSettings.MasterVolume);
        changeMusicVolumeEvent.RaiseEvent(currentSettings.MusicVolume);
        changeSfxVolumeEvent.RaiseEvent(currentSettings.SfxVolume);
        changeResolutionEvent.RaiseEvent(currentSettings.ResolutionIndex);
    }
    
    private void SaveSettings()
    {
        saveLoadSystem.SaveDataToDisk();
    }
}
