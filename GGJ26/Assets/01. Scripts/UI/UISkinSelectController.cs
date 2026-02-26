using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using StarterAssets;

public class UISkinSelectController : MonoBehaviour
{
    [System.Serializable]
    private class SkinEntry
    {
        public string skinName = "Skin";
        public GameObject previewRoot;
    }

    [Header("Skin Data")]
    [SerializeField] private SkinEntry[] skins = new SkinEntry[0];
    [SerializeField] private int defaultSkinIndex = 0;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI skinNameText;
    [SerializeField] private TextMeshProUGUI equipButtonText;
    [SerializeField] private GameObject previewActorRoot;
    [SerializeField] private string previewActorName = "PreviewSeekerModel";

    [Header("Input")]
    [SerializeField] private KeyCode prevKey = KeyCode.LeftArrow;
    [SerializeField] private KeyCode nextKey = KeyCode.RightArrow;
    [SerializeField] private KeyCode prevAltKey = KeyCode.A;
    [SerializeField] private KeyCode nextAltKey = KeyCode.D;
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;
    [SerializeField] private bool alwaysOpenInLobby = true;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private float runtimeApplyRetryInterval = 0.5f;
    [SerializeField] private float runtimeApplyTimeout = 15f;

    [Header("RenderTexture Preview (optional)")]
    [SerializeField] private bool useRenderTexturePreview = true;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private RawImage previewRawImage;
    [SerializeField] private Vector3 previewCameraOffset = new Vector3(0f, 1.2f, 2.2f);
    [SerializeField] private Vector2Int previewTextureSize = new Vector2Int(512, 768);
    [SerializeField] private Color previewBackgroundColor = new Color(0f, 0f, 0f, 0f);

    private int currentIndex;
    private int equippedIndex;
    private bool isOpen;
    private PlayerAppearance previewAppearance;
    private bool pendingRuntimeApply;
    private float runtimeApplyDeadline;
    private float nextRuntimeApplyTime;
    private bool previewInputDisabled;
    private bool lobbyInputPrepared;
    private RenderTexture previewRenderTexture;
    private bool ownsPreviewTexture;

    private void Awake()
    {
        equippedIndex = SeekerSkinSelection.LoadSelectedSkinIndex(defaultSkinIndex);
        currentIndex = Mathf.Clamp(equippedIndex, 0, Mathf.Max(0, GetSkinCount() - 1));

        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        EnsurePreviewActorResolved();
        if (previewActorRoot != null && alwaysOpenInLobby == false)
        {
            previewActorRoot.SetActive(false);
        }

        if (alwaysOpenInLobby)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (previewActorRoot != null)
            {
                previewActorRoot.SetActive(true);
            }

            if (equipButton != null)
            {
                equipButton.gameObject.SetActive(false);
            }

            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(false);
            }

