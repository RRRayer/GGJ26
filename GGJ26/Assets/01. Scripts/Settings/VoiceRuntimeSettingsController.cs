using System.Collections.Generic;
using Photon.Voice;
using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class VoiceRuntimeSettingsController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private SettingsSO currentSettings;
    [SerializeField] private SaveLoadSystem saveLoadSystem;

    [Header("Runtime")]
    [SerializeField] private float refreshInterval = 1f;
    [SerializeField] private Key pttKey = Key.V;

    private readonly List<Recorder> recorders = new List<Recorder>();
    private readonly List<Speaker> speakers = new List<Speaker>();
    private float nextRefreshTime;
    private bool loadedFromSave;
    private bool lastPttPressed;

    private const int VoiceModePtt = 0;
    private const int VoiceModeVad = 1;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureLoadedSettings();
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshTargets();
        ApplyFromCurrentSettings();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        EnsureLoadedSettings();

        if (Time.unscaledTime >= nextRefreshTime)
        {
            nextRefreshTime = Time.unscaledTime + Mathf.Max(0.2f, refreshInterval);
            RefreshTargets();
            ApplyStaticSettings();
        }

        HandlePushToTalk();
    }

    public void ApplyFromCurrentSettings()
    {
        ResolveReferences();
        EnsureLoadedSettings();
        RefreshTargets();
        ApplyStaticSettings();
        HandlePushToTalk(forceApply: true);
    }

    private void EnsureLoadedSettings()
    {
        if (loadedFromSave)
        {
            return;
        }

        if (saveLoadSystem != null)
        {
            saveLoadSystem.LoadSaveDataFromDisk();
        }

        if (currentSettings != null && saveLoadSystem != null)
        {
            currentSettings.LoadSavedSettings(saveLoadSystem.SaveData);
        }

        loadedFromSave = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshTargets();
        ApplyStaticSettings();
        HandlePushToTalk(forceApply: true);
    }

    private void RefreshTargets()
    {
        recorders.Clear();
        speakers.Clear();

        Recorder[] foundRecorders = FindObjectsByType<Recorder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < foundRecorders.Length; i++)
        {
            Recorder recorder = foundRecorders[i];
            if (recorder != null)
            {
                recorders.Add(recorder);
            }
        }

        Speaker[] foundSpeakers = FindObjectsByType<Speaker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < foundSpeakers.Length; i++)
        {
            Speaker speaker = foundSpeakers[i];
            if (speaker != null)
            {
                speakers.Add(speaker);
            }
        }
    }

    private void ApplyStaticSettings()
    {
        if (currentSettings == null)
        {
            return;
        }

        int modeIndex = Mathf.Clamp(currentSettings.VoiceModeIndex, 0, 1);
        bool useVad = modeIndex == VoiceModeVad;
        DeviceInfo microphoneDevice = GetConfiguredMicrophoneDevice();

        for (int i = 0; i < recorders.Count; i++)
        {
            Recorder recorder = recorders[i];
            if (recorder == null)
            {
                continue;
            }

            recorder.VoiceDetection = useVad;
            recorder.MicrophoneDevice = microphoneDevice;

            if (useVad)
            {
                recorder.TransmitEnabled = true;
            }
        }

        float voiceVolume = Mathf.Clamp01(currentSettings.VoiceVolume);
        for (int i = 0; i < speakers.Count; i++)
        {
            Speaker speaker = speakers[i];
            if (speaker == null)
            {
                continue;
            }

            AudioSource source = speaker.GetComponent<AudioSource>();
            if (source == null)
            {
                source = speaker.gameObject.AddComponent<AudioSource>();
            }

            source.volume = voiceVolume;
        }
    }

    private void HandlePushToTalk(bool forceApply = false)
    {
        if (currentSettings == null)
        {
            return;
        }

        int modeIndex = Mathf.Clamp(currentSettings.VoiceModeIndex, 0, 1);
        if (modeIndex != VoiceModePtt)
        {
            if (forceApply)
            {
                for (int i = 0; i < recorders.Count; i++)
                {
                    Recorder recorder = recorders[i];
                    if (recorder != null)
                    {
                        recorder.TransmitEnabled = true;
                    }
                }
            }
            return;
        }

        bool isPressed = false;
        if (Keyboard.current != null)
        {
            var control = Keyboard.current[pttKey];
            isPressed = control != null && control.isPressed;
        }

        if (forceApply == false && isPressed == lastPttPressed)
        {
            return;
        }

        lastPttPressed = isPressed;

        for (int i = 0; i < recorders.Count; i++)
        {
            Recorder recorder = recorders[i];
            if (recorder != null)
            {
                recorder.TransmitEnabled = isPressed;
            }
        }
    }

    private DeviceInfo GetConfiguredMicrophoneDevice()
    {
        if (currentSettings == null || string.IsNullOrWhiteSpace(currentSettings.VoiceInputDeviceName))
        {
            return DeviceInfo.Default;
        }

        return new DeviceInfo(currentSettings.VoiceInputDeviceName);
    }

    private void ResolveReferences()
    {
        if (currentSettings == null)
        {
            currentSettings = FindAssetByName<SettingsSO>("SettingsSO");
        }

        if (saveLoadSystem == null)
        {
            saveLoadSystem = FindAssetByName<SaveLoadSystem>("SaveLoadSystem");
        }
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
}
