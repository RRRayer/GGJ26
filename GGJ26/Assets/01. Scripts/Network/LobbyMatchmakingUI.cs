using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Fusion;
using System.Collections.Generic;

public class LobbyMatchmakingUI : MonoBehaviour
{
    private enum RoomMode
    {
        Classic = 0,
        Nuguri = 1
    }

    [SerializeField] private FusionLauncher launcher;
    [SerializeField] private UIGenericButton startButton;
    [SerializeField] private UIGenericButton publicButton;
    [SerializeField] private UIGenericButton changeSkinButton;
    [SerializeField] private UIGenericButton exitButton;
    [SerializeField] private UISkinSelectController skinSelectPanel;
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
    [SerializeField] private Button roomCloseButton;
    [SerializeField] private Toggle privateRoomToggle;
    [SerializeField] private Button modeToggleButton;
    [SerializeField] private TextMeshProUGUI modeValueText;
    [SerializeField] private TextMeshProUGUI modeDescriptionText;
    [SerializeField] private Button maxPlayersPrevButton;
    [SerializeField] private Button maxPlayersNextButton;
    [SerializeField] private TextMeshProUGUI maxPlayersValueText;
    [SerializeField] private int minRoomPlayers = 1;
    [SerializeField] private int maxRoomPlayers = 4;
    [SerializeField] private int selectedMaxPlayers = 4;
    [SerializeField] private int roomTitleCharacterLimit = 20;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;

    [Header("Public Room List UI")]
    [SerializeField] private GameObject publicRoomPanel;
    [SerializeField] private RectTransform publicRoomListContent;
    [SerializeField] private Button publicRoomCloseButton;
    [SerializeField] private TextMeshProUGUI publicRoomEmptyText;
    [SerializeField] private Button publicRoomDirectJoinButton;
    [SerializeField] private Button publicRoomRefreshButton;

    [Header("Popup (optional)")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TextMeshProUGUI popupText;
    [SerializeField] private Button cancelButton;

    private const string NicknameKey = "GGJ26.Nickname";
    private const string PrivateRoomSeparator = "#";
    private RoomMode selectedRoomMode = RoomMode.Classic;
    private bool roomPanelCreateMode = true;

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
        ResolveMainMenuButtons();
        ConfigureRoomPanelRaycasts();
        NormalizeRoomPlayerSettings();
        ApplyRoomInputConstraints();
        RefreshMaxPlayersUi();
        RefreshRoomModeUi();
        UpdatePasswordVisibility();

        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }

        LoadNickname();
        ShowNicknamePanel(false);
        ShowRoomPanel(false);
        ShowPublicRoomPanel(false);
    }

private void OnEnable()
    {
        ResolveMainMenuButtons();

        if (startButton != null)
        {
            startButton.Clicked += StartMatchmakingFlow;
        }

        if (publicButton != null)
        {
            publicButton.Clicked += OpenPublicRoomPopup;
        }

        if (changeSkinButton != null)
        {
            changeSkinButton.Clicked += OpenSkinPopup;
        }

        if (publicRoomCloseButton != null)
        {
            publicRoomCloseButton.onClick.AddListener(ClosePublicRoomPanel);
        }

        if (publicRoomDirectJoinButton != null)
        {
            publicRoomDirectJoinButton.onClick.AddListener(OpenDirectJoinFromPublic);
        }

        if (publicRoomRefreshButton != null)
        {
            publicRoomRefreshButton.onClick.AddListener(RefreshPublicRoomList);
        }

        if (nicknameConfirmButton != null)
        {
            nicknameConfirmButton.onClick.AddListener(ConfirmNickname);
        }

        if (createRoomButton != null)
        {
            createRoomButton.onClick.AddListener(CreateRoom);
        }

        if (roomCloseButton != null)
        {
            roomCloseButton.onClick.AddListener(CloseRoomPanel);
        }

        if (privateRoomToggle != null)
        {
            privateRoomToggle.onValueChanged.AddListener(OnPrivateRoomToggleChanged);
        }

        if (modeToggleButton != null)
        {
            modeToggleButton.onClick.AddListener(ToggleRoomMode);
        }

        if (maxPlayersPrevButton != null)
        {
            maxPlayersPrevButton.onClick.AddListener(DecreaseMaxPlayers);
        }

        if (maxPlayersNextButton != null)
        {
            maxPlayersNextButton.onClick.AddListener(IncreaseMaxPlayers);
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
            launcher.SessionListUpdated += OnSessionListUpdated;
            OnMatchmakingStateChanged(launcher.IsMatchmaking);
        }
    }

