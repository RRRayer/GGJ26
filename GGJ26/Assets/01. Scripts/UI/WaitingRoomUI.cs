using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WaitingRoomUI : MonoBehaviour
{
    [System.Serializable]
    private class WaitingRoomSlotWidgets
    {
        public RectTransform root;
        public Image portraitImage;
        public RawImage previewImage;
        public Image baseImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI statusText;
    }

    [SerializeField] private WaitingRoomState waitingRoomState;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonLabel;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI startButtonLabel;
    [SerializeField] private Button leaveButton;
    [SerializeField] private TextMeshProUGUI leaveButtonLabel;
    [SerializeField] private WaitingRoomSlotWidgets[] slots = new WaitingRoomSlotWidgets[4];
    [SerializeField] private Transform previewStageRoot;
    [SerializeField] private Camera[] previewCameras = new Camera[4];
    [SerializeField] private GameObject[] previewActors = new GameObject[4];
    [SerializeField] private Vector2Int previewTextureSize = new Vector2Int(256, 320);

    private static readonly Color LocalBaseColor = new Color(0.35f, 0.9f, 0.4f, 0.95f);
    private static readonly Color RemoteBaseColor = new Color(0.34f, 0.78f, 0.95f, 0.95f);
    private static readonly Color EmptyBaseColor = new Color(0.22f, 0.22f, 0.22f, 0.4f);
    private static readonly Color OccupiedPortraitColor = new Color(0.82f, 0.9f, 1f, 0.95f);
    private static readonly Color EmptyPortraitColor = new Color(1f, 1f, 1f, 0.08f);

    private bool lastIsHost;
    private RenderTexture[] previewTextures;
    private int lastLocalSkinIndex = -1;
    private Vector3[] previewInitialLocalPositions;
    private Quaternion[] previewInitialLocalRotations;
    private Vector3[] previewInitialLocalScales;

    private void Awake()
    {
        if (waitingRoomState == null)
        {
            waitingRoomState = FindFirstObjectByType<WaitingRoomState>();
        }

        TryBindSceneWidgets();
        BuildUiIfNeeded();
        ConfigurePreviewStage();
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

        if (leaveButton != null)
        {
            leaveButton.onClick.AddListener(OnLeaveClicked);
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

        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveListener(OnLeaveClicked);
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

        RefreshSlots();
        RefreshPreviewActors();
    }

    private void OnDestroy()
    {
        if (previewTextures == null)
        {
            return;
        }

        for (int i = 0; i < previewTextures.Length; i++)
        {
            if (previewTextures[i] == null)
            {
                continue;
            }

            previewTextures[i].Release();
            Destroy(previewTextures[i]);
        }
    }

    private void RefreshButtons()
    {
        bool isHost = waitingRoomState != null && waitingRoomState.IsHost;
        if (readyButton != null)
        {
            readyButton.gameObject.SetActive(false);
        }

        if (startButton != null)
        {
            startButton.gameObject.SetActive(isHost);
            startButton.interactable = waitingRoomState != null && waitingRoomState.CanStartGame();
        }

        if (leaveButton != null)
        {
            leaveButton.gameObject.SetActive(true);
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

    private void OnLeaveClicked()
    {
        var launcher = FindFirstObjectByType<FusionLauncher>();
        if (launcher != null)
        {
            launcher.ShutdownRunner();
        }

        SceneManager.LoadScene("Lobby");
    }

    private void OnReadyStateChanged(int readyCount, int playerCount, bool allReady)
    {
        if (statusText != null)
        {
            int capacity = waitingRoomState != null ? waitingRoomState.GetSlotCapacity() : 4;
            statusText.text = $"Players {playerCount}/{capacity}";
        }

        RefreshButtons();
        RefreshSlots();

        if (startButton != null)
        {
            startButton.interactable = waitingRoomState != null && waitingRoomState.IsHost && waitingRoomState.CanStartGame();
        }
    }

    private void BuildUiIfNeeded()
    {
        EnsureEventSystem();

        bool hasBoundSlots = slots != null && slots.Length > 0 && slots[0] != null && slots[0].root != null;
        if (statusText != null && startButton != null && leaveButton != null && hasBoundSlots)
        {
            return;
        }

        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            canvas = CreateCanvas("WaitingRoomCanvas");
        }

        var panel = CreatePanel(canvas.transform, "WaitingRoomPanel", new Vector2(520f, 260f));
        statusText = CreateText(panel.transform, "StatusText", "Players 0/0", 28, new Vector2(0.5f, 0.75f));
        startButton = CreateButton(panel.transform, "StartButton", "Start Game", new Vector2(0.5f, 0.35f));
        startButtonLabel = startButton.GetComponentInChildren<TextMeshProUGUI>();

        leaveButton = CreateButton(panel.transform, "LeaveButton", "Leave Room", new Vector2(0.5f, 0.15f));
        leaveButtonLabel = leaveButton.GetComponentInChildren<TextMeshProUGUI>();
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

        if (leaveButton == null)
        {
            var found = GameObject.Find("LeaveButton");
            if (found != null)
            {
                leaveButton = found.GetComponent<Button>();
                leaveButtonLabel = found.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        if (previewStageRoot == null)
        {
            var found = GameObject.Find("PreviewStage");
            if (found != null)
            {
                previewStageRoot = found.transform;
            }
        }

        for (int i = 0; i < previewCameras.Length; i++)
        {
            if (previewCameras[i] == null)
            {
                var found = GameObject.Find($"PreviewCamera{i + 1}");
                if (found != null)
                {
                    previewCameras[i] = found.GetComponent<Camera>();
                }
            }

            if (previewActors[i] == null)
            {
                var found = GameObject.Find($"PreviewActor{i + 1}");
                if (found == null)
                {
                    found = GameObject.Find($"PreviewSeekerModel{i + 1}");
                }
                if (found != null)
                {
                    previewActors[i] = found;
                }
            }
        }

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] ??= new WaitingRoomSlotWidgets();
            int slotNumber = i + 1;

            if (slots[i].root == null)
            {
                var found = GameObject.Find($"PlayerSlot{slotNumber}");
                if (found != null)
                {
                    slots[i].root = found.GetComponent<RectTransform>();
                }
            }

            if (slots[i].portraitImage == null)
            {
                var found = GameObject.Find($"PlayerSlot{slotNumber}_Portrait");
                if (found != null)
                {
                    slots[i].portraitImage = found.GetComponent<Image>();
                }
            }

            if (slots[i].previewImage == null)
            {
                var found = GameObject.Find($"PlayerSlot{slotNumber}_Preview");
                if (found != null)
                {
                    slots[i].previewImage = found.GetComponent<RawImage>();
                }
            }

            if (slots[i].baseImage == null)
            {
                var found = GameObject.Find($"PlayerSlot{slotNumber}_Base");
                if (found != null)
                {
                    slots[i].baseImage = found.GetComponent<Image>();
                }
            }

            if (slots[i].nameText == null)
            {
                var found = GameObject.Find($"PlayerSlot{slotNumber}_Name");
                if (found != null)
                {
                    slots[i].nameText = found.GetComponent<TextMeshProUGUI>();
                }
            }

            if (slots[i].statusText == null)
            {
                var found = GameObject.Find($"PlayerSlot{slotNumber}_Status");
                if (found != null)
                {
                    slots[i].statusText = found.GetComponent<TextMeshProUGUI>();
                }
            }
        }
    }

    private void RefreshSlots()
    {
        if (waitingRoomState == null || slots == null)
        {
            return;
        }

        int capacity = waitingRoomState.GetSlotCapacity();

        for (int i = 0; i < slots.Length; i++)
        {
            bool slotVisible = i < capacity;
            if (slots[i].root != null)
            {
                slots[i].root.gameObject.SetActive(slotVisible);
            }

            if (slotVisible == false)
            {
                continue;
            }

            if (slots[i].previewImage != null)
            {
                slots[i].previewImage.gameObject.SetActive(false);
            }

            bool occupied = waitingRoomState.TryGetSlotInfo(i, out var player, out bool isReady);
            bool isLocal = occupied && waitingRoomState.IsLocalPlayer(player);
            bool isHostSlot = occupied && waitingRoomState.IsHostPlayer(player);

            if (slots[i].portraitImage != null)
            {
                slots[i].portraitImage.color = occupied ? OccupiedPortraitColor : EmptyPortraitColor;
            }

            if (slots[i].baseImage != null)
            {
                slots[i].baseImage.color = occupied
                    ? (isLocal ? LocalBaseColor : RemoteBaseColor)
                    : EmptyBaseColor;
            }

            if (slots[i].nameText != null)
            {
                if (occupied == false)
                {
                    slots[i].nameText.text = "Empty";
                }
                else if (isLocal)
                {
                    slots[i].nameText.text = "You";
                }
                else
                {
                    slots[i].nameText.text = $"Player {player.PlayerId}";
                }
            }

            if (slots[i].statusText != null)
            {
                if (occupied == false)
                {
                    slots[i].statusText.text = string.Empty;
                }
                else if (isHostSlot)
                {
                    slots[i].statusText.text = "Host";
                }
                else
                {
                    slots[i].statusText.text = isReady ? "In Room" : "Joining";
                }
            }
        }
    }

    private void ConfigurePreviewStage()
    {
        if (previewCameras == null || previewActors == null)
        {
            return;
        }

        previewInitialLocalPositions = new Vector3[previewActors.Length];
        previewInitialLocalRotations = new Quaternion[previewActors.Length];
        previewInitialLocalScales = new Vector3[previewActors.Length];
        previewTextures = new RenderTexture[Mathf.Min(previewCameras.Length, slots.Length)];
        int width = Mathf.Max(64, previewTextureSize.x);
        int height = Mathf.Max(64, previewTextureSize.y);

        for (int i = 0; i < previewTextures.Length; i++)
        {
            if (previewCameras[i] == null || slots[i] == null || slots[i].previewImage == null)
            {
                continue;
            }

            slots[i].previewImage.gameObject.SetActive(false);

            var texture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);
            texture.name = $"WaitingRoomPreview_{i + 1}";
            texture.Create();
            previewTextures[i] = texture;
            previewCameras[i].targetTexture = texture;
            previewCameras[i].enabled = true;
            slots[i].previewImage.texture = texture;
            slots[i].previewImage.color = Color.white;

            SetupPreviewActor(i);
        }
    }

    private void SetupPreviewActor(int index)
    {
        if (index < 0 || index >= previewActors.Length)
        {
            return;
        }

        GameObject actor = previewActors[index];
        if (actor == null)
        {
            return;
        }

        actor.SetActive(true);

        var appearance = actor.GetComponent<PlayerAppearance>();
        if (appearance != null)
        {
            appearance.SetPreviewMode(true);
        }

        DisablePreviewBehaviour<PlayerElimination>(actor);
        DisablePreviewBehaviour<StunGun>(actor);
        DisablePreviewBehaviour<SpectatorController>(actor);
        DisablePreviewBehaviour<CharacterController>(actor);
        DisablePreviewBehaviour<StarterAssets.ThirdPersonController>(actor);
        DisablePreviewBehaviour<BasicRigidBodyPush>(actor);
        DisablePreviewBehaviour<StarterAssets.StarterAssetsInputs>(actor);
        DisablePreviewBehaviour<PlayerInput>(actor);
        DisablePreviewBehaviour<NetworkObject>(actor);
        DisablePreviewBehaviour<FusionPlayerAvatar>(actor);
        DisablePreviewBehaviour<FusionThirdPersonMotor>(actor);
        DisablePreviewBehaviour<NetworkTransform>(actor);
        DisablePreviewBehaviour<FusionThirdPersonCamera>(actor);
        DisablePreviewBehaviour<PlayerRole>(actor);
        DisablePreviewBehaviour<SpectatorSabotageController>(actor);
        DisablePreviewBehaviour<NetworkObjectPrefabData>(actor);
    }

    private void RefreshPreviewActors()
    {
        if (waitingRoomState == null || previewActors == null)
        {
            return;
        }

        int capacity = waitingRoomState.GetSlotCapacity();
        int localSkinIndex = SeekerSkinSelection.LoadSelectedSkinIndex();
        bool localSkinChanged = localSkinIndex != lastLocalSkinIndex;
        if (localSkinChanged)
        {
            lastLocalSkinIndex = localSkinIndex;
        }

        for (int i = 0; i < previewActors.Length; i++)
        {
            GameObject actor = previewActors[i];
            if (actor == null)
            {
                continue;
            }

            PlayerRef player = default;
            bool activeSlot = i < capacity && waitingRoomState.TryGetSlotInfo(i, out player, out _);
            actor.SetActive(activeSlot);

            if (activeSlot == false)
            {
                continue;
            }

            var appearance = actor.GetComponent<PlayerAppearance>();
            if (appearance == null)
            {
                continue;
            }

            appearance.SetPreviewMode(true);
            int skinIndex = waitingRoomState.GetSlotSkinIndex(i);
            if (localSkinChanged || waitingRoomState.IsLocalPlayer(player) == false || skinIndex != 0)
            {
                appearance.SetPreviewSeekerSkinIndex(skinIndex);
            }
        }
    }

    private static void DisablePreviewBehaviour<T>(GameObject actor) where T : Component
    {
        var component = actor.GetComponent<T>();
        if (component is UnityEngine.Behaviour behaviour)
        {
            behaviour.enabled = false;
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
