using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class LobbyMatchmakingUI : MonoBehaviour
{
    [SerializeField] private FusionLauncher launcher;
    [SerializeField] private UIGenericButton startButton;
    [SerializeField] private bool allowKeyboardStart = true;
    [SerializeField] private Key startKey = Key.Enter;
    [SerializeField] private string matchmakingMessage = "매칭 중...";

    [Header("Popup (optional)")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TextMeshProUGUI popupText;
    [SerializeField] private Button cancelButton;

    private void Awake()
    {
        if (launcher == null)
        {
            launcher = FindFirstObjectByType<FusionLauncher>();
        }

        if (startButton == null)
        {
            startButton = FindFirstObjectByType<UIGenericButton>();
        }

        if (popupRoot == null)
        {
            BuildPopup();
        }

        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (startButton != null)
        {
            startButton.Clicked += StartMatchmaking;
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(CancelMatchmaking);
        }

        if (launcher != null)
        {
            launcher.MatchmakingStateChanged += OnMatchmakingStateChanged;
            OnMatchmakingStateChanged(launcher.IsMatchmaking);
        }
    }

    private void OnDisable()
    {
        if (startButton != null)
        {
            startButton.Clicked -= StartMatchmaking;
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(CancelMatchmaking);
        }

        if (launcher != null)
        {
            launcher.MatchmakingStateChanged -= OnMatchmakingStateChanged;
        }

        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }

    private void Update()
    {
        if (allowKeyboardStart && Keyboard.current != null && Keyboard.current[startKey].wasPressedThisFrame)
        {
            StartMatchmaking();
        }
    }

    private void StartMatchmaking()
    {
        if (launcher == null)
        {
            return;
        }

        if (popupRoot != null)
        {
            popupRoot.SetActive(true);
        }

        launcher.StartMatchmaking();
    }

    private void CancelMatchmaking()
    {
        if (launcher == null)
        {
            return;
        }

        launcher.CancelMatchmaking();
    }

    private void OnMatchmakingStateChanged(bool isMatchmaking)
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(isMatchmaking);
        }
    }

    private void BuildPopup()
    {
        EnsureEventSystem();

        var canvas = CreateCanvas("MatchmakingCanvas");
        popupRoot = CreatePanel(canvas.transform, "MatchmakingPopup", new Vector2(480f, 220f));
        popupText = CreateText(popupRoot.transform, "MatchmakingText", matchmakingMessage);
        cancelButton = CreateButton(popupRoot.transform, "CancelButton", "매칭 취소");
    }

    private void EnsureEventSystem()
    {
        var eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var eventSystemObj = new GameObject("EventSystem");
            eventSystem = eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<InputSystemUIInputModule>();
            DontDestroyOnLoad(eventSystemObj);
            return;
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        var legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacyModule != null)
        {
            legacyModule.enabled = false;
        }
    }

    private Canvas CreateCanvas(string name)
    {
        var canvasObj = new GameObject(name);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObj.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObj);
        return canvas;
    }

    private GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        var panelObj = new GameObject(name);
        panelObj.transform.SetParent(parent, false);

        var rect = panelObj.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        var image = panelObj.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.75f);

        return panelObj;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string text)
    {
        var textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        var rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.7f);
        rect.anchorMax = new Vector2(0.5f, 0.7f);
        rect.sizeDelta = new Vector2(420f, 80f);
        rect.anchoredPosition = Vector2.zero;

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 32;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return tmp;
    }

    private Button CreateButton(Transform parent, string name, string label)
    {
        var buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        var rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.3f);
        rect.anchorMax = new Vector2(0.5f, 0.3f);
        rect.sizeDelta = new Vector2(200f, 60f);
        rect.anchoredPosition = Vector2.zero;

        var image = buttonObj.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.9f);

        var button = buttonObj.AddComponent<Button>();

        var labelObj = new GameObject("Text");
        labelObj.transform.SetParent(buttonObj.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 26;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;

        return button;
    }
}
