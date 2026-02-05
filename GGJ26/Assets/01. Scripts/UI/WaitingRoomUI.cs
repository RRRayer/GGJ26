using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class WaitingRoomUI : MonoBehaviour
{
    [SerializeField] private WaitingRoomState waitingRoomState;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonLabel;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI startButtonLabel;

    private bool lastIsHost;

    private void Awake()
    {
        if (waitingRoomState == null)
        {
            waitingRoomState = FindFirstObjectByType<WaitingRoomState>();
        }

        TryBindSceneWidgets();
        BuildUiIfNeeded();
    }

    private void OnEnable()
    {
        if (waitingRoomState != null)
        {
            waitingRoomState.ReadyStateChanged += OnReadyStateChanged;
        }

        if (readyButton != null)
        {
            readyButton.onClick.AddListener(OnReadyClicked);
        }

        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartClicked);
        }
    }

    private void OnDisable()
    {
        if (waitingRoomState != null)
        {
            waitingRoomState.ReadyStateChanged -= OnReadyStateChanged;
        }

        if (readyButton != null)
        {
            readyButton.onClick.RemoveListener(OnReadyClicked);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartClicked);
        }
    }

    private void Start()
    {
        RefreshButtons();
        OnReadyStateChanged(0, 0, false);
    }

    private void Update()
    {
        bool isHost = waitingRoomState != null && waitingRoomState.IsHost;
        if (isHost != lastIsHost)
        {
            lastIsHost = isHost;
            RefreshButtons();
        }

        if (waitingRoomState != null && startButton != null)
        {
            startButton.interactable = waitingRoomState.IsHost && waitingRoomState.CanStartGame();
        }
    }

    private void RefreshButtons()
    {
        bool isHost = waitingRoomState != null && waitingRoomState.IsHost;
        if (readyButton != null)
        {
            readyButton.gameObject.SetActive(!isHost);
        }

        if (startButton != null)
        {
            startButton.gameObject.SetActive(isHost);
            startButton.interactable = false;
        }
    }

    private void OnReadyClicked()
    {
        waitingRoomState?.ToggleLocalReady();
    }

    private void OnStartClicked()
    {
        waitingRoomState?.RequestStartGame();
    }

    private void OnReadyStateChanged(int readyCount, int playerCount, bool allReady)
    {
        if (statusText != null)
        {
            statusText.text = $"Ready {readyCount}/{playerCount}";
        }

        RefreshButtons();

        if (startButton != null)
        {
            startButton.interactable = waitingRoomState != null && waitingRoomState.IsHost && (allReady || playerCount == 1);
        }
    }

    private void BuildUiIfNeeded()
    {
        EnsureEventSystem();

        if (statusText != null && readyButton != null && startButton != null)
        {
            return;
        }

        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            canvas = CreateCanvas("WaitingRoomCanvas");
        }

        var panel = CreatePanel(canvas.transform, "WaitingRoomPanel", new Vector2(520f, 260f));
        statusText = CreateText(panel.transform, "StatusText", "Ready 0/0", 28, new Vector2(0.5f, 0.75f));

        readyButton = CreateButton(panel.transform, "ReadyButton", "Ready", new Vector2(0.5f, 0.35f));
        readyButtonLabel = readyButton.GetComponentInChildren<TextMeshProUGUI>();

        startButton = CreateButton(panel.transform, "StartButton", "Start Game", new Vector2(0.5f, 0.35f));
        startButtonLabel = startButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void TryBindSceneWidgets()
    {
        if (statusText == null)
        {
            var found = GameObject.Find("StatusText");
            if (found != null)
            {
                statusText = found.GetComponent<TextMeshProUGUI>();
            }
        }

        if (readyButton == null)
        {
            var found = GameObject.Find("ReadyButton");
            if (found != null)
            {
                readyButton = found.GetComponent<Button>();
                readyButtonLabel = found.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        if (startButton == null)
        {
            var found = GameObject.Find("StartButton");
            if (found != null)
            {
                startButton = found.GetComponent<Button>();
                startButtonLabel = found.GetComponentInChildren<TextMeshProUGUI>();
            }
        }
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

    private TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize, Vector2 anchor)
    {
        var textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        var rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = new Vector2(420f, 60f);
        rect.anchoredPosition = Vector2.zero;

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return tmp;
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 anchor)
    {
        var buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        var rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = new Vector2(220f, 60f);
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
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;

        return button;
    }
}
