using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISpectatorSabotageHud : MonoBehaviour
{
    private const string RootName = "SabotageHudRuntime";

    [Header("Sprites")]
    [SerializeField] private Sprite shoeSprite;
    [SerializeField] private Sprite smokeSprite;
    [SerializeField] private Sprite danceSprite;
    [SerializeField] private Sprite keyOneSprite;
    [SerializeField] private Sprite keyTwoSprite;
    [SerializeField] private Sprite keyThreeSprite;

    [Header("Layout")]
    [SerializeField] private Vector2 rootAnchoredPosition = new Vector2(0f, 96f);
    [SerializeField] private Vector2 iconSize = new Vector2(120f, 120f);
    [SerializeField] private Vector2 keySize = new Vector2(42f, 56f);
    [SerializeField] private float slotSpacing = 150f;
    [SerializeField] private Vector2 keyOffset = new Vector2(0f, -12f);
    [SerializeField] private Vector2 titleOffset = new Vector2(0f, 150f);
    [SerializeField] private float titleFontSize = 36f;

    [Header("Style")]
    [SerializeField] private Color availableColor = Color.white;
    [SerializeField] private Color usedColor = new Color(1f, 1f, 1f, 0.28f);
    [SerializeField] private Color selectedOutlineColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color idleOutlineColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private Vector2 outlineDistance = new Vector2(6f, 6f);

    private SpectatorSabotageController sabotageController;
    private RectTransform root;
    private SlotView[] slots;
    private TextMeshProUGUI titleLabel;

    private void Awake()
    {
        EnsureHud();
    }

    private void OnEnable()
    {
        EnsureHud();
        RefreshView();
    }

    private void Update()
    {
        if (root == null)
        {
            EnsureHud();
        }

        ResolveController();
        RefreshView();
    }

    private void EnsureHud()
    {
        if (root != null)
        {
            return;
        }

        var existing = transform.Find(RootName) as RectTransform;
        if (existing != null)
        {
            root = existing;
            slots = CollectExistingSlots(existing);
            titleLabel = existing.Find("Title")?.GetComponent<TextMeshProUGUI>();
            return;
        }

        var rootObject = new GameObject(RootName, typeof(RectTransform));
        rootObject.transform.SetParent(transform, false);
        root = rootObject.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0f);
        root.anchorMax = new Vector2(0.5f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = rootAnchoredPosition;
        root.sizeDelta = new Vector2(slotSpacing * 2f + iconSize.x, iconSize.y + 80f);

        titleLabel = CreateTitle(root);

        slots = new SlotView[3];
        slots[0] = CreateSlot(root, "ShoeSlot", shoeSprite, keyOneSprite, -slotSpacing, "1");
        slots[1] = CreateSlot(root, "SmokeSlot", smokeSprite, keyTwoSprite, 0f, "2");
        slots[2] = CreateSlot(root, "DanceSlot", danceSprite, keyThreeSprite, slotSpacing, "3");
    }

    private SlotView[] CollectExistingSlots(RectTransform parent)
    {
        var collected = new SlotView[3];
        for (int i = 0; i < parent.childCount && i < 3; i++)
        {
            var child = parent.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            collected[i] = new SlotView
            {
                root = child,
                iconImage = child.Find("Icon")?.GetComponent<Image>(),
                keyImage = child.Find("Key")?.GetComponent<Image>(),
                keyText = child.Find("KeyText")?.GetComponent<Text>(),
                outline = child.GetComponent<Outline>()
            };
        }
        return collected;
    }

    private TextMeshProUGUI CreateTitle(RectTransform parent)
    {
        var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObject.transform.SetParent(parent, false);

        var titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0f);
        titleRect.anchorMax = new Vector2(0.5f, 0f);
        titleRect.pivot = new Vector2(0.5f, 0f);
        titleRect.anchoredPosition = titleOffset;
        titleRect.sizeDelta = new Vector2(400f, 48f);

        var label = titleObject.GetComponent<TextMeshProUGUI>();
        label.text = "Sabotage";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = titleFontSize;
        label.raycastTarget = false;

        ApplyDeadTitleStyle(label);
        return label;
    }

    private void ApplyDeadTitleStyle(TextMeshProUGUI label)
    {
        var deadTitle = FindDeadTitleTemplate();
        if (deadTitle == null)
        {
            label.color = new Color32(255, 238, 159, 255);
            return;
        }

        label.font = deadTitle.font;
        label.fontSharedMaterial = deadTitle.fontSharedMaterial;
        label.color = deadTitle.color;
        label.fontStyle = deadTitle.fontStyle;
        label.enableWordWrapping = false;
        label.outlineWidth = deadTitle.outlineWidth;
        label.outlineColor = deadTitle.outlineColor;
        label.characterSpacing = deadTitle.characterSpacing;
    }

    private static TextMeshProUGUI FindDeadTitleTemplate()
    {
        var texts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name == "txtDead")
            {
                return texts[i];
            }
        }

        return null;
    }

    private SlotView CreateSlot(RectTransform parent, string name, Sprite iconSprite, Sprite keySprite, float xOffset, string fallbackKeyText)
    {
        var slotObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
        slotObject.transform.SetParent(parent, false);

        var slotRect = slotObject.GetComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0.5f, 0f);
        slotRect.anchorMax = new Vector2(0.5f, 0f);
        slotRect.pivot = new Vector2(0.5f, 0f);
        slotRect.anchoredPosition = new Vector2(xOffset, 0f);
        slotRect.sizeDelta = iconSize;

        var background = slotObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.3f);
        background.raycastTarget = false;

        var outline = slotObject.GetComponent<Outline>();
        outline.effectColor = idleOutlineColor;
        outline.effectDistance = outlineDistance;

        var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(slotObject.transform, false);
        var iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        var iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = iconSprite;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        var keyObject = new GameObject("Key", typeof(RectTransform), typeof(Image));
        keyObject.transform.SetParent(slotObject.transform, false);
        var keyRect = keyObject.GetComponent<RectTransform>();
        keyRect.anchorMin = new Vector2(0.5f, 0f);
        keyRect.anchorMax = new Vector2(0.5f, 0f);
        keyRect.pivot = new Vector2(0.5f, 1f);
        keyRect.anchoredPosition = keyOffset;
        keyRect.sizeDelta = keySize;
        var keyImage = keyObject.GetComponent<Image>();
        keyImage.sprite = keySprite;
        keyImage.preserveAspect = true;
        keyImage.raycastTarget = false;

        var textObject = new GameObject("KeyText", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(keyObject.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var keyText = textObject.GetComponent<Text>();
        keyText.text = fallbackKeyText;
        keyText.alignment = TextAnchor.MiddleCenter;
        keyText.color = new Color(0.95f, 0.9f, 0.8f, 1f);
        keyText.fontSize = 26;
        keyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        keyText.raycastTarget = false;
        keyText.enabled = keySprite == null;

        return new SlotView
        {
            root = slotRect,
            iconImage = iconImage,
            keyImage = keyImage,
            keyText = keyText,
            outline = outline
        };
    }

    private void ResolveController()
    {
        if (sabotageController != null && sabotageController.Object != null && sabotageController.Object.HasInputAuthority)
        {
            return;
        }

        sabotageController = null;
        var controllers = FindObjectsByType<SpectatorSabotageController>(FindObjectsSortMode.None);
        for (int i = 0; i < controllers.Length; i++)
        {
            var candidate = controllers[i];
            if (candidate == null || candidate.Object == null)
            {
                continue;
            }

            if (candidate.Object.HasInputAuthority)
            {
                sabotageController = candidate;
                break;
            }
        }
    }

    private void RefreshView()
    {
        if (slots == null || slots.Length < 3)
        {
            return;
        }

        bool hasController = sabotageController != null;
        bool show = false;
        SabotageType armed = SabotageType.None;

        bool canUseShoe = false;
        bool canUseSmoke = false;
        bool canUseDance = false;

        if (hasController)
        {
            armed = sabotageController.ArmedType;
            canUseShoe = sabotageController.CanUseShoe;
            canUseSmoke = sabotageController.CanUseSmoke;
            canUseDance = sabotageController.CanUseDance;
            show = sabotageController.enabled && sabotageController.gameObject.activeInHierarchy;
        }

        if (root != null)
        {
            root.gameObject.SetActive(show);
        }

        if (show == false)
        {
            return;
        }

        ApplySlotState(slots[0], canUseShoe, armed == SabotageType.ShoeToss);
        ApplySlotState(slots[1], canUseSmoke, armed == SabotageType.GhostSmoke);
        ApplySlotState(slots[2], canUseDance, armed == SabotageType.PhantomDance);
    }

    private void ApplySlotState(SlotView slot, bool isAvailable, bool isSelected)
    {
        if (slot == null)
        {
            return;
        }

        if (slot.iconImage != null)
        {
            slot.iconImage.color = isAvailable ? availableColor : usedColor;
        }

        if (slot.keyImage != null)
        {
            slot.keyImage.color = isAvailable ? availableColor : usedColor;
        }

        if (slot.keyText != null)
        {
            slot.keyText.color = isAvailable ? availableColor : usedColor;
            slot.keyText.enabled = slot.keyImage == null || slot.keyImage.sprite == null;
        }

        if (slot.outline != null)
        {
            slot.outline.effectColor = isSelected ? selectedOutlineColor : idleOutlineColor;
        }
    }

    private sealed class SlotView
    {
        public RectTransform root;
        public Image iconImage;
        public Image keyImage;
        public Text keyText;
        public Outline outline;
    }
}
