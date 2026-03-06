using UnityEngine;

public class UIPauseManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private UIPause pausePanel;
    [SerializeField] private UISettingManger settingManager;

    [Header("SaveLoadSystem")]
    [SerializeField] private SaveLoadSystem saveLoadSystem;

    public bool IsOpen => pausePanel != null && pausePanel.gameObject.activeSelf;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        if (pausePanel != null)
        {
            pausePanel.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        UnbindPauseEvents();
    }

    public void HandlePauseInput()
    {
        if (IsOpen == false)
        {
            Open();
            return;
        }

        Close();
    }

    public void Toggle()
    {
        if (IsOpen)
        {
            Close();
            return;
        }

        Open();
    }

    public void Open()
    {
        if (pausePanel == null)
        {
            Debug.LogWarning("[UIPauseManager] Pause panel is missing.");
            return;
        }

        if (IsOpen)
        {
            return;
        }

        BindPauseEvents();
        pausePanel.gameObject.SetActive(true);
        SetPauseState(true);
    }

    public void Close()
    {
        if (pausePanel == null)
        {
            return;
        }

        if (IsOpen == false)
        {
            UnbindPauseEvents();
            return;
        }

        UnbindPauseEvents();
        pausePanel.gameObject.SetActive(false);
        SetPauseState(false);
    }

    private void OnBackRequested()
    {
        Close();
    }

    private void OnSettingsRequested()
    {
        if (settingManager == null)
        {
            Debug.LogWarning("[UIPauseManager] Setting manager is missing.");
            return;
        }

        UnbindPauseEvents();
        pausePanel.gameObject.SetActive(false);
        settingManager.OpenFromPause(ReopenPausePanel);
    }

    private void OnExitRequested()
    {
        if (saveLoadSystem != null)
        {
            saveLoadSystem.SaveDataToDisk();
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ReopenPausePanel()
    {
        if (pausePanel == null)
        {
            return;
        }

        pausePanel.gameObject.SetActive(true);
        BindPauseEvents();
    }

    private void SetPauseState(bool isPause)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (isPause)
        {
            GameManager.Instance.UpdateGameState(GameState.Pause);
            return;
        }

        if (GameManager.Instance.CurrentGameState == GameState.Pause)
        {
            GameManager.Instance.UpdateGameState(GameState.Gameplay);
        }
    }

    private void ResolveReferences()
    {
        if (pausePanel == null)
        {
            pausePanel = FindObjectByName<UIPause>("PausePanel");
        }

        if (pausePanel == null)
        {
            pausePanel = FindFirstObjectByType<UIPause>(FindObjectsInactive.Include);
        }

        if (settingManager == null)
        {
            settingManager = FindFirstObjectByType<UISettingManger>(FindObjectsInactive.Include);
        }
    }

    private void BindPauseEvents()
    {
        UnbindPauseEvents();

        pausePanel.BackEvent += OnBackRequested;
        pausePanel.SettingsEvent += OnSettingsRequested;
        pausePanel.ExitEvent += OnExitRequested;
    }

    private void UnbindPauseEvents()
    {
        if (pausePanel == null)
        {
            return;
        }

        pausePanel.BackEvent -= OnBackRequested;
        pausePanel.SettingsEvent -= OnSettingsRequested;
        pausePanel.ExitEvent -= OnExitRequested;
    }

    private T FindObjectByName<T>(string objectName) where T : Component
    {
        T[] objects = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < objects.Length; i++)
        {
            T candidate = objects[i];
            if (candidate != null && candidate.gameObject.name == objectName)
            {
                return candidate;
            }
        }

        return null;
    }
}

