using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private SaveLoadSystem saveLoadSystem;
    
    [Header("UI Components")]
    [SerializeField] private UIPause pausePanel;
    [SerializeField] private UISettingsController settingsPanel;
    [SerializeField] private UIPopup popupPanel;

    private void OnEnable()
    {
        inputReader.PauseEvent += TogglePausePanel;
    }

    private void OnDisable()
    {
        inputReader.PauseEvent -= TogglePausePanel;
    }

    private void Start()
    {
        pausePanel.gameObject.SetActive(false);
        settingsPanel.gameObject.SetActive(false);
        popupPanel.gameObject.SetActive(false);
    }

    /// <summary>
    /// 만약 Pause Panel이 활성화돼있다면 비활성화,
    /// 그렇지 않으면 활성화 한다.
    /// </summary>
    private void TogglePausePanel()
    {
        if (!pausePanel.gameObject.activeSelf)
        {
            pausePanel.gameObject.SetActive(true);
            pausePanel.ResumeEvent       += Resume;
            pausePanel.RestartEvent      += Restart;
            pausePanel.SettingsEvent     += ShowSettingsPanel;
            pausePanel.LoadMainMenuEvent += LoadMainMenu;
            inputReader.CancelEvent += Resume;
            
            GameManager.Instance.UpdateGameState(GameState.Pause);
        }
        else
        {
            pausePanel.gameObject.SetActive(false);
            pausePanel.ResumeEvent       -= Resume;
            pausePanel.RestartEvent      -= Restart;
            pausePanel.SettingsEvent     -= ShowSettingsPanel;
            pausePanel.LoadMainMenuEvent -= LoadMainMenu;
            
            GameManager.Instance.UpdateGameState(GameState.Gameplay);
        }
    }

    /// <summary>
    /// 게임 이어하기 로직
    /// </summary>
    private void Resume()
    {
        pausePanel.gameObject.SetActive(false);
        pausePanel.ResumeEvent -= Resume;
        pausePanel.LoadMainMenuEvent -= LoadMainMenu;
        inputReader.CancelEvent -= Resume;
        
        GameManager.Instance.UpdateGameState(GameState.Gameplay);
    }

    /// <summary>
    /// 게임 재시작(씬 재로딩)
    /// </summary>
    private void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ShowSettingsPanel()
    {
        inputReader.CancelEvent += HideSettingsPanel;
        settingsPanel.CloseButtonAction += HideSettingsPanel;
        settingsPanel.gameObject.SetActive(true);
    }

    private void HideSettingsPanel()
    {
        inputReader.CancelEvent -= HideSettingsPanel;
        settingsPanel.CloseButtonAction -= HideSettingsPanel;
        settingsPanel.gameObject.SetActive(false);
    }

    /// <summary>
    /// 메인 메뉴로 이동하기 로직
    /// </summary>
    private void LoadMainMenu()
    {
        saveLoadSystem.SaveDataToDisk();
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// 확인 버튼만 존재하는 Information 팝업창
    /// </summary>
    private void ShowPopup(string content)
    {
        popupPanel.ConfirmationResponseAction += ExitGame;
        popupPanel.ClosePopupAction           += HidePopup;
        popupPanel.gameObject.SetActive(true);
        popupPanel.SetPopup(content);
    }

    private void ShowPopup(PopupType type)
    {
        popupPanel.ConfirmationResponseAction += ExitGame;
        popupPanel.ClosePopupAction           += HidePopup;
        popupPanel.gameObject.SetActive(true);
        popupPanel.SetPopup(type);
    }

    private void BackToMainMenu(bool state)
    {
        if (state)
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    private void HidePopup()
    {
        popupPanel.ConfirmationResponseAction -= ExitGame;
        popupPanel.ClosePopupAction           -= HidePopup;
        popupPanel.gameObject.SetActive(false);
    }

    private void ExitGame(bool state)
    {
        HidePopup();
        Application.Quit();
    }
}