private void OnDisable()
    {
        if (startButton != null)
        {
            startButton.Clicked -= StartMatchmakingFlow;
        }

        if (publicButton != null)
        {
            publicButton.Clicked -= OpenPublicRoomPopup;
        }

        if (changeSkinButton != null)
        {
            changeSkinButton.Clicked -= OpenSkinPopup;
        }

        if (publicRoomCloseButton != null)
        {
            publicRoomCloseButton.onClick.RemoveListener(ClosePublicRoomPanel);
        }

        if (publicRoomDirectJoinButton != null)
        {
            publicRoomDirectJoinButton.onClick.RemoveListener(OpenDirectJoinFromPublic);
        }

        if (publicRoomRefreshButton != null)
        {
            publicRoomRefreshButton.onClick.RemoveListener(RefreshPublicRoomList);
        }

        if (nicknameConfirmButton != null)
        {
            nicknameConfirmButton.onClick.RemoveListener(ConfirmNickname);
        }

        if (createRoomButton != null)
        {
            createRoomButton.onClick.RemoveListener(CreateRoom);
        }

        if (roomCloseButton != null)
        {
            roomCloseButton.onClick.RemoveListener(CloseRoomPanel);
        }

        if (privateRoomToggle != null)
        {
            privateRoomToggle.onValueChanged.RemoveListener(OnPrivateRoomToggleChanged);
        }

        if (modeToggleButton != null)
        {
            modeToggleButton.onClick.RemoveListener(ToggleRoomMode);
        }

        if (maxPlayersPrevButton != null)
        {
            maxPlayersPrevButton.onClick.RemoveListener(DecreaseMaxPlayers);
        }

        if (maxPlayersNextButton != null)
        {
            maxPlayersNextButton.onClick.RemoveListener(IncreaseMaxPlayers);
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
            launcher.SessionListUpdated -= OnSessionListUpdated;
        }

        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }

    private void Update()
    {
        HandleRoomInputTabNavigation();
        HandleEscapeKey();

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
        OpenHostCreateRoomPopup();
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
        UpdatePasswordVisibility();
        FocusRoomInput();
    }

private void CreateRoom()
    {
        roomPanelCreateMode = true;
        StartMatchmakingWithInputs(true);
    }

private void JoinRoom()
    {
        roomPanelCreateMode = false;
        StartMatchmakingWithInputs(false);
    }

    private void OpenHostCreateRoomPopup()
    {
        ShowNicknamePanel(false);
        ShowPublicRoomPanel(false);
        roomPanelCreateMode = true;
        ShowRoomPanel(true);
        SetRoomPanelMode(true);
        UpdatePasswordVisibility();
        FocusRoomInput();
    }

    private void OpenPublicRoomPopup()
    {
        ShowNicknamePanel(false);
        ShowRoomPanel(false);
        ShowPublicRoomPanel(true);
        RefreshPublicRoomList();
    }

    private void OpenSkinPopup()
    {
        ShowPublicRoomPanel(false);
        ShowRoomPanel(false);
        if (skinSelectPanel != null)
        {
            skinSelectPanel.Open();
        }
    }


private void StartMatchmakingWithInputs(bool isCreateRoom)
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
        bool hasPassword = isCreateRoom
            ? privateRoomToggle != null && privateRoomToggle.isOn
            : string.IsNullOrEmpty(password) == false;

        if (isCreateRoom && hasPassword && string.IsNullOrEmpty(password))
        {
            ShowPopup("Please enter a password.");
            return;
        }

        string sessionName = BuildSessionName(room, hasPassword ? password : string.Empty);

        int requestedMaxPlayers = isCreateRoom
            ? selectedMaxPlayers
            : Mathf.Max(1, launcher.MaxPlayers);

        launcher.StartMatchmaking(sessionName, requestedMaxPlayers);

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

    private void OnSessionListUpdated(IReadOnlyList<SessionInfo> sessions)
    {
        if (publicRoomPanel != null && publicRoomPanel.activeInHierarchy)
        {
            RebuildPublicRoomList(sessions);
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
            if (visible)
            {
                SetRoomPanelMode(roomPanelCreateMode);
            }
        }
    }

    private void ShowPublicRoomPanel(bool visible)
    {
        if (publicRoomPanel != null)
        {
            publicRoomPanel.SetActive(visible);
        }
    }

