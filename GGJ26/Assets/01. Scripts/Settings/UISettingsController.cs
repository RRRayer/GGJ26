using UnityEngine;
using UnityEngine.Events;

public class UISettingsController : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private UIGenericButton backButton;
    [SerializeField] private UIGenericButton saveButton;
    [SerializeField] private UIGenericButton resetButton;
    
    [Header("Settings")]
    [SerializeField] private SettingsSO currentSettings;
    [SerializeField] private UISettingsAudioComponent audioComponent;
    [SerializeField] private UISettingsGraphicsComponent graphicsComponent;
    
    [Header("Broadcasting on")]
    [SerializeField] private VoidEventChannelSO saveSettingsEvent;

    public event UnityAction CloseButtonAction;

    private void OnEnable()
    {
        backButton.Clicked  += CloseSettingPanel;
        saveButton.Clicked  += SaveSettings;
        resetButton.Clicked += ResetSettings;

        ShowSettingPanel();
    }

    private void OnDisable()
    {
        backButton.Clicked  -= CloseSettingPanel;
        saveButton.Clicked  -= SaveSettings;
        resetButton.Clicked -= ResetSettings;
    }

    private void ShowSettingPanel()
    {
        audioComponent.Setup(currentSettings.MasterVolume, currentSettings.MusicVolume, currentSettings.SfxVolume);
        graphicsComponent.Setup(currentSettings.ResolutionIndex, currentSettings.IsFullScreen);
    }

    private void CloseSettingPanel()
    {
        CloseButtonAction?.Invoke();
    }

    private void SaveSettings()
    {
        audioComponent.SaveVolumes(currentSettings);
        graphicsComponent.SaveGraphics(currentSettings);
        
        saveSettingsEvent.RaiseEvent();
    }

    private void ResetSettings()
    {
        audioComponent.ResetVolumes(currentSettings);
        graphicsComponent.ResetGraphics(currentSettings);
        
        saveSettingsEvent.RaiseEvent();
    }
}
