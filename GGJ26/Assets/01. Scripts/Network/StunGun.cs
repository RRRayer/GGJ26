using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StunGun : NetworkBehaviour
{
    [SerializeField] private float range = 6f;
    [SerializeField] private float cooldownSeconds = 5f;
    [SerializeField] private LayerMask hitMask = -1;
    [Header("Crosshair")]
    [SerializeField] private bool showCrosshair = true;
    [SerializeField] private int crosshairSize = 24;
    [SerializeField] private int crosshairThickness = 2;
    [SerializeField] private Color crosshairColor = Color.white;
    [Header("Hit Effect")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float hitEffectLifetime = 1.5f;
    [Header("Audio")]
    [SerializeField] private AudioCueEventChannelSO sfxEventChannel;
    [SerializeField] private AudioConfigurationSO sfxConfiguration;
    [SerializeField] private AudioCueSO shootSfxCue;
    [SerializeField] private AudioCueSO hitSfxCue;

    private PlayerRole role;
    private float lastFireTime = -999f;
    private Camera mainCamera;
    private PlayerElimination elimination;

    private static GameObject crosshairRoot;
    private static Image crosshairImage;

    private void Awake()
    {
        role = GetComponent<PlayerRole>();
        mainCamera = Camera.main;
        elimination = GetComponent<PlayerElimination>();
    }

    private void Update()
    {
        if (Object == null || Object.HasInputAuthority == false)
        {
            return;
        }

        UpdateCrosshair();

        if (role != null && role.IsSeeker == false)
        {
            return;
        }

        if (Mouse.current == null || Mouse.current.leftButton.wasPressedThisFrame == false)
        {
            return;
        }

        if (Time.time - lastFireTime < cooldownSeconds)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
        }

        lastFireTime = Time.time;
        PlaySfx(shootSfxCue, transform.position);
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore) == false)
        {
            return;
        }

        SpawnHitEffect(hit.point, hit.normal);
        RpcPlayHitSfx(hit.point);

        var target = hit.collider.GetComponentInParent<PlayerElimination>();
        if (target == null)
        {
            var npc = hit.collider.GetComponentInParent<BaseNPC>();
            if (npc != null)
            {
                Destroy(npc.gameObject);
            }
            return;
        }

        target.RpcRequestEliminate();
    }

    private void SpawnHitEffect(Vector3 position, Vector3 normal)
    {
        if (hitEffectPrefab == null)
        {
            return;
        }

        var rotation = normal.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(normal) : Quaternion.identity;
        var effect = Instantiate(hitEffectPrefab, position, rotation);
        if (hitEffectLifetime > 0f)
        {
            Destroy(effect, hitEffectLifetime);
        }
    }

    private void PlaySfx(AudioCueSO cue, Vector3 position)
    {
        if (sfxEventChannel == null || cue == null)
        {
            return;
        }

        sfxEventChannel.RaisePlayEvent(cue, sfxConfiguration, position);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RpcPlayHitSfx(Vector3 position)
    {
        PlaySfx(hitSfxCue, position);
    }

    private void UpdateCrosshair()
    {
        if (showCrosshair == false)
        {
            SetCrosshairVisible(false);
            return;
        }

        if (role != null && role.HasRoleAssigned() == false)
        {
            SetCrosshairVisible(false);
            return;
        }

        bool isSeeker = role == null || role.IsSeeker;
        bool isEliminated = elimination != null && elimination.IsEliminated;
        SetCrosshairVisible(isSeeker && isEliminated == false);
    }

    private void SetCrosshairVisible(bool visible)
    {
        if (visible)
        {
            EnsureCrosshair();
        }

        if (crosshairRoot != null)
        {
            crosshairRoot.SetActive(visible);
        }
    }

    private void EnsureCrosshair()
    {
        if (crosshairRoot != null)
        {
            return;
        }

        crosshairRoot = new GameObject("SeekerCrosshairUI");
        var canvas = crosshairRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        crosshairRoot.AddComponent<CanvasScaler>();
        crosshairRoot.AddComponent<GraphicRaycaster>();

        var imageObject = new GameObject("Crosshair");
        imageObject.transform.SetParent(crosshairRoot.transform, false);
        crosshairImage = imageObject.AddComponent<Image>();

        var rect = crosshairImage.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(crosshairSize, crosshairSize);

        crosshairImage.sprite = CreateCrosshairSprite(crosshairSize, crosshairThickness, crosshairColor);
        crosshairImage.raycastTarget = false;
    }

    private Sprite CreateCrosshairSprite(int size, int thickness, Color color)
    {
        int texSize = Mathf.Max(8, size);
        int center = texSize / 2;
        int half = texSize / 2;
        int halfThickness = Mathf.Max(1, thickness / 2);

        var tex = new Texture2D(texSize, texSize, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Point;
        var clear = new Color(0, 0, 0, 0);

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                bool vertical = Mathf.Abs(x - center) <= halfThickness && Mathf.Abs(y - center) <= half;
                bool horizontal = Mathf.Abs(y - center) <= halfThickness && Mathf.Abs(x - center) <= half;
                tex.SetPixel(x, y, (vertical || horizontal) ? color : clear);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), texSize);
    }

    private void OnDestroy()
    {
        if (Object != null && Object.HasInputAuthority && crosshairRoot != null)
        {
            Destroy(crosshairRoot);
            crosshairRoot = null;
            crosshairImage = null;
        }
    }
}