            DisablePreviewInputComponents();
            PrepareLobbyInputForUI();
            var preview = GetPreviewAppearance();
            if (preview != null)
            {
                preview.SetPreviewMode(true);
            }
            SetupRenderTexturePreview();
            isOpen = true;
        }
    }

    private void OnEnable()
    {
        if (prevButton != null) prevButton.onClick.AddListener(Prev);
        if (nextButton != null) nextButton.onClick.AddListener(Next);
        if (alwaysOpenInLobby == false)
        {
            if (equipButton != null) equipButton.onClick.AddListener(EquipCurrent);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }
        ApplyPreview();
        if (alwaysOpenInLobby)
        {
            var preview = GetPreviewAppearance();
            if (preview != null)
            {
                preview.SetPreviewMode(true);
            }
            PrepareLobbyInputForUI();
            SetupRenderTexturePreview();
            ApplyCurrentSelectionImmediately();
        }
    }

    private void OnDisable()
    {
        if (prevButton != null) prevButton.onClick.RemoveListener(Prev);
        if (nextButton != null) nextButton.onClick.RemoveListener(Next);
        if (alwaysOpenInLobby == false)
        {
            if (equipButton != null) equipButton.onClick.RemoveListener(EquipCurrent);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }
    }

    private void Update()
    {
        if (pendingRuntimeApply && Time.unscaledTime >= nextRuntimeApplyTime)
        {
            bool applied = ApplyEquippedSkinToLocalSeeker();
            if (applied)
            {
                pendingRuntimeApply = false;
            }
            else if (Time.unscaledTime > runtimeApplyDeadline)
            {
                pendingRuntimeApply = false;
                if (enableDebugLogs)
                {
                    Debug.LogWarning("[SkinSelect] Runtime apply timed out. Keeping saved value only.");
                }
            }
            else
            {
                nextRuntimeApplyTime = Time.unscaledTime + Mathf.Max(0.1f, runtimeApplyRetryInterval);
            }
        }

        if (isOpen == false)
        {
            return;
        }

        if (alwaysOpenInLobby)
        {
            ForceLobbyCursorUnlocked();
        }

        if (WasPressedThisFrame(prevKey) || WasPressedThisFrame(prevAltKey))
        {
            Prev();
        }

        if (WasPressedThisFrame(nextKey) || WasPressedThisFrame(nextAltKey))
        {
            Next();
        }

        if (alwaysOpenInLobby == false && WasPressedThisFrame(closeKey))
        {
            Close();
        }
    }

    public void Open()
    {
        if (alwaysOpenInLobby)
        {
            isOpen = true;
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (previewActorRoot != null)
            {
                previewActorRoot.SetActive(true);
            }

            var preview = GetPreviewAppearance();
            if (preview != null)
            {
                preview.SetPreviewMode(true);
            }

            SetupRenderTexturePreview();
            ApplyPreview();
            ApplyCurrentSelectionImmediately();
            return;
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        equippedIndex = SeekerSkinSelection.LoadSelectedSkinIndex(defaultSkinIndex);
        currentIndex = Mathf.Clamp(equippedIndex, 0, Mathf.Max(0, GetSkinCount() - 1));
        isOpen = true;
        if (previewActorRoot != null)
        {
            previewActorRoot.SetActive(true);
        }

        var previewComp = GetPreviewAppearance();
        if (previewComp != null)
        {
            previewComp.SetPreviewMode(true);
        }
        ApplyPreview();
    }

    public void Close()
    {
        if (alwaysOpenInLobby)
        {
            return;
        }

        isOpen = false;
        if (previewAppearance != null)
        {
            previewAppearance.SetPreviewMode(false);
        }

        if (previewActorRoot != null)
        {
            previewActorRoot.SetActive(false);
        }
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void Next()
    {
        int skinCount = GetSkinCount();
        if (skinCount == 0)
        {
            return;
        }

        currentIndex = (currentIndex + 1) % skinCount;
        ApplyPreview();
        ApplyCurrentSelectionImmediately();
    }

    public void Prev()
    {
        int skinCount = GetSkinCount();
        if (skinCount == 0)
        {
            return;
        }

        currentIndex = (currentIndex - 1 + skinCount) % skinCount;
        ApplyPreview();
        ApplyCurrentSelectionImmediately();
    }

    public void EquipCurrent()
    {
        if (GetSkinCount() == 0)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[SkinSelect] Equip ignored: skin count is 0.");
            }
            return;
        }

        equippedIndex = currentIndex;
        SeekerSkinSelection.SaveSelectedSkinIndex(equippedIndex);
        if (enableDebugLogs)
        {
            Debug.Log($"[SkinSelect] Equip clicked. currentIndex={currentIndex}, savedIndex={equippedIndex}");
        }
        pendingRuntimeApply = true;
        runtimeApplyDeadline = Time.unscaledTime + Mathf.Max(1f, runtimeApplyTimeout);
        nextRuntimeApplyTime = Time.unscaledTime;
        ApplyEquippedSkinToLocalSeeker();
        UpdateEquipLabel();
    }

    private void ApplyCurrentSelectionImmediately()
    {
        equippedIndex = currentIndex;
        SeekerSkinSelection.SaveSelectedSkinIndex(equippedIndex);
        pendingRuntimeApply = true;
        runtimeApplyDeadline = Time.unscaledTime + Mathf.Max(1f, runtimeApplyTimeout);
        nextRuntimeApplyTime = Time.unscaledTime;
        ApplyEquippedSkinToLocalSeeker();
        UpdateEquipLabel();
    }

    private void ApplyPreview()
    {
        int skinCount = GetSkinCount();
        if (skinCount <= 0)
        {
            return;
        }

        for (int i = 0; i < skinCount; i++)
        {
            var preview = i < skins.Length && skins[i] != null ? skins[i].previewRoot : null;
            if (preview != null)
            {
                preview.SetActive(i == currentIndex);
            }
        }

        if (previewAppearance != null)
        {
            previewAppearance.SetPreviewSeekerSkinIndex(currentIndex);
        }

        if (skinNameText != null)
        {
            if (skins != null && currentIndex < skins.Length && string.IsNullOrWhiteSpace(skins[currentIndex].skinName) == false)
            {
                skinNameText.text = skins[currentIndex].skinName;
            }
            else
            {
                skinNameText.text = $"Mask {currentIndex + 1}";
            }
        }

        UpdateEquipLabel();
    }

    private void UpdateEquipLabel()
    {
        if (equipButtonText == null)
        {
            return;
        }

        equipButtonText.text = alwaysOpenInLobby ? "Selected" : (currentIndex == equippedIndex ? "Equipped" : "Equip");
    }

    private bool WasPressedThisFrame(KeyCode key)
    {
        if (Keyboard.current != null)
        {
            switch (key)
            {
                case KeyCode.LeftArrow:
                    return Keyboard.current.leftArrowKey.wasPressedThisFrame;
                case KeyCode.RightArrow:
                    return Keyboard.current.rightArrowKey.wasPressedThisFrame;
                case KeyCode.A:
                    return Keyboard.current.aKey.wasPressedThisFrame;
                case KeyCode.D:
                    return Keyboard.current.dKey.wasPressedThisFrame;
                case KeyCode.Escape:
                    return Keyboard.current.escapeKey.wasPressedThisFrame;
            }
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(key);
#else
        return false;
#endif
    }

    private int GetSkinCount()
    {
        int dataCount = skins != null ? skins.Length : 0;
        int previewCount = 0;
        var preview = GetPreviewAppearance();
        if (preview != null)
        {
            previewCount = preview.GetSeekerMaskCount();
        }
        return Mathf.Max(dataCount, previewCount);
    }

    private PlayerAppearance GetPreviewAppearance()
    {
        EnsurePreviewActorResolved();

        if (previewAppearance == null && previewActorRoot != null)
        {
            previewAppearance = previewActorRoot.GetComponent<PlayerAppearance>();
        }

        return previewAppearance;
    }

    private void EnsurePreviewActorResolved()
    {
        if (previewActorRoot != null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(previewActorName))
        {
            return;
        }

        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform tr = allTransforms[i];
            if (tr == null || tr.name != previewActorName)
            {
                continue;
            }

            Scene scene = tr.gameObject.scene;
            if (scene.IsValid() == false || scene.isLoaded == false)
            {
                continue;
            }

            previewActorRoot = tr.gameObject;
            break;
        }
    }

    private void DisablePreviewInputComponents()
    {
        if (previewInputDisabled || previewActorRoot == null)
        {
            return;
        }

        var starterInputs = previewActorRoot.GetComponentsInChildren<StarterAssetsInputs>(true);
        for (int i = 0; i < starterInputs.Length; i++)
        {
            if (starterInputs[i] == null)
            {
                continue;
            }

            starterInputs[i].ForceCursorUnlocked();
            starterInputs[i].enabled = false;
        }

        var playerInputs = previewActorRoot.GetComponentsInChildren<PlayerInput>(true);
        for (int i = 0; i < playerInputs.Length; i++)
        {
            if (playerInputs[i] == null)
            {
                continue;
            }

            playerInputs[i].enabled = false;
        }

        previewInputDisabled = true;
    }

    private void ForceLobbyCursorUnlocked()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LateUpdate()
    {
        if (alwaysOpenInLobby == false || isOpen == false)
        {
            return;
        }

        UpdatePreviewCameraPose();

        // Some gameplay components can re-lock the cursor later in the frame.
        // Enforce unlocked cursor state in LateUpdate so UGUI remains clickable.
        ForceLobbyCursorUnlocked();
    }

    private void PrepareLobbyInputForUI()
    {
        ForceLobbyCursorUnlocked();

        if (lobbyInputPrepared)
        {
            return;
        }

        var spectators = FindObjectsByType<SpectatorController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < spectators.Length; i++)
        {
            if (spectators[i] != null)
            {
                spectators[i].enabled = false;
            }
        }

        var allInputs = FindObjectsByType<StarterAssetsInputs>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allInputs.Length; i++)
        {
            if (allInputs[i] == null)
            {
                continue;
            }

            allInputs[i].ForceCursorUnlocked();
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.sendNavigationEvents = true;
        }

        lobbyInputPrepared = true;
    }

    private void SetupRenderTexturePreview()
    {
        if (useRenderTexturePreview == false || previewActorRoot == null)
        {
            return;
        }

        if (previewCamera == null || previewRawImage == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[SkinSelect] Assign previewCamera and previewRawImage in Lobby scene.");
            }
            return;
        }

        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = previewBackgroundColor;
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = 20f;
        previewCamera.enabled = true;

        var listener = previewCamera.GetComponent<AudioListener>();
        if (listener != null)
        {
            listener.enabled = false;
        }

        int width = Mathf.Max(128, previewTextureSize.x);
        int height = Mathf.Max(128, previewTextureSize.y);
        bool needsNewTexture = previewRenderTexture == null || previewRenderTexture.width != width || previewRenderTexture.height != height;
        if (needsNewTexture)
        {
            if (ownsPreviewTexture && previewRenderTexture != null)
            {
                previewRenderTexture.Release();
                Destroy(previewRenderTexture);
            }

            previewRenderTexture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);
            previewRenderTexture.name = "RT_SkinPreview";
            previewRenderTexture.Create();
            ownsPreviewTexture = true;
        }

        previewCamera.targetTexture = previewRenderTexture;
        previewCamera.rect = new Rect(0f, 0f, 1f, 1f);
        previewRawImage.texture = previewRenderTexture;

        UpdatePreviewCameraPose();
    }

    private void UpdatePreviewCameraPose()
    {
        if (previewCamera == null || previewActorRoot == null)
        {
            return;
        }

        Vector3 lookPos = previewActorRoot.transform.position + Vector3.up * 1.4f;
        previewCamera.transform.position = lookPos + previewCameraOffset;
        previewCamera.transform.LookAt(lookPos);
    }

    private void OnDestroy()
    {
        if (ownsPreviewTexture && previewRenderTexture != null)
        {
            previewRenderTexture.Release();
            Destroy(previewRenderTexture);
            previewRenderTexture = null;
        }
    }

    private bool ApplyEquippedSkinToLocalSeeker()
    {
        var roles = FindObjectsByType<PlayerRole>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        bool requested = false;
        if (enableDebugLogs)
        {
            Debug.Log($"[SkinSelect] Trying runtime apply. roleCount={roles.Length}, targetSkinIndex={equippedIndex}");
        }

        for (int i = 0; i < roles.Length; i++)
        {
            var candidate = roles[i];
            if (candidate == null || candidate.HasRoleAssigned() == false || candidate.IsSeeker == false)
            {
                continue;
            }

            var networkObject = candidate.GetComponent<Fusion.NetworkObject>();
            if (networkObject == null || networkObject.HasInputAuthority == false)
            {
                continue;
            }

            requested = true;
            if (enableDebugLogs)
            {
                Debug.Log($"[SkinSelect] Runtime apply target found: {candidate.name}, hasStateAuth={networkObject.HasStateAuthority}, hasInputAuth={networkObject.HasInputAuthority}");
            }
            candidate.RequestSeekerSkinChange(equippedIndex);
        }

        if (enableDebugLogs && requested == false)
        {
            Debug.Log("[SkinSelect] Runtime apply skipped: no local seeker with input authority found. Save only.");
        }

        return requested;
    }
}
