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
    [SerializeField] private UIGenericButton exitButton;
    [SerializeField] private bool allowKeyboardStart = true;
    [SerializeField] private Key startKey = Key.Enter;
    [SerializeField] private string matchmakingMessage = "Matching...";

    [Header("Nickname UI")]
    [SerializeField] private GameObject nicknamePanel;
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private Button nicknameConfirmButton;

    [Header("Room UI")]
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_InputField roomPasswordInput;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;

    [Header("Popup (optional)")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TextMeshProUGUI popupText;
    [SerializeField] private Button cancelButton;

    private const string NicknameKey = "GGJ26.Nickname";

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

        if (IsWaitingRoomScene())
        {
            HideLobbyUi();
            HideLegacyLobbyInputs();
            enabled = false;
            return;
        }

        BuildUiIfNeeded();

        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }

        LoadNickname();
        ShowNicknamePanel(false);
        ShowRoomPanel(false);
    }

    private void OnEnable()
    {
        if (startButton != null)
        {
            startButton.Clicked += StartMatchmakingFlow;
        }

        if (nicknameConfirmButton != null)
        {
            nicknameConfirmButton.onClick.AddListener(ConfirmNickname);
        }

        if (createRoomButton != null)
        {
            createRoomButton.onClick.AddListener(CreateRoom);
        }

        if (joinRoomButton != null)
        {
            joinRoomButton.onClick.AddListener(JoinRoom);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(CancelMatchmaking);
            cancelButton.onClick.AddListener(ClosePopup);
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
            startButton.Clicked -= StartMatchmakingFlow;
        }

        if (nicknameConfirmButton != null)
        {
            nicknameConfirmButton.onClick.RemoveListener(ConfirmNickname);
        }

        if (createRoomButton != null)
        {
            createRoomButton.onClick.RemoveListener(CreateRoom);
        }

        if (joinRoomButton != null)
        {
            joinRoomButton.onClick.RemoveListener(JoinRoom);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(CancelMatchmaking);
            cancelButton.onClick.RemoveListener(ClosePopup);
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
            StartMatchmakingFlow();
        }
    }

    public void onBtnExit()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private void StartMatchmakingFlow()
    {
        ShowRoomPanel(false);
        ShowNicknamePanel(true);
        FocusNicknameInput();
    }

    private void ConfirmNickname()
    {
        if (nicknameInput == null)
        {
            return;
        }

        string nickname = nicknameInput.text.Trim();
        if (string.IsNullOrEmpty(nickname))
        {
            ShowPopup("Invalid nickname.");
            return;
        }

        PlayerPrefs.SetString(NicknameKey, nickname);
        PlayerPrefs.Save();

        ShowNicknamePanel(false);
        ShowRoomPanel(true);
        FocusRoomInput();
    }

    private void CreateRoom()
    {
        StartMatchmakingWithInputs();
    }

    private void JoinRoom()
    {
        StartMatchmakingWithInputs();
    }

    private void StartMatchmakingWithInputs()
    {
        if (launcher == null)
        {
            return;
        }

        string room = roomNameInput != null ? roomNameInput.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(room))
        {
            ShowPopup("Please enter a room name.");
            return;
        }

        string password = roomPasswordInput != null ? roomPasswordInput.text.Trim() : string.Empty;
        string sessionName = BuildSessionName(room, password);

        launcher.StartMatchmaking(sessionName, 4);

        if (popupRoot != null)
        {
            popupRoot.SetActive(true);
            if (popupText != null)
            {
                popupText.text = matchmakingMessage;
            }
        }
    }

    private string BuildSessionName(string room, string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return room;
        }

        return $"{room}#{password}";
    }

    private void CancelMatchmaking()
    {
        if (launcher == null)
        {
            return;
        }

        launcher.CancelMatchmaking();
    }

    private void ClosePopup()
    {
        if (popupRoot == null)
        {
            return;
        }

        popupRoot.SetActive(false);
    }

    private void OnMatchmakingStateChanged(bool isMatchmaking)
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(isMatchmaking);
        }
    }

    private void LoadNickname()
    {
        if (nicknameInput == null)
        {
            return;
        }

        if (PlayerPrefs.HasKey(NicknameKey))
        {
            nicknameInput.text = PlayerPrefs.GetString(NicknameKey, string.Empty);
        }
    }

    private void ShowNicknamePanel(bool visible)
    {
        if (nicknamePanel != null)
        {
            nicknamePanel.SetActive(visible);
        }
    }

    private void ShowRoomPanel(bool visible)
    {
        if (roomPanel != null)
        {
            roomPanel.SetActive(visible);
        }
    }

    private void FocusNicknameInput()
    {
        if (nicknameInput == null)
        {
            return;
        }

        nicknameInput.Select();
        nicknameInput.ActivateInputField();
    }

    private void FocusRoomInput()
    {
        if (roomNameInput == null)
        {
            return;
        }

        roomNameInput.Select();
        roomNameInput.ActivateInputField();
    }

    private void ShowPopup(string message)
    {
        if (popupRoot == null)
        {
            return;
        }

        popupRoot.SetActive(true);
        if (popupText != null)
        {
            popupText.text = message;
        }
    }

    private bool IsWaitingRoomScene()
    {
        var scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        return scenePath.EndsWith("WaitingRoom.unity", System.StringComparison.OrdinalIgnoreCase);
    }

    private void HideLobbyUi()
    {
        if (startButton != null)
        {
            startButton.gameObject.SetActive(false);
        }

        if (exitButton != null)
        {
            exitButton.gameObject.SetActive(false);
        }

        if (roomNameInput != null)
        {
            roomNameInput.gameObject.SetActive(false);
        }

        if (roomPasswordInput != null)
        {
            roomPasswordInput.gameObject.SetActive(false);
        }

        if (createRoomButton != null)
        {
            createRoomButton.gameObject.SetActive(false);
        }

        if (joinRoomButton != null)
        {
            joinRoomButton.gameObject.SetActive(false);
        }

        if (nicknamePanel != null)
        {
            nicknamePanel.SetActive(false);
        }

        if (roomPanel != null)
        {
            roomPanel.SetActive(false);
        }

        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }

    private void HideLegacyLobbyInputs()
    {
        var inputs = FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var input in inputs)
        {
            if (input == null)
            {
                continue;
            }

            input.gameObject.SetActive(false);
        }

        var dropdowns = FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var dropdown in dropdowns)
        {
            if (dropdown == null)
            {
                continue;
            }

            dropdown.gameObject.SetActive(false);
        }
    }

    private void BuildUiIfNeeded()
    {
        EnsureEventSystem();

        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            canvas = CreateCanvas("LobbyUI");
        }

        if (nicknamePanel == null)
        {
            nicknamePanel = CreatePanel(canvas.transform, "NicknamePanel", new Vector2(520f, 260f));
            CreateText(nicknamePanel.transform, "NicknameTitle", "Enter Nickname", 30, new Vector2(0.5f, 0.8f));
            nicknameInput = CreateInputField(nicknamePanel.transform, "NicknameInput", "Nickname", new Vector2(0.5f, 0.55f));
            nicknameConfirmButton = CreateButton(nicknamePanel.transform, "NicknameConfirm", "Confirm", new Vector2(0.5f, 0.25f));
        }

        if (roomPanel == null)
        {
            roomPanel = CreatePanel(canvas.transform, "RoomPanel", new Vector2(560f, 360f));
            CreateText(roomPanel.transform, "RoomTitle", "Create Room / Join Room", 30, new Vector2(0.5f, 0.85f));
            roomNameInput = CreateInputField(roomPanel.transform, "RoomNameInput", "Room Name", new Vector2(0.5f, 0.62f));
            roomPasswordInput = CreateInputField(roomPanel.transform, "RoomPasswordInput", "Password (Optional)", new Vector2(0.5f, 0.45f));
            createRoomButton = CreateButton(roomPanel.transform, "CreateRoomButton", "Create Room", new Vector2(0.32f, 0.22f));
            joinRoomButton = CreateButton(roomPanel.transform, "JoinRoomButton", "Join Room", new Vector2(0.68f, 0.22f));
        }

        if (popupRoot == null)
        {
            popupRoot = CreatePanel(canvas.transform, "MatchmakingPopup", new Vector2(480f, 220f));
            popupText = CreateText(popupRoot.transform, "PopupText", matchmakingMessage, 26, new Vector2(0.5f, 0.65f));
            cancelButton = CreateButton(popupRoot.transform, "PopupCancel", "Cancel", new Vector2(0.5f, 0.3f));
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

    private TMP_InputField CreateInputField(Transform parent, string name, string placeholder, Vector2 anchor)
    {
        var rootObj = new GameObject(name);
        rootObj.transform.SetParent(parent, false);

        var rect = rootObj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = new Vector2(420f, 46f);
        rect.anchoredPosition = Vector2.zero;

        var image = rootObj.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.9f);

        var input = rootObj.AddComponent<TMP_InputField>();

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(rootObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 6f);
        textRect.offsetMax = new Vector2(-12f, -6f);

        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 24;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Left;

        var placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(rootObj.transform, false);
        var placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(12f, 6f);
        placeholderRect.offsetMax = new Vector2(-12f, -6f);

        var placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderText.fontSize = 24;
        placeholderText.text = placeholder;
        placeholderText.color = new Color(0f, 0f, 0f, 0.5f);
        placeholderText.alignment = TextAlignmentOptions.Left;

        input.textComponent = text;
        input.placeholder = placeholderText;

        return input;
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 anchor)
    {
        var buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        var rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = new Vector2(200f, 54f);
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
