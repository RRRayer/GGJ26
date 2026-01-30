using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class UISettingsGraphicsComponent : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private UISettingsDropdown resolutionDropdown;
    [SerializeField] private UISettingsDropdown fullScreenDropdown;
    
    [Header("Listening to")]
    [SerializeField] private IntEventChannelSO changeResolutionEvent;
    
    private List<Resolution> resolutionList;
    private Resolution currentResolution;
    private int currentResolutionIndex;
    private bool isFullScreen;

    private const int minResolution = 1920;
    private const int minRefreshRate = 60;

    private void OnEnable()
    {
        resolutionDropdown.ValueChanged += OnResolutionDropdownChanged;
        fullScreenDropdown.ValueChanged += OnFullScreenDropdownChanged;
        changeResolutionEvent.OnEventRaised += OnResolutionDropdownChanged;
    }

    private void OnDisable()
    {
        resolutionDropdown.ValueChanged -= OnResolutionDropdownChanged;
        fullScreenDropdown.ValueChanged -= OnFullScreenDropdownChanged;
        changeResolutionEvent.OnEventRaised -= OnResolutionDropdownChanged;
    }
    
    private void Init()
    {
        // 해상도 초기화
        resolutionList = GetResolutionsList();
        
        currentResolution = Screen.currentResolution;
        currentResolutionIndex = GetCurrentResolutionIndex();
        InitializeResolutionDropdown();
    }

    public void Setup(int currentResolutionIndex, bool isFullScreen)
    {
        this.currentResolutionIndex = currentResolutionIndex;
        this.isFullScreen = isFullScreen;
        
        Init();
    }
    
    #region RESOLUTION
    
    /// <summary>
    /// 사용자 모니터에서 설정 가능한 해상도 리스트 반환
    /// </summary>
    private List<Resolution> GetResolutionsList()
    {
        return Screen.resolutions
            .Where(resolution => resolution.width >= minResolution && Mathf.RoundToInt((float)resolution.refreshRateRatio.value) >= minRefreshRate)
            .Distinct()
            .Reverse()
            .ToList();
    }

    /// <summary>
    /// 현재 설정된 해상도의 
    /// </summary>
    /// <returns></returns>
    private int GetCurrentResolutionIndex()
    {
        if (resolutionList == null)
        {
            resolutionList = GetResolutionsList();
        }
        return resolutionList.IndexOf(currentResolution);;
    }

    /// <summary>
    /// 해상도 드롭다운 UI 초기화
    /// </summary>
    private void InitializeResolutionDropdown()
    {
        if (resolutionDropdown == null) return;
        
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        for (int i = 0; i < resolutionList.Count; ++i)
        {
            options.Add($"{resolutionList[i].width} x {resolutionList[i].height} " +
                        $"@{Mathf.FloorToInt((float)resolutionList[i].refreshRateRatio.value)}Hz");
        }
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValue(currentResolutionIndex);
        resolutionDropdown.RefreshShownValue();
    }
    
    /// <summary>
    /// Resolution 드롭다운 값이 변경됐을 때 실행
    /// </summary>
    private void OnResolutionDropdownChanged(int resolutionIndex)
    {
        if (currentResolutionIndex != resolutionIndex)
        {
            currentResolutionIndex = resolutionIndex;
            OnResolutionChanged();
        }
    }

    /// <summary>
    /// 해상도 및 화면 사이즈 변경 이벤트
    /// </summary>
    private void OnResolutionChanged()
    {
        currentResolution = resolutionList[currentResolutionIndex];
        FullScreenMode fullScreenMode = isFullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(currentResolution.width, currentResolution.height, fullScreenMode);
        StartCoroutine(VerifyResolutionChange(fullScreenMode));
    }

    /// <summary>
    /// 해상도 바뀌는 딜레이 이후 성공 여부 확인. 실패 시 기본 설정으로 롤백
    /// </summary>
    private IEnumerator VerifyResolutionChange(FullScreenMode fullScreenMode)
    {
        yield return new WaitForSeconds(5.0f);

        if (Screen.currentResolution.width == currentResolution.width &&
            Screen.currentResolution.height == currentResolution.height)
        {
            //Debug.Log("해상도 변경 성공");
        }
        else
        {
            Log.W($"해상도 변경 실패 {Screen.currentResolution.width}x{Screen.currentResolution.height}" +
                      $" to {currentResolution.width}x{currentResolution.height}");
            currentResolutionIndex = 0; 
            currentResolution = resolutionList[currentResolutionIndex];
            Screen.SetResolution(currentResolution.width, currentResolution.height, fullScreenMode);
            resolutionDropdown.SetValue(currentResolutionIndex);
        }
    }
    
    #endregion

    #region FULL SCREEN

    /// <summary>
    /// 0: FullScreen, 1: Window
    /// </summary>
    private void OnFullScreenDropdownChanged(int fullScreenIndex)
    {
        if (fullScreenIndex == 0)
            isFullScreen = true;
        else
            isFullScreen = false;
    }
    #endregion

    public void SaveGraphics(SettingsSO currentSettings)
    {
        currentSettings.SaveGraphicsSettings(currentResolutionIndex, isFullScreen);
    }

    /// <summary>
    /// Default 값으로 바꾸고 설정 적용
    /// </summary>
    public void ResetGraphics(SettingsSO currentSettings)
    {
        resolutionDropdown.SetValue(0);
        currentResolutionIndex = 0;
        OnResolutionChanged();
        
        currentSettings.SaveGraphicsSettings(currentResolutionIndex, true);
        Setup(currentResolutionIndex, true);
    }
}
