using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StunGun : NetworkBehaviour
{
    [SerializeField] private float range = 6f;
    [SerializeField] private float cooldownSeconds = 5f;
    [SerializeField] private LayerMask hitMask = -1;
    // [Header("Crosshair")]
    // [SerializeField] private bool showCrosshair = true;
    // [SerializeField] private int crosshairSize = 24;
    // [SerializeField] private int crosshairThickness = 2;
    // [SerializeField] private Color crosshairColor = Color.white;
    [Header("Hit Effect")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float hitEffectLifetime = 1.5f;
    [Header("Muzzle Effect")]
    [SerializeField] private Transform shootTransform;
    [SerializeField] private UnityEngine.VFX.VisualEffect shootVfx;
    [Header("AI Death Effect")]
    [SerializeField] private GameObject fogEffectPrefab;
    [Header("Audio")]
    [SerializeField] private AudioCueEventChannelSO sfxEventChannel;
    [SerializeField] private AudioConfigurationSO sfxConfiguration;
    [SerializeField] private AudioCueSO shootSfxCue;
    [SerializeField] private AudioCueSO hitSfxCue;
    [Header("UI Cooldown")]
    private Image cooldownImage;

    private PlayerRole role;
    private float lastFireTime = -999f;
    private Camera mainCamera;
    private PlayerElimination elimination;
    private Animator animator;
    private int animIDShoot;
    private bool rotateToCamera;
    private float rotateTimer;
    [SerializeField] private float rotateDuration = 0.05f;

    private static GameObject crosshairRoot;

    private StunGunFx fx;
    private StunGunCooldownUI cooldownUi;

    private void Awake()
    {
        role = GetComponent<PlayerRole>();
        mainCamera = Camera.main;
        elimination = GetComponent<PlayerElimination>();
        animator = GetComponent<Animator>();
        animIDShoot = Animator.StringToHash("Shoot");
        if (shootTransform == null)
        {
            var found = transform.Find("ShootTransform");
            if (found != null)
            {
                shootTransform = found;
            }
        }
        if (shootVfx == null && shootTransform != null)
        {
            shootVfx = shootTransform.GetComponent<UnityEngine.VFX.VisualEffect>();
        }
        
        // Find the cooldown UI dynamically
        GameObject cooldownUIObject = GameObject.FindGameObjectWithTag("AimCoolDown");
        if (cooldownUIObject != null)
        {
            cooldownImage = cooldownUIObject.GetComponent<Image>();
        }
        else
        {
            Debug.LogWarning("StunGun: Could not find GameObject with tag 'AimCoolDown'.");
        }

        fx = new StunGunFx(
            hitEffectPrefab,
            hitEffectLifetime,
            shootTransform,
            shootVfx,
            fogEffectPrefab,
            sfxEventChannel,
            sfxConfiguration,
            shootSfxCue,
            hitSfxCue);
        cooldownUi = new StunGunCooldownUI(this, cooldownImage, cooldownSeconds);
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

        if (DanceEventPublisher.IsGroupDanceActive)
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

        if (EnsureCamera() == false)
        {
            return;
        }

        lastFireTime = Time.time;
        cooldownUi.StartCooldown();
        RpcTriggerShoot();
        fx.PlayShootSfx(transform.position);
        BeginLookToCamera();

        if (TryBuildAimData(out var aimData) == false)
        {
            return;
        }

        RpcRequestFire(aimData.FireOrigin, aimData.FireDirection);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RpcTriggerShoot()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (animator == null)
        {
            return;
        }

        animator.SetTrigger(animIDShoot);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcPlayMuzzleVfx(Vector3 shootDirection)
    {
        fx.PlayMuzzleVfx(shootDirection);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcSpawnFogEffect(Vector3 position, Quaternion rotation)
    {
        fx.SpawnFogEffect(position, rotation);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcPlayHitSfx(Vector3 position)
    {
        fx.PlayHitSfx(position);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcSpawnHitEffect(Vector3 position, Vector3 normal)
    {
        fx.SpawnHitEffect(position, normal);
    }

    private void UpdateCrosshair()
    {
        // if (showCrosshair == false)
        // {
        //     SetCrosshairVisible(false);
        //     return;
        // }

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

        // crosshairRoot = new GameObject("SeekerCrosshairUI");
        // var canvas = crosshairRoot.AddComponent<Canvas>();
        // canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // crosshairRoot.AddComponent<CanvasScaler>();
        // crosshairRoot.AddComponent<GraphicRaycaster>();

        // var imageObject = new GameObject("Crosshair");
        // imageObject.transform.SetParent(crosshairRoot.transform, false);
        // crosshairImage = imageObject.AddComponent<Image>();

        // var rect = crosshairImage.rectTransform;
        // rect.anchorMin = new Vector2(0.5f, 0.5f);
        // rect.anchorMax = new Vector2(0.5f, 0.5f);
        // rect.anchoredPosition = Vector2.zero;
        // rect.sizeDelta = new Vector2(crosshairSize, crosshairSize);

        // crosshairImage.sprite = CreateCrosshairSprite(crosshairSize, crosshairThickness, crosshairColor);
        // crosshairImage.raycastTarget = false;
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
        }
    }

    private void BeginLookToCamera()
    {
        if (mainCamera == null)
        {
            return;
        }

        rotateToCamera = true;
        rotateTimer = 0f;
    }

    private void LateUpdate()
    {
        if (rotateToCamera == false)
        {
            return;
        }

        if (mainCamera == null)
        {
            rotateToCamera = false;
            return;
        }

        Vector3 forward = mainCamera.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            rotateToCamera = false;
            return;
        }

        float duration = Mathf.Max(0.001f, rotateDuration);
        rotateTimer += Time.deltaTime;
        float t = Mathf.Clamp01(rotateTimer / duration);
        Quaternion target = Quaternion.LookRotation(forward.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, t);

        if (t >= 1f)
        {
            rotateToCamera = false;
        }
    }

    private bool EnsureCamera()
    {
        if (mainCamera != null)
        {
            return true;
        }

        mainCamera = Camera.main;
        return mainCamera != null;
    }

    private bool TryBuildAimData(out AimData data)
    {
        data = default;
        Ray aimRay = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        bool aimHit = Physics.Raycast(aimRay, out RaycastHit aimHitInfo, range, hitMask, QueryTriggerInteraction.Ignore);
        Vector3 aimPoint = aimHit ? aimHitInfo.point : aimRay.origin + aimRay.direction * range;

        Vector3 fireOrigin = shootTransform != null ? shootTransform.position : aimRay.origin;
        Vector3 fireDirection = (aimPoint - fireOrigin).normalized;
        Ray fireRay = new Ray(fireOrigin, fireDirection);

        data = new AimData
        {
            AimRay = aimRay,
            FireRay = fireRay,
            AimPoint = aimPoint,
            FireOrigin = fireOrigin,
            FireDirection = fireDirection
        };
        return true;
    }

    private bool TryResolveHit(AimData data, out RaycastHit hitInfo)
    {
        return Physics.Raycast(data.FireRay, out hitInfo, range, hitMask, QueryTriggerInteraction.Ignore);
    }

    private void HandleHit(RaycastHit hit)
    {
        var target = hit.collider.GetComponentInParent<PlayerElimination>();
        if (target == null)
        {
            var npc = hit.collider.GetComponentInParent<BaseNPC>();
            if (npc != null)
            {
                var npcController = npc.GetComponent<NPCController>();
                if (npcController != null)
                {
                    npcController.RpcTriggerDead();
                }
                RpcSpawnFogEffect(npc.transform.position, npc.transform.rotation);
            }
            return;
        }

        target.RpcRequestPlayDeadAnimation();
        target.RpcRequestEliminate();
        target.ApplyEliminatedStateImmediate();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RpcRequestFire(Vector3 fireOrigin, Vector3 fireDirection)
    {
        RpcPlayMuzzleVfx(fireDirection);

        var fireRay = new Ray(fireOrigin, fireDirection);
        if (Physics.Raycast(fireRay, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore) == false)
        {
            return;
        }

        RpcPlayHitSfx(hit.point);
        RpcSpawnHitEffect(hit.point, hit.normal);
        HandleHit(hit);
    }

    private struct AimData
    {
        public Ray AimRay;
        public Ray FireRay;
        public Vector3 AimPoint;
        public Vector3 FireOrigin;
        public Vector3 FireDirection;
    }
}

internal sealed class StunGunFx
{
    private readonly GameObject hitEffectPrefab;
    private readonly float hitEffectLifetime;
    private readonly Transform shootTransform;
    private UnityEngine.VFX.VisualEffect shootVfx;
    private readonly GameObject fogEffectPrefab;
    private readonly AudioCueEventChannelSO sfxEventChannel;
    private readonly AudioConfigurationSO sfxConfiguration;
    private readonly AudioCueSO shootSfxCue;
    private readonly AudioCueSO hitSfxCue;

    public StunGunFx(
        GameObject hitEffectPrefab,
        float hitEffectLifetime,
        Transform shootTransform,
        UnityEngine.VFX.VisualEffect shootVfx,
        GameObject fogEffectPrefab,
        AudioCueEventChannelSO sfxEventChannel,
        AudioConfigurationSO sfxConfiguration,
        AudioCueSO shootSfxCue,
        AudioCueSO hitSfxCue)
    {
        this.hitEffectPrefab = hitEffectPrefab;
        this.hitEffectLifetime = hitEffectLifetime;
        this.shootTransform = shootTransform;
        this.shootVfx = shootVfx;
        this.fogEffectPrefab = fogEffectPrefab;
        this.sfxEventChannel = sfxEventChannel;
        this.sfxConfiguration = sfxConfiguration;
        this.shootSfxCue = shootSfxCue;
        this.hitSfxCue = hitSfxCue;
    }

    public void PlayShootSfx(Vector3 position)
    {
        PlaySfx(shootSfxCue, position);
    }

    public void PlayHitSfx(Vector3 position)
    {
        PlaySfx(hitSfxCue, position);
    }

    public void PlayMuzzleVfx(Vector3 shootDirection)
    {
        if (shootVfx == null && shootTransform != null)
        {
            shootVfx = shootTransform.GetComponent<UnityEngine.VFX.VisualEffect>();
        }

        if (shootVfx == null || shootTransform == null)
        {
            return;
        }

        shootVfx.SetVector3("MuzzlePosition", shootTransform.position);
        shootVfx.SetVector3("ShootDirection", shootDirection);
        shootVfx.Play();
    }

    public void SpawnFogEffect(Vector3 position, Quaternion rotation)
    {
        if (fogEffectPrefab == null)
        {
            return;
        }

        Object.Instantiate(fogEffectPrefab, position, rotation);
    }

    public void SpawnHitEffect(Vector3 position, Vector3 normal)
    {
        if (hitEffectPrefab == null)
        {
            return;
        }

        var rotation = normal.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(normal) : Quaternion.identity;
        var effect = Object.Instantiate(hitEffectPrefab, position, rotation);
        if (hitEffectLifetime > 0f)
        {
            Object.Destroy(effect, hitEffectLifetime);
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
}

internal sealed class StunGunCooldownUI
{
    private readonly MonoBehaviour owner;
    private readonly Image cooldownImage;
    private readonly float cooldownSeconds;

    public StunGunCooldownUI(MonoBehaviour owner, Image cooldownImage, float cooldownSeconds)
    {
        this.owner = owner;
        this.cooldownImage = cooldownImage;
        this.cooldownSeconds = cooldownSeconds;
    }

    public void StartCooldown()
    {
        if (owner == null || cooldownImage == null)
        {
            return;
        }

        owner.StartCoroutine(CooldownRoutine());
    }

    private System.Collections.IEnumerator CooldownRoutine()
    {
        cooldownImage.fillAmount = 0f;
        float timer = 0f;

        while (timer < cooldownSeconds)
        {
            timer += Time.deltaTime;
            cooldownImage.fillAmount = timer / cooldownSeconds;
            yield return null;
        }

        cooldownImage.fillAmount = 1f;
    }
}
