using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISoundSetting : MonoBehaviour
{
    [Header("Slider Wrappers (Optional)")]
    [SerializeField] private UISettingsSlider masterSliderWrapper;
    [SerializeField] private UISettingsSlider bgmSliderWrapper;
    [SerializeField] private UISettingsSlider sfxSliderWrapper;
    [SerializeField] private UISettingsSlider voiceVolumeSliderWrapper;

    [Header("Slider (Fallback)")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider voiceVolumeSlider;

    [Header("Dropdown Wrappers (Optional)")]
    [SerializeField] private UISettingsDropdown voiceModeDropdownWrapper;
    [SerializeField] private UISettingsDropdown inputDeviceDropdownWrapper;

    [Header("Dropdown (Fallback)")]
    [SerializeField] private TMP_Dropdown voiceModeDropdown;
    [SerializeField] private TMP_Dropdown inputDeviceDropdown;

    [Header("Data")]
    [SerializeField] private SettingsSO currentSettings;
    [SerializeField] private SaveLoadSystem saveLoadSystem;

    [Header("Audio Events")]
    [SerializeField] private FloatEventChannelSO changeMasterVolumeEvent;
    [SerializeField] private FloatEventChannelSO changeMusicVolumeEvent;
    [SerializeField] private FloatEventChannelSO changeSfxVolumeEvent;

    [Header("Runtime")]
    [SerializeField] private VoiceRuntimeSettingsController voiceRuntimeSettingsController;

    private readonly List<string> microphoneDeviceNames = new List<string>();
    private bool suppressEvents;
    private float masterVolume = 1f;
    private float bgmVolume = 0.8f;
    private float sfxVolume = 1f;
    private float voiceVolume = 1f;
    private int voiceModeIndex = 1;
    private string voiceInputDeviceName = string.Empty;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindEvents();
        Initialize();
    }

    private void OnDisable()
    {
        UnbindEvents();
    }

    private void Initialize()
    {
        LoadSavedData();
        BuildDropdownOptions();
        PullFromSettings();
        ApplyCurrentValuesToUi();
        ApplyAudioChannels();
        RequestVoiceRuntimeApply();
    }

    private void LoadSavedData()
    {
        if (saveLoadSystem == null || currentSettings == null)
        {
            return;
        }

        saveLoadSystem.LoadSaveDataFromDisk();
        currentSettings.LoadSavedSettings(saveLoadSystem.SaveData);
    }

    private void PullFromSettings()
    {
        if (currentSettings != null)
        {
            masterVolume = Mathf.Clamp01(currentSettings.MasterVolume);
            bgmVolume = Mathf.Clamp01(currentSettings.MusicVolume);
            sfxVolume = Mathf.Clamp01(currentSettings.SfxVolume);
            voiceVolume = Mathf.Clamp01(currentSettings.VoiceVolume);
            voiceModeIndex = Mathf.Clamp(currentSettings.VoiceModeIndex, 0, 1);
            voiceInputDeviceName = currentSettings.VoiceInputDeviceName ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(voiceInputDeviceName) == false)
        {
            int found = microphoneDeviceNames.IndexOf(voiceInputDeviceName);
            if (found < 0)
            {
                voiceInputDeviceName = string.Empty;
            }
        }
    }

    private void BuildDropdownOptions()
    {
        List<string> modeOptions = new List<string>
        {
            "Push To Talk",
            "Voice Detection"
        };
        SetOptions(voiceModeDropdownWrapper, voiceModeDropdown, modeOptions);

        microphoneDeviceNames.Clear();
        string[] devices = Microphone.devices;
        for (int i = 0; i < devices.Length; i++)
        {
            string device = devices[i];
            if (string.IsNullOrWhiteSpace(device))
            {
                continue;
            }

            if (microphoneDeviceNames.Contains(device) == false)
            {
                microphoneDeviceNames.Add(device);
            }
        }

        List<string> inputOptions = new List<string> { "Default" };
        inputOptions.AddRange(microphoneDeviceNames);
        SetOptions(inputDeviceDropdownWrapper, inputDeviceDropdown, inputOptions);
    }

    private void ApplyCurrentValuesToUi()
    {
        suppressEvents = true;

        SetSliderValue(masterSliderWrapper, masterSlider, masterVolume);
        SetSliderValue(bgmSliderWrapper, bgmSlider, bgmVolume);
        SetSliderValue(sfxSliderWrapper, sfxSlider, sfxVolume);
        SetSliderValue(voiceVolumeSliderWrapper, voiceVolumeSlider, voiceVolume);

        SetValue(voiceModeDropdownWrapper, voiceModeDropdown, voiceModeIndex);

        int inputIndex = 0;
        if (string.IsNullOrWhiteSpace(voiceInputDeviceName) == false)
        {
            int found = microphoneDeviceNames.IndexOf(voiceInputDeviceName);
            inputIndex = found >= 0 ? found + 1 : 0;
        }
        SetValue(inputDeviceDropdownWrapper, inputDeviceDropdown, inputIndex);

        Refresh(voiceModeDropdownWrapper, voiceModeDropdown);
        Refresh(inputDeviceDropdownWrapper, inputDeviceDropdown);

        suppressEvents = false;
    }

    private void BindEvents()
    {
        UnbindEvents();

        BindSlider(masterSliderWrapper, masterSlider, OnMasterVolumeChanged);
        BindSlider(bgmSliderWrapper, bgmSlider, OnBgmVolumeChanged);
        BindSlider(sfxSliderWrapper, sfxSlider, OnSfxVolumeChanged);
        BindSlider(voiceVolumeSliderWrapper, voiceVolumeSlider, OnVoiceVolumeChanged);

        BindDropdown(voiceModeDropdownWrapper, voiceModeDropdown, OnVoiceModeChanged);
        BindDropdown(inputDeviceDropdownWrapper, inputDeviceDropdown, OnInputDeviceChanged);
    }

    private void UnbindEvents()
    {
        UnbindSlider(masterSliderWrapper, masterSlider, OnMasterVolumeChanged);
        UnbindSlider(bgmSliderWrapper, bgmSlider, OnBgmVolumeChanged);
        UnbindSlider(sfxSliderWrapper, sfxSlider, OnSfxVolumeChanged);
        UnbindSlider(voiceVolumeSliderWrapper, voiceVolumeSlider, OnVoiceVolumeChanged);

        UnbindDropdown(voiceModeDropdownWrapper, voiceModeDropdown, OnVoiceModeChanged);
        UnbindDropdown(inputDeviceDropdownWrapper, inputDeviceDropdown, OnInputDeviceChanged);
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (suppressEvents)
        {
            return;
        }

        masterVolume = Mathf.Clamp01(value);
        changeMasterVolumeEvent?.RaiseEvent(masterVolume);
        SaveAudioSettings();
    }

    private void OnBgmVolumeChanged(float value)
    {
        if (suppressEvents)
        {
            return;
        }

        bgmVolume = Mathf.Clamp01(value);
        changeMusicVolumeEvent?.RaiseEvent(bgmVolume);
        SaveAudioSettings();
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (suppressEvents)
        {
            return;
        }

        sfxVolume = Mathf.Clamp01(value);
        changeSfxVolumeEvent?.RaiseEvent(sfxVolume);
        SaveAudioSettings();
    }

    private void OnVoiceVolumeChanged(float value)
    {
        if (suppressEvents)
        {
            return;
        }

        voiceVolume = Mathf.Clamp01(value);
        SaveVoiceSettings();
        RequestVoiceRuntimeApply();
    }

    private void OnVoiceModeChanged(int index)
    {
        if (suppressEvents)
        {
            return;
        }

        voiceModeIndex = Mathf.Clamp(index, 0, 1);
        SaveVoiceSettings();
        RequestVoiceRuntimeApply();
    }

    private void OnInputDeviceChanged(int index)
    {
        if (suppressEvents)
        {
            return;
        }

        int safeIndex = Mathf.Max(0, index);
        if (safeIndex == 0)
        {
            voiceInputDeviceName = string.Empty;
        }
        else
        {
            int deviceIndex = safeIndex - 1;
            if (deviceIndex >= 0 && deviceIndex < microphoneDeviceNames.Count)
            {
                voiceInputDeviceName = microphoneDeviceNames[deviceIndex];
            }
            else
            {
                voiceInputDeviceName = string.Empty;
            }
        }

        SaveVoiceSettings();
        RequestVoiceRuntimeApply();
    }

    private void SaveAudioSettings()
    {
        if (currentSettings != null)
        {
            currentSettings.SaveAudioSettings(masterVolume, bgmVolume, sfxVolume);
        }

        SaveToDisk();
    }

    private void SaveVoiceSettings()
    {
        if (currentSettings != null)
        {
            currentSettings.SaveVoiceSettings(voiceVolume, voiceModeIndex, voiceInputDeviceName);
        }

        SaveToDisk();
    }

    private void SaveToDisk()
    {
        if (saveLoadSystem != null)
        {
            saveLoadSystem.SaveDataToDisk();
        }
    }

    private void ApplyAudioChannels()
    {
        changeMasterVolumeEvent?.RaiseEvent(masterVolume);
        changeMusicVolumeEvent?.RaiseEvent(bgmVolume);
        changeSfxVolumeEvent?.RaiseEvent(sfxVolume);
    }

    private void RequestVoiceRuntimeApply()
    {
        if (voiceRuntimeSettingsController == null)
        {
            voiceRuntimeSettingsController = FindFirstObjectByType<VoiceRuntimeSettingsController>(FindObjectsInactive.Include);
        }

        if (voiceRuntimeSettingsController != null)
        {
            voiceRuntimeSettingsController.ApplyFromCurrentSettings();
        }
    }

    private void ResolveReferences()
    {
        ResolveSliderRefs(ref masterSliderWrapper, ref masterSlider, "Master");
        ResolveSliderRefs(ref bgmSliderWrapper, ref bgmSlider, "BGM");
        ResolveSliderRefs(ref sfxSliderWrapper, ref sfxSlider, "SFX");
        ResolveSliderRefs(ref voiceVolumeSliderWrapper, ref voiceVolumeSlider, "UISlider");

        ResolveDropdownRefs(ref voiceModeDropdownWrapper, ref voiceModeDropdown, "VMDropdown");
        ResolveDropdownRefs(ref inputDeviceDropdownWrapper, ref inputDeviceDropdown, "IDDropDown", "IDDropdown");

        if (currentSettings == null)
        {
            currentSettings = FindAssetByName<SettingsSO>("SettingsSO");
        }

        if (saveLoadSystem == null)
        {
            saveLoadSystem = FindAssetByName<SaveLoadSystem>("SaveLoadSystem");
        }

        if (changeMasterVolumeEvent == null)
        {
            changeMasterVolumeEvent = FindAssetByName<FloatEventChannelSO>("ChangeMasterVolumeEventChannelSO");
        }

        if (changeMusicVolumeEvent == null)
        {
            changeMusicVolumeEvent = FindAssetByName<FloatEventChannelSO>("ChangeMusicVolumeEventChannelSO");
        }

        if (changeSfxVolumeEvent == null)
        {
            changeSfxVolumeEvent = FindAssetByName<FloatEventChannelSO>("ChangeSfxVolumeEventChannelSO");
        }

        if (voiceRuntimeSettingsController == null)
        {
            voiceRuntimeSettingsController = FindFirstObjectByType<VoiceRuntimeSettingsController>(FindObjectsInactive.Include);
        }
    }

    private void ResolveSliderRefs(ref UISettingsSlider wrapper, ref Slider slider, params string[] names)
    {
        if (wrapper != null || slider != null)
        {
            if (slider == null && wrapper != null)
            {
                slider = wrapper.GetComponentInChildren<Slider>(true);
            }
            return;
        }

        Transform target = FindByNames(names);
        if (target == null)
        {
            return;
        }

        wrapper = target.GetComponent<UISettingsSlider>();
        if (wrapper == null)
        {
            wrapper = target.GetComponentInChildren<UISettingsSlider>(true);
        }

        slider = target.GetComponent<Slider>();
        if (slider == null)
        {
            slider = target.GetComponentInChildren<Slider>(true);
        }
    }

    private void ResolveDropdownRefs(ref UISettingsDropdown wrapper, ref TMP_Dropdown dropdown, params string[] names)
    {
        if (wrapper != null || dropdown != null)
        {
            if (dropdown == null && wrapper != null)
            {
                dropdown = wrapper.GetComponentInChildren<TMP_Dropdown>(true);
            }
            return;
        }

        Transform target = FindByNames(names);
        if (target == null)
        {
            return;
        }

        wrapper = target.GetComponent<UISettingsDropdown>();
        if (wrapper == null)
        {
            wrapper = target.GetComponentInChildren<UISettingsDropdown>(true);
        }

        dropdown = target.GetComponent<TMP_Dropdown>();
        if (dropdown == null)
        {
            dropdown = target.GetComponentInChildren<TMP_Dropdown>(true);
        }
    }

    private Transform FindByNames(params string[] names)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform candidate = children[i];
            if (candidate == null)
            {
                continue;
            }

            for (int j = 0; j < names.Length; j++)
            {
                if (candidate.name == names[j])
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static T FindAssetByName<T>(string assetName) where T : ScriptableObject
    {
        T[] assets = Resources.FindObjectsOfTypeAll<T>();
        for (int i = 0; i < assets.Length; i++)
        {
            T asset = assets[i];
            if (asset != null && asset.name == assetName)
            {
                return asset;
            }
        }

        return null;
    }

    private static void SetSliderValue(UISettingsSlider wrapper, Slider slider, float value)
    {
        if (wrapper != null)
        {
            wrapper.SetSlider(value);
            return;
        }

        if (slider != null)
        {
            slider.value = value;
        }
    }

    private static void BindSlider(UISettingsSlider wrapper, Slider slider, UnityEngine.Events.UnityAction<float> callback)
    {
        if (wrapper != null)
        {
            wrapper.ValueChanged += callback;
            return;
        }

        if (slider != null)
        {
            slider.onValueChanged.AddListener(callback);
        }
    }

    private static void UnbindSlider(UISettingsSlider wrapper, Slider slider, UnityEngine.Events.UnityAction<float> callback)
    {
        if (wrapper != null)
        {
            wrapper.ValueChanged -= callback;
            return;
        }

        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(callback);
        }
    }

    private static void BindDropdown(UISettingsDropdown wrapper, TMP_Dropdown dropdown, UnityEngine.Events.UnityAction<int> callback)
    {
        if (wrapper != null)
        {
            wrapper.ValueChanged += callback;
            return;
        }

        if (dropdown != null)
        {
            dropdown.onValueChanged.AddListener(callback);
        }
    }

    private static void UnbindDropdown(UISettingsDropdown wrapper, TMP_Dropdown dropdown, UnityEngine.Events.UnityAction<int> callback)
    {
        if (wrapper != null)
        {
            wrapper.ValueChanged -= callback;
            return;
        }

        if (dropdown != null)
        {
            dropdown.onValueChanged.RemoveListener(callback);
        }
    }

    private static void SetOptions(UISettingsDropdown wrapper, TMP_Dropdown dropdown, List<string> options)
    {
        if (wrapper != null)
        {
            wrapper.ClearOptions();
            wrapper.AddOptions(options);
            wrapper.RefreshShownValue();
            return;
        }

        if (dropdown != null)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdown.RefreshShownValue();
        }
    }

    private static void SetValue(UISettingsDropdown wrapper, TMP_Dropdown dropdown, int value)
    {
        if (wrapper != null)
        {
            wrapper.SetValue(value);
            return;
        }

        if (dropdown != null)
        {
            dropdown.value = value;
        }
    }

    private static void Refresh(UISettingsDropdown wrapper, TMP_Dropdown dropdown)
    {
        if (wrapper != null)
        {
            wrapper.RefreshShownValue();
            return;
        }

        if (dropdown != null)
        {
            dropdown.RefreshShownValue();
        }
    }
}