private void SetRoomPanelMode(bool createMode)
    {
        roomPanelCreateMode = createMode;

        if (createRoomButton != null)
        {
            createRoomButton.gameObject.SetActive(createMode);
        }

        if (joinRoomButton != null)
        {
            joinRoomButton.gameObject.SetActive(createMode == false);
        }

        if (maxPlayersPrevButton != null)
        {
            maxPlayersPrevButton.gameObject.SetActive(createMode);
        }

        if (maxPlayersNextButton != null)
        {
            maxPlayersNextButton.gameObject.SetActive(createMode);
        }

        if (maxPlayersValueText != null)
        {
            maxPlayersValueText.gameObject.SetActive(createMode);
        }

        if (modeToggleButton != null)
        {
            modeToggleButton.gameObject.SetActive(createMode);
        }

        if (modeValueText != null)
        {
            modeValueText.gameObject.SetActive(createMode);
        }

        if (modeDescriptionText != null)
        {
            modeDescriptionText.gameObject.SetActive(createMode);
        }

        if (privateRoomToggle != null)
        {
            privateRoomToggle.gameObject.SetActive(createMode);
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

    private void FocusPasswordInput()
    {
        if (roomPasswordInput == null || roomPasswordInput.gameObject.activeInHierarchy == false)
        {
            return;
        }

        roomPasswordInput.Select();
        roomPasswordInput.ActivateInputField();
    }

    private void HandleEscapeKey()
    {
        if (Keyboard.current == null || Keyboard.current.escapeKey.wasPressedThisFrame == false)
        {
            return;
        }

        if (roomPanel != null && roomPanel.activeInHierarchy)
        {
            CloseRoomPanel();
            return;
        }

        if (publicRoomPanel != null && publicRoomPanel.activeInHierarchy)
        {
            ClosePublicRoomPanel();
            return;
        }

        onBtnExit();
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

        if (publicRoomPanel != null)
        {
            publicRoomPanel.SetActive(false);
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
            modeValueText = CreateText(roomPanel.transform, "ModeValueText", "Classic", 24, new Vector2(0.5f, 0.5f));
            modeToggleButton = CreateButton(roomPanel.transform, "ModeToggleButton", "Toggle Mode", new Vector2(0.5f, 0.5f));
            modeDescriptionText = CreateText(roomPanel.transform, "ModeDescriptionText", "Classic mode", 20, new Vector2(0.75f, 0.5f));
            roomPasswordInput = CreateInputField(roomPanel.transform, "RoomPasswordInput", "Password (Optional)", new Vector2(0.5f, 0.45f));
            maxPlayersPrevButton = CreateButton(roomPanel.transform, "MaxPlayersPrevButton", "<", new Vector2(0.34f, 0.33f));
            maxPlayersNextButton = CreateButton(roomPanel.transform, "MaxPlayersNextButton", ">", new Vector2(0.66f, 0.33f));
            maxPlayersValueText = CreateText(roomPanel.transform, "MaxPlayersValueText", selectedMaxPlayers.ToString(), 28, new Vector2(0.5f, 0.33f));
            createRoomButton = CreateButton(roomPanel.transform, "CreateRoomButton", "Create Room", new Vector2(0.32f, 0.22f));
            joinRoomButton = CreateButton(roomPanel.transform, "JoinRoomButton", "Join Room", new Vector2(0.68f, 0.22f));
        }
        else
        {
            ResolveRoomUiReferencesFromChildren();
        }

        if (popupRoot == null)
        {
            popupRoot = CreatePanel(canvas.transform, "MatchmakingPopup", new Vector2(480f, 220f));
            popupText = CreateText(popupRoot.transform, "PopupText", matchmakingMessage, 26, new Vector2(0.5f, 0.65f));
            cancelButton = CreateButton(popupRoot.transform, "PopupCancel", "Cancel", new Vector2(0.5f, 0.3f));
        }

        ResolvePublicRoomUiReferences();
    }

    private void ResolveRoomUiReferencesFromChildren()
    {
        if (roomPanel == null)
        {
            return;
        }

        if (roomNameInput == null)
        {
            roomNameInput = FindChildComponentByName<TMP_InputField>(roomPanel.transform, "RoomNameInput");
        }

        if (roomPasswordInput == null)
        {
            roomPasswordInput = FindChildComponentByName<TMP_InputField>(roomPanel.transform, "RoomPasswordInput");
        }

        if (maxPlayersPrevButton == null)
        {
            maxPlayersPrevButton = FindChildComponentByName<Button>(roomPanel.transform, "MaxPlayersPrevButton");
        }

        if (maxPlayersNextButton == null)
        {
            maxPlayersNextButton = FindChildComponentByName<Button>(roomPanel.transform, "MaxPlayersNextButton");
        }

        if (maxPlayersValueText == null)
        {
            maxPlayersValueText = FindChildComponentByName<TextMeshProUGUI>(roomPanel.transform, "MaxPlayersValueText");
        }

        if (createRoomButton == null)
        {
            createRoomButton = FindChildComponentByName<Button>(roomPanel.transform, "CreateRoomButton");
        }

        if (joinRoomButton == null)
        {
            joinRoomButton = FindChildComponentByName<Button>(roomPanel.transform, "JoinRoomButton");
        }

        if (roomCloseButton == null)
        {
            roomCloseButton = FindChildComponentByName<Button>(roomPanel.transform, "RoomCloseButton");
        }

        if (privateRoomToggle == null)
        {
            privateRoomToggle = FindChildComponentByName<Toggle>(roomPanel.transform, "PrivateRoomToggle");
        }

        if (modeToggleButton == null)
        {
            modeToggleButton = FindChildComponentByName<Button>(roomPanel.transform, "ModeToggleButton");
        }

        if (modeValueText == null)
        {
            modeValueText = FindChildComponentByName<TextMeshProUGUI>(roomPanel.transform, "ModeValueText");
        }

        if (modeDescriptionText == null)
        {
            modeDescriptionText = FindChildComponentByName<TextMeshProUGUI>(roomPanel.transform, "ModeDescriptionText");
        }
    }

    private void ResolvePublicRoomUiReferences()
    {
        if (publicRoomPanel == null)
        {
            publicRoomPanel = FindGameObjectByName("PublicRoomPanel");
        }

        if (publicRoomPanel == null)
        {
            Debug.LogWarning("[LobbyMatchmakingUI] PublicRoomPanel is not assigned. Please wire scene UI objects.");
            return;
        }

        if (publicRoomListContent == null)
        {
            publicRoomListContent = FindChildComponentByName<RectTransform>(publicRoomPanel.transform, "Content");
        }

        if (publicRoomCloseButton == null)
        {
            publicRoomCloseButton = FindChildComponentByName<Button>(publicRoomPanel.transform, "PublicRoomCloseButton");
        }

        if (publicRoomEmptyText == null)
        {
            publicRoomEmptyText = FindChildComponentByName<TextMeshProUGUI>(publicRoomPanel.transform, "PublicRoomEmptyText");
        }

        if (publicRoomDirectJoinButton == null)
        {
            publicRoomDirectJoinButton = FindChildComponentByName<Button>(publicRoomPanel.transform, "PublicRoomDirectJoinButton");
        }

        if (publicRoomRefreshButton == null)
        {
            publicRoomRefreshButton = FindChildComponentByName<Button>(publicRoomPanel.transform, "PublicRoomRefreshButton");
        }
    }

    private void RebuildPublicRoomList(IReadOnlyList<SessionInfo> sessions)
    {
        if (publicRoomListContent == null)
        {
            return;
        }

        for (int i = publicRoomListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(publicRoomListContent.GetChild(i).gameObject);
        }

        int rowIndex = 0;
        if (sessions != null)
        {
            Debug.Log($"[PublicRoom] Session list received: {sessions.Count}");
            for (int i = 0; i < sessions.Count; i++)
            {
                SessionInfo session = sessions[i];
                Debug.Log($"[PublicRoom] Session[{i}] name={session.Name}, players={session.PlayerCount}/{session.MaxPlayers}, visible={session.IsVisible}");
                CreatePublicRoomRow(publicRoomListContent, rowIndex, session);
                rowIndex++;
            }
        }

        if (publicRoomEmptyText != null)
        {
            publicRoomEmptyText.gameObject.SetActive(rowIndex == 0);
        }

        float viewportHeight = publicRoomListContent.parent is RectTransform parentRect
            ? parentRect.rect.height
            : 0f;
        float requiredHeight = Mathf.Max(viewportHeight, rowIndex * 70f + 8f);
        publicRoomListContent.sizeDelta = new Vector2(publicRoomListContent.sizeDelta.x, requiredHeight);
    }

    private void CreatePublicRoomRow(Transform parent, int index, SessionInfo session)
    {
        var row = new GameObject($"RoomRow_{index + 1}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        var rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, -index * 70f);
        rowRect.sizeDelta = new Vector2(0f, 64f);
        row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

        var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.childControlHeight = true;
        rowLayout.childControlWidth = false;
        rowLayout.childForceExpandHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.spacing = 6f;
        rowLayout.padding = new RectOffset(8, 8, 4, 4);

        CreateRowCell(row.transform, (index + 1).ToString(), 60f, TextAlignmentOptions.Center);

        string displayName = session.Name;
        int separator = displayName.IndexOf(PrivateRoomSeparator, System.StringComparison.Ordinal);
        if (separator >= 0)
        {
            displayName = displayName.Substring(0, separator);
        }

        CreateRowCell(row.transform, $"{displayName} ({session.PlayerCount}/{session.MaxPlayers})", 430f, TextAlignmentOptions.Left);

        var joinButton = CreateRowJoinButton(row.transform, "Join");
        bool isFull = session.PlayerCount >= session.MaxPlayers;
        joinButton.interactable = isFull == false;
        joinButton.onClick.AddListener(() => JoinSessionFromList(session));
    }

    private void JoinSessionFromList(SessionInfo session)
    {
        string roomName = session.Name;
        int separator = roomName.IndexOf(PrivateRoomSeparator, System.StringComparison.Ordinal);
        if (separator >= 0)
        {
            string baseName = roomName.Substring(0, separator);
            ShowPublicRoomPanel(false);
            roomPanelCreateMode = false;
            ShowRoomPanel(true);
            SetRoomPanelMode(false);
            if (roomNameInput != null)
            {
                roomNameInput.text = baseName;
            }

            if (roomPasswordInput != null)
            {
                roomPasswordInput.text = string.Empty;
            }

            UpdatePasswordVisibility();
            FocusPasswordInput();
            ShowPopup("Enter password to join this room.");
            return;
        }

        if (launcher != null)
        {
            launcher.StartMatchmaking(roomName, Mathf.Max(1, session.MaxPlayers));
        }
    }

    private void ClosePublicRoomPanel()
    {
        ShowPublicRoomPanel(false);
    }

    private void OpenDirectJoinFromPublic()
    {
        ShowPublicRoomPanel(false);
        OpenHostCreateRoomPopup();
    }

    private void RefreshPublicRoomList()
    {
        if (launcher != null)
        {
            launcher.RequestSessionListRefresh();
            RebuildPublicRoomList(launcher.CachedSessionList);
            return;
        }

        RebuildPublicRoomList(null);
    }

    private void CreateRowCell(Transform parent, string text, float width, TextAlignmentOptions align)
    {
        var cellObj = new GameObject("Cell", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
        cellObj.transform.SetParent(parent, false);
        var layout = cellObj.GetComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.minWidth = width;
        var tmp = cellObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 20f;
        tmp.color = Color.black;
        tmp.alignment = align;
        tmp.raycastTarget = false;
    }

    private Button CreateRowJoinButton(Transform parent, string text)
    {
        var buttonObj = new GameObject("JoinButton", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, false);
        var layout = buttonObj.GetComponent<LayoutElement>();
        layout.preferredWidth = 140f;
        layout.minWidth = 140f;
        buttonObj.GetComponent<Image>().color = new Color(0.82f, 0.82f, 0.82f, 1f);

        var labelObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(buttonObj.transform, false);
        var rect = labelObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var label = labelObj.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 20f;
        label.color = Color.black;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        return buttonObj.GetComponent<Button>();
    }

    private void ConfigureRoomPanelRaycasts()
    {
        if (roomPanel == null)
        {
            return;
        }

        var texts = roomPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
            {
                continue;
            }

            texts[i].raycastTarget = false;
        }
    }

private void ResolveMainMenuButtons()
    {
        if (publicButton == null)
        {
            publicButton = FindButtonByName("BtnPublic");
            if (publicButton == null)
            {
                publicButton = FindButtonByName("BtnSkin");
            }
        }

        if (changeSkinButton == null)
        {
            changeSkinButton = FindButtonByName("BtnSkin");
            if (changeSkinButton == null)
            {
                changeSkinButton = FindButtonByName("BtnSettings");
            }
        }

        if (skinSelectPanel == null)
        {
            skinSelectPanel = FindObjectByName<UISkinSelectController>("SkinSelectPanel");
            if (skinSelectPanel == null)
            {
                skinSelectPanel = FindFirstObjectByType<UISkinSelectController>(FindObjectsInactive.Include);
            }
        }
    }

    private UIGenericButton FindButtonByName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        var buttons = FindObjectsByType<UIGenericButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (button != null && button.gameObject.name == objectName)
            {
                return button;
            }
        }

        return null;
    }

    private T FindObjectByName<T>(string objectName) where T : Component
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        var objects = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < objects.Length; i++)
        {
            var candidate = objects[i];
            if (candidate != null && candidate.gameObject.name == objectName)
            {
                return candidate;
            }
        }

        return null;
    }

    private GameObject FindGameObjectByName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            var transform = transforms[i];
            if (transform != null && transform.gameObject.name == objectName)
            {
                return transform.gameObject;
            }
        }

        return null;
    }


    private T FindChildComponentByName<T>(Transform root, string childName) where T : Component
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        var children = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < children.Length; i++)
        {
            var component = children[i];
            if (component != null && component.gameObject.name == childName)
            {
                return component;
            }
        }

        return null;
    }

    private void NormalizeRoomPlayerSettings()
    {
        minRoomPlayers = Mathf.Max(1, minRoomPlayers);
        maxRoomPlayers = Mathf.Max(minRoomPlayers, maxRoomPlayers);
        selectedMaxPlayers = Mathf.Clamp(selectedMaxPlayers, minRoomPlayers, maxRoomPlayers);
    }

    private void RefreshMaxPlayersUi()
    {
        if (maxPlayersValueText != null)
        {
            maxPlayersValueText.text = selectedMaxPlayers.ToString();
        }

        if (maxPlayersPrevButton != null)
        {
            maxPlayersPrevButton.interactable = selectedMaxPlayers > minRoomPlayers;
        }

        if (maxPlayersNextButton != null)
        {
            maxPlayersNextButton.interactable = selectedMaxPlayers < maxRoomPlayers;
        }
    }

    private void ApplyRoomInputConstraints()
    {
        if (roomNameInput != null)
        {
            roomNameInput.characterLimit = Mathf.Max(1, roomTitleCharacterLimit);
        }
    }

    private void IncreaseMaxPlayers()
    {
        selectedMaxPlayers = Mathf.Clamp(selectedMaxPlayers + 1, minRoomPlayers, maxRoomPlayers);
        RefreshMaxPlayersUi();
    }

    private void DecreaseMaxPlayers()
    {
        selectedMaxPlayers = Mathf.Clamp(selectedMaxPlayers - 1, minRoomPlayers, maxRoomPlayers);
        RefreshMaxPlayersUi();
    }

    private void ToggleRoomMode()
    {
        selectedRoomMode = selectedRoomMode == RoomMode.Classic ? RoomMode.Nuguri : RoomMode.Classic;
        RefreshRoomModeUi();
    }

