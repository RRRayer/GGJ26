using UnityEngine;
using UnityEngine.SceneManagement;

public class UIMenuManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private UIGenericButton startGameButton;
    [SerializeField] private UIGenericButton skinChangeButton;
    [SerializeField] private UIGenericButton settingsButton;
    [SerializeField] private UIGenericButton exitButton;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private UISkinSelectController skinSelectPanel;

    [Header("Start Flow")]
    [SerializeField] private bool startBySceneLoad = true;
    [SerializeField] private string startSceneName = "Lobby";

    [Header("Listening to")]
    [SerializeField] private GameStateEventChannelSO onGameStateChanged;

    private void OnEnable()
    {
        if (startGameButton != null) startGameButton.Clicked += StartGame;
        if (skinChangeButton != null) skinChangeButton.Clicked += OpenSkinSelect;
        if (settingsButton != null) settingsButton.Clicked += OpenSettings;
        if (exitButton != null) exitButton.Clicked += ExitGame;

    }

    private void OnDisable()
    {
        if (startGameButton != null) startGameButton.Clicked -= StartGame;
        if (skinChangeButton != null) skinChangeButton.Clicked -= OpenSkinSelect;
        if (settingsButton != null) settingsButton.Clicked -= OpenSettings;
        if (exitButton != null) exitButton.Clicked -= ExitGame;

    }

    private void StartGame()
    {
        if (startBySceneLoad)
        {
            if (string.IsNullOrWhiteSpace(startSceneName) == false)
            {
                SceneManager.LoadScene(startSceneName);
            }
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateGameState(GameState.Gameplay);
        }
    }

    private void OpenSkinSelect()
    {
        if (skinSelectPanel != null)
        {
            skinSelectPanel.Open();
        }
    }

    private void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    private void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
