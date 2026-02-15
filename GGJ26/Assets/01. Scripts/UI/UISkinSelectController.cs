using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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

    [Header("Input")]
    [SerializeField] private KeyCode prevKey = KeyCode.LeftArrow;
    [SerializeField] private KeyCode nextKey = KeyCode.RightArrow;
    [SerializeField] private KeyCode prevAltKey = KeyCode.A;
    [SerializeField] private KeyCode nextAltKey = KeyCode.D;
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private int currentIndex;
    private int equippedIndex;
    private bool isOpen;

    private void Awake()
    {
        equippedIndex = SeekerSkinSelection.LoadSelectedSkinIndex(defaultSkinIndex);
        currentIndex = Mathf.Clamp(equippedIndex, 0, Mathf.Max(0, skins.Length - 1));

        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }
    }

    private void OnEnable()
    {
        if (prevButton != null) prevButton.onClick.AddListener(Prev);
        if (nextButton != null) nextButton.onClick.AddListener(Next);
        if (equipButton != null) equipButton.onClick.AddListener(EquipCurrent);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        ApplyPreview();
    }

    private void OnDisable()
    {
        if (prevButton != null) prevButton.onClick.RemoveListener(Prev);
        if (nextButton != null) nextButton.onClick.RemoveListener(Next);
        if (equipButton != null) equipButton.onClick.RemoveListener(EquipCurrent);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
    }

    private void Update()
    {
        if (isOpen == false)
        {
            return;
        }

        if (WasPressedThisFrame(prevKey) || WasPressedThisFrame(prevAltKey))
        {
            Prev();
        }

        if (WasPressedThisFrame(nextKey) || WasPressedThisFrame(nextAltKey))
        {
            Next();
        }

        if (WasPressedThisFrame(closeKey))
        {
            Close();
        }
    }

    public void Open()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        equippedIndex = SeekerSkinSelection.LoadSelectedSkinIndex(defaultSkinIndex);
        currentIndex = Mathf.Clamp(equippedIndex, 0, Mathf.Max(0, skins.Length - 1));
        isOpen = true;
        ApplyPreview();
    }

    public void Close()
    {
        isOpen = false;
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void Next()
    {
        if (skins == null || skins.Length == 0)
        {
            return;
        }

        currentIndex = (currentIndex + 1) % skins.Length;
        ApplyPreview();
    }

    public void Prev()
    {
        if (skins == null || skins.Length == 0)
        {
            return;
        }

        currentIndex = (currentIndex - 1 + skins.Length) % skins.Length;
        ApplyPreview();
    }

    public void EquipCurrent()
    {
        if (skins == null || skins.Length == 0)
        {
            return;
        }

        equippedIndex = currentIndex;
        SeekerSkinSelection.SaveSelectedSkinIndex(equippedIndex);
        UpdateEquipLabel();
    }

    private void ApplyPreview()
    {
        if (skins == null)
        {
            return;
        }

        for (int i = 0; i < skins.Length; i++)
        {
            var preview = skins[i] != null ? skins[i].previewRoot : null;
            if (preview != null)
            {
                preview.SetActive(i == currentIndex);
            }
        }

        if (skinNameText != null)
        {
            skinNameText.text = skins.Length > 0 ? skins[currentIndex].skinName : "No Skin";
        }

        UpdateEquipLabel();
    }

    private void UpdateEquipLabel()
    {
        if (equipButtonText == null)
        {
            return;
        }

        equipButtonText.text = currentIndex == equippedIndex ? "Equipped" : "Equip";
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
}
