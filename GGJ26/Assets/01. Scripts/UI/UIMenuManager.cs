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

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

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

    private void ResolveReferences()
    {
        if (startGameButton != null)
        {
            startGameButton = ResolveButton(startGameButton, "BtnStart");
        }

        if (skinChangeButton != null)
        {
            skinChangeButton = ResolveButton(skinChangeButton, "BtnSkinChange");
        }

        if (settingsButton != null)
        {
            settingsButton = ResolveButton(settingsButton, "BtnSettings");
        }

        if (exitButton != null)
        {
            exitButton = ResolveButton(exitButton, "BtnExit");
        }

        if (settingsPanel == null)
        {
            settingsPanel = FindGameObjectByName("MainMenuSettingsPanel");
        }

        if (skinSelectPanel == null)
        {
            skinSelectPanel = FindObjectByName<UISkinSelectController>("SkinSelectPanel");
        }
    }

    private UIGenericButton ResolveButton(UIGenericButton current, string expectedName)
    {
        if (current != null && current.gameObject.name == expectedName)
        {
            return current;
        }

        UIGenericButton[] buttons = FindObjectsByType<UIGenericButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            UIGenericButton candidate = buttons[i];
            if (candidate != null && candidate.gameObject.name == expectedName)
            {
                return candidate;
            }
        }

        return current;
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

    private GameObject FindGameObjectByName(string objectName)
    {
        Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            Transform tr = all[i];
            if (tr == null || tr.name != objectName)
            {
                continue;
            }

            if (tr.gameObject.scene.IsValid() && tr.gameObject.scene.isLoaded)
            {
                return tr.gameObject;
            }
        }

        return null;
    }
}