private void RefreshRoomModeUi()
    {
        if (modeValueText != null)
        {
            modeValueText.text = selectedRoomMode == RoomMode.Classic ? "클래식 모드" : "너구리 모드";
        }

        if (modeDescriptionText != null)
        {
            modeDescriptionText.text = selectedRoomMode == RoomMode.Classic
                ? "클래식 모드는 일반 플레이어는 술래를 피해 달아나고, 술래는 모든 일반 플레이어를 잡는 모드입니다."
                : "너구리 모드는 특수 규칙으로 진행되는 이벤트 모드입니다.";
        }
    }

    private void OnPrivateRoomToggleChanged(bool _)
    {
        UpdatePasswordVisibility();
    }

private void UpdatePasswordVisibility()
    {
        bool visible = roomPanelCreateMode == false || privateRoomToggle == null || privateRoomToggle.isOn;
        if (roomPasswordInput != null)
        {
            roomPasswordInput.gameObject.SetActive(visible);
            if (visible == false)
            {
                roomPasswordInput.text = string.Empty;
            }
        }
    }

    private void CloseRoomPanel()
    {
        ShowRoomPanel(false);
        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }

    private void HandleRoomInputTabNavigation()
    {
        if (Keyboard.current == null || roomPanel == null || roomPanel.activeInHierarchy == false)
        {
            return;
        }

        if (Keyboard.current.tabKey.wasPressedThisFrame == false)
        {
            return;
        }

        if (EventSystem.current == null)
        {
            return;
        }

        bool shiftPressed = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null)
        {
            FocusRoomInput();
            return;
        }

        if (roomNameInput != null && selected == roomNameInput.gameObject)
        {
            if (shiftPressed)
            {
                FocusPasswordInput();
            }
            else
            {
                FocusPasswordInput();
            }
            return;
        }

        if (roomPasswordInput != null && selected == roomPasswordInput.gameObject)
        {
            FocusRoomInput();
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
