using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class UIGamePlaySetting : MonoBehaviour
{
    [Header("Dropdown Wrappers (Optional)")]
    [SerializeField] private UISettingsDropdown resolutionDropdownWrapper;
    [SerializeField] private UISettingsDropdown windowModeDropdownWrapper;

    [Header("TMP Dropdown (Fallback)")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown windowModeDropdown;

    [Header("Data")]
    [SerializeField] private SettingsSO currentSettings;
    [SerializeField] private SaveLoadSystem saveLoadSystem;

    private readonly List<Resolution> resolutionList = new List<Resolution>();
    private bool suppressDropdownEvent;
    private int currentResolutionIndex;
    private int currentWindowModeIndex;

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
        BuildResolutionList();

        if (resolutionList.Count == 0)
        {
            Debug.LogWarning("[UIGamePlaySetting] No available resolutions.");
            return;
        }

        currentResolutionIndex = GetSafeResolutionIndex(currentSettings != null ? currentSettings.ResolutionIndex : 0);
        currentWindowModeIndex = GetSafeWindowModeIndex(currentSettings != null ? currentSettings.WindowModeIndex : 0);

        ApplyDropdownOptions();
        ApplyCurrentValueToDropdowns();
        ApplyScreenSettings();

        if (currentSettings != null)
        {
            currentSettings.SaveGraphicsSettings(currentResolutionIndex, currentWindowModeIndex);
        }
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

    private void BuildResolutionList()
    {
        resolutionList.Clear();

        Resolution[] allResolutions = Screen.resolutions;
        HashSet<string> keys = new HashSet<string>();

        for (int i = 0; i < allResolutions.Length; i++)
        {
            Resolution resolution = allResolutions[i];
            int hz = GetRefreshRate(resolution);
            if (resolution.width < 1920 || hz < 60)
            {
                continue;
            }

            string key = $"{resolution.width}x{resolution.height}@{hz}";
            if (keys.Add(key))
            {
                resolutionList.Add(resolution);
            }
        }

        if (resolutionList.Count == 0)
        {
            for (int i = 0; i < allResolutions.Length; i++)
            {
                Resolution resolution = allResolutions[i];
                int hz = GetRefreshRate(resolution);
                string key = $"{resolution.width}x{resolution.height}@{hz}";
                if (keys.Add(key))
                {
                    resolutionList.Add(resolution);
                }
            }
        }

        resolutionList.Sort((a, b) =>
        {
            int widthCompare = b.width.CompareTo(a.width);
            if (widthCompare != 0) return widthCompare;

            int heightCompare = b.height.CompareTo(a.height);
            if (heightCompare != 0) return heightCompare;

            return GetRefreshRate(b).CompareTo(GetRefreshRate(a));
        });
    }

    private void ApplyDropdownOptions()
    {
        List<string> resolutionOptions = new List<string>();
        for (int i = 0; i < resolutionList.Count; i++)
        {
            Resolution resolution = resolutionList[i];
            resolutionOptions.Add($"{resolution.width} x {resolution.height} @{GetRefreshRate(resolution)}Hz");
        }

        SetOptions(resolutionDropdownWrapper, resolutionDropdown, resolutionOptions);

        List<string> windowOptions = new List<string>
        {
            "Fullscreen",
            "Windowed",
            "Borderless"
        };
        SetOptions(windowModeDropdownWrapper, windowModeDropdown, windowOptions);
    }

    private void ApplyCurrentValueToDropdowns()
    {
        suppressDropdownEvent = true;

        SetValue(resolutionDropdownWrapper, resolutionDropdown, currentResolutionIndex);
        SetValue(windowModeDropdownWrapper, windowModeDropdown, currentWindowModeIndex);

        Refresh(resolutionDropdownWrapper, resolutionDropdown);
        Refresh(windowModeDropdownWrapper, windowModeDropdown);

        suppressDropdownEvent = false;
    }

    private void BindEvents()
    {
        UnbindEvents();

        Bind(resolutionDropdownWrapper, resolutionDropdown, OnResolutionChanged);
        Bind(windowModeDropdownWrapper, windowModeDropdown, OnWindowModeChanged);
    }

    private void UnbindEvents()
    {
        Unbind(resolutionDropdownWrapper, resolutionDropdown, OnResolutionChanged);
        Unbind(windowModeDropdownWrapper, windowModeDropdown, OnWindowModeChanged);
    }

    private void OnResolutionChanged(int index)
    {
        if (suppressDropdownEvent)
        {
            return;
        }

        currentResolutionIndex = GetSafeResolutionIndex(index);
        ApplyScreenSettings();
        SaveCurrentSettings();
    }

    private void OnWindowModeChanged(int index)
    {
        if (suppressDropdownEvent)
        {
            return;
        }

        currentWindowModeIndex = GetSafeWindowModeIndex(index);
        ApplyScreenSettings();
        SaveCurrentSettings();
    }

    private void ApplyScreenSettings()
    {
        if (resolutionList.Count == 0)
        {
            return;
        }

        Resolution resolution = resolutionList[currentResolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, ToFullScreenMode(currentWindowModeIndex));
    }

    private void SaveCurrentSettings()
    {
        if (currentSettings != null)
        {
            currentSettings.SaveGraphicsSettings(currentResolutionIndex, currentWindowModeIndex);
        }

        if (saveLoadSystem != null)
        {
            saveLoadSystem.SaveDataToDisk();
        }
    }

    private void ResolveReferences()
    {
        ResolveDropdownRefs(ref resolutionDropdownWrapper, ref resolutionDropdown, "SRDropDown");
        ResolveDropdownRefs(ref windowModeDropdownWrapper, ref windowModeDropdown, "WindowDropDown ", "WindowDropDown");

        if (currentSettings == null)
        {
            currentSettings = FindAssetByName<SettingsSO>("SettingsSO");
        }

        if (saveLoadSystem == null)
        {
            saveLoadSystem = FindAssetByName<SaveLoadSystem>("SaveLoadSystem");
        }
    }

    private void ResolveDropdownRefs(
        ref UISettingsDropdown wrapper,
        ref TMP_Dropdown dropdown,
        params string[] names)
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

    private static int GetRefreshRate(Resolution resolution)
    {
        return Mathf.RoundToInt((float)resolution.refreshRateRatio.value);
    }

    private static FullScreenMode ToFullScreenMode(int modeIndex)
    {
        switch (modeIndex)
        {
            case 0: return FullScreenMode.ExclusiveFullScreen;
            case 1: return FullScreenMode.Windowed;
            default: return FullScreenMode.FullScreenWindow;
        }
    }

    private int GetSafeResolutionIndex(int index)
    {
        if (resolutionList.Count == 0)
        {
            return 0;
        }

        return Mathf.Clamp(index, 0, resolutionList.Count - 1);
    }

    private static int GetSafeWindowModeIndex(int index)
    {
        return Mathf.Clamp(index, 0, 2);
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
            dropdown.SetValueWithoutNotify(value);
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

    private static void Bind(UISettingsDropdown wrapper, TMP_Dropdown dropdown, UnityAction<int> callback)
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

    private static void Unbind(UISettingsDropdown wrapper, TMP_Dropdown dropdown, UnityAction<int> callback)
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
}
