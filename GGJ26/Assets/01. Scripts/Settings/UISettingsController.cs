using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UISettingsController : MonoBehaviour
{
    [Header("Tab Buttons")]
    [SerializeField] private Button gameplayButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private Button closeButton;

    [Header("Tab Panels")]
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject soundPanel;

    public event UnityAction CloseButtonAction;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        BindEvents();
        ShowGameplayPanel();
    }

    private void OnDisable()
    {
        UnbindEvents();
    }

    private void BindEvents()
    {
        UnbindEvents();

        if (gameplayButton != null)
        {
            gameplayButton.onClick.AddListener(ShowGameplayPanel);
        }

        if (soundButton != null)
        {
            soundButton.onClick.AddListener(ShowSoundPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseRequested);
        }
    }

    private void UnbindEvents()
    {
        if (gameplayButton != null)
        {
            gameplayButton.onClick.RemoveListener(ShowGameplayPanel);
        }

        if (soundButton != null)
        {
            soundButton.onClick.RemoveListener(ShowSoundPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseRequested);
        }
    }

    private void ShowGameplayPanel()
    {
        SetPanelState(true);
    }

    private void ShowSoundPanel()
    {
        SetPanelState(false);
    }

    private void SetPanelState(bool showGameplayPanel)
    {
        if (gameplayPanel != null)
        {
            gameplayPanel.SetActive(showGameplayPanel);
        }

        if (soundPanel != null)
        {
            soundPanel.SetActive(showGameplayPanel == false);
        }
    }

    private void OnCloseRequested()
    {
        CloseButtonAction?.Invoke();
    }

    private void ResolveReferences()
    {
        if (gameplayButton == null)
        {
            gameplayButton = FindButtonByPath("BtnContainer/Button");
        }

        if (soundButton == null)
        {
            soundButton = FindButtonByPath("BtnContainer/Button (1)");
        }

        if (gameplayPanel == null)
        {
            gameplayPanel = FindByName("GamePlayContainer");
        }

        if (soundPanel == null)
        {
            soundPanel = FindByName("SoundContainer");
        }
    }

    private Button FindButtonByPath(string path)
    {
        Transform target = transform.Find(path);
        if (target == null)
        {
            return null;
        }

        return target.GetComponent<Button>();
    }

    private GameObject FindByName(string objectName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform candidate = children[i];
            if (candidate != null && candidate.name == objectName)
            {
                return candidate.gameObject;
            }
        }

        return null;
    }
}
