using System;
using UnityEngine;
using UnityEngine.Events;

public class UIPause : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private UIGenericButton resumeButton;
    [SerializeField] private UIGenericButton restartButton;
    [SerializeField] private UIGenericButton settingsButton;
    [SerializeField] private UIGenericButton loadMainMenuButton;

    [Header("SaveLoadSystem")]
    [SerializeField] private SaveLoadSystem saveLoadSystem;
    
    public event UnityAction ResumeEvent;
    public event UnityAction RestartEvent;
    public event UnityAction SettingsEvent;
    public event UnityAction LoadMainMenuEvent;

    private void OnEnable()
    {
        resumeButton.Clicked       += OnResumeButtonClicked;
        restartButton.Clicked      += OnRestartButtonClicked;
        settingsButton.Clicked     += OnSettingsButtonClicked;
        loadMainMenuButton.Clicked += OnLoadMainMenuButtonClicked;
    }

    private void OnDisable()
    {
        resumeButton.Clicked       -= OnResumeButtonClicked;
        restartButton.Clicked      -= OnRestartButtonClicked;
        settingsButton.Clicked     -= OnSettingsButtonClicked;
        loadMainMenuButton.Clicked -= OnLoadMainMenuButtonClicked;
    }

    /// <summary>
    /// 재시작 이벤트 실행
    /// </summary>
    private void OnResumeButtonClicked()
    {
        ResumeEvent?.Invoke();
    }

    private void OnRestartButtonClicked()
    {
        saveLoadSystem.SaveDataToDisk();
        RestartEvent?.Invoke();
    }

    private void OnSettingsButtonClicked()
    {
        SettingsEvent?.Invoke();
    }

    private void OnLoadMainMenuButtonClicked()
    {
        LoadMainMenuEvent?.Invoke();
    }
}
