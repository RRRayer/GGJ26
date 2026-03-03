using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

[RequireComponent(typeof(PlayerElimination))]
public class SpectatorSabotageController : NetworkBehaviour
{
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool unlimitedUsesForTesting = false;

    [Header("General")]
    [SerializeField] private LayerMask aimLayerMask = -1;
    [SerializeField] private float aimDistance = 60f;
    [SerializeField] private float smokeSpawnForwardOffset = 5f;

    [Header("Shoe Toss")]
    [SerializeField] private Object shoeProjectilePrefab;
    [SerializeField] private float shoeStunSeconds = 0.5f;
    [SerializeField] private float shoeKnockbackSpeed = 2.4f;
    [SerializeField] private float shoeProjectileSpeed = 18f;
    [SerializeField] private float shoeProjectileLifetime = 1.2f;
    [SerializeField] private float shoeProjectileGravity = 9.8f;
    [SerializeField] private float shoeSpawnForwardOffset = 0.8f;
    [SerializeField] private float shoeHitRadius = 0.45f;
    [SerializeField] private float shoeHitDistance = 22f;

    [Header("Ghost Smoke")]
    [SerializeField] private Object ghostSmokePrefab;
    [SerializeField] private string ghostSmokeTemplateObjectName = "SabotageGhostSmokeTemplate";
    [SerializeField] private float ghostSmokeLifetime = 10f;
    [SerializeField] private float ghostSmokeFadeDuration = 2f;

    [Header("Phantom Dance")]
    [SerializeField] private float phantomDanceDuration = 2f;

    [Header("Spectator Aim UI")]
    [SerializeField] private bool showSpectatorCrosshair = true;
    [SerializeField] private Color crosshairIdleColor = Color.white;
    [SerializeField] private Color crosshairArmedColor = new Color(1f, 0.95f, 0.4f, 1f);
    [SerializeField] private float crosshairSize = 18f;
    [SerializeField] private float crosshairThickness = 2f;
    [SerializeField] private string spectatorCrosshairObjectName = "SpectatorSabotageCrosshair";
    [SerializeField] private GameObject spectatorCrosshairRoot;

    private PlayerElimination elimination;
    private SpectatorSabotageState sabotageState = SpectatorSabotageState.CreateDefault();
    private Transform spectatorAimOrigin;
    private Image[] crosshairImages = System.Array.Empty<Image>();
    private bool crosshairVisible;
    private Color currentCrosshairColor;

    public SabotageType ArmedType => sabotageState.ArmedType;
    public bool CanUseShoe => sabotageState.CanUse(SabotageType.ShoeToss);
    public bool CanUseSmoke => sabotageState.CanUse(SabotageType.GhostSmoke);
    public bool CanUseDance => sabotageState.CanUse(SabotageType.PhantomDance);

    private void Awake()
    {
        elimination = GetComponent<PlayerElimination>();
        TryAutoAssignGhostSmokePrefab();
        ResolveCrosshairReferences();
    }

    public override void Spawned()
    {
        sabotageState = SpectatorSabotageState.CreateDefault();
        if (enableDebugLogs)
        {
            Debug.Log($"[Sabotage] Spawned on {name}. hasInputAuth={Object != null && Object.HasInputAuthority}", this);
        }
    }

    private void Update()
    {
        if (Object == null || Object.HasInputAuthority == false)
        {
            SetCrosshairVisible(false);
            return;
        }

        bool visible = showSpectatorCrosshair && IsSpectator();
        SetCrosshairVisible(visible);
        crosshairVisible = visible;
        currentCrosshairColor = sabotageState.ArmedType == SabotageType.None ? crosshairIdleColor : crosshairArmedColor;
        if (visible)
        {
            Color targetColor = currentCrosshairColor;
            for (int i = 0; i < crosshairImages.Length; i++)
            {
                if (crosshairImages[i] != null)
                {
                    crosshairImages[i].color = targetColor;
                }
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || Object.HasInputAuthority == false)
        {
            return;
        }

        if (GetInput(out PlayerInputData input) == false)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[Sabotage] GetInput failed this tick.", this);
            }
            return;
        }

        if (IsSpectator() == false)
        {
            sabotageState.ArmedType = SabotageType.None;
            if (enableDebugLogs && (input.sabotageArm1 || input.sabotageArm2 || input.sabotageArm3 || input.sabotageExecute))
            {
                Debug.Log("[Sabotage] Input ignored because local player is not spectator.", this);
            }
            return;
        }

        HandleArmInput(input);

        if (input.sabotageExecute)
        {
            TryExecuteArmedSabotage();
        }
    }

    private void HandleArmInput(PlayerInputData input)
    {
        if (input.sabotageArm1) ToggleArm(SabotageType.ShoeToss);
        if (input.sabotageArm2) ToggleArm(SabotageType.GhostSmoke);
        if (input.sabotageArm3) ToggleArm(SabotageType.PhantomDance);
    }

    private void ToggleArm(SabotageType type)
    {
        if (sabotageState.CanUse(type) == false)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[Sabotage] Cannot arm {type}: already consumed.", this);
            }
            sabotageState.ArmedType = SabotageType.None;
            return;
        }

        if (sabotageState.ArmedType == type)
        {
            sabotageState.ArmedType = SabotageType.None;
            if (enableDebugLogs)
            {
                Debug.Log($"[Sabotage] Unarmed {type}.", this);
            }
            return;
        }

        sabotageState.ArmedType = type;
        if (enableDebugLogs)
        {
            Debug.Log($"[Sabotage] Armed {type}.", this);
        }
    }

    private void TryExecuteArmedSabotage()
    {
        var armed = sabotageState.ArmedType;
        if (armed == SabotageType.None || sabotageState.CanUse(armed) == false)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[Sabotage] Execute ignored. armed={armed}, canUse={sabotageState.CanUse(armed)}", this);
            }
            return;
        }

        if (TryBuildAimRay(out Ray ray) == false)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[Sabotage] Execute failed: no valid spectator aim origin.", this);
            }
            return;
        }

        if (enableDebugLogs)
        {
            Debug.Log($"[Sabotage] Execute request. type={armed}, origin={ray.origin}, dir={ray.direction}", this);
        }

        if (Object.HasStateAuthority)
        {
            ExecuteOnStateAuthority(armed, ray);
        }
        else
        {
            RpcRequestExecuteSabotage((int)armed, ray.origin, ray.direction);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RpcRequestExecuteSabotage(int rawType, Vector3 rayOrigin, Vector3 rayDirection)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[Sabotage] RpcRequestExecuteSabotage received on state authority. type={(SabotageType)rawType}", this);
        }
        ExecuteOnStateAuthority((SabotageType)rawType, new Ray(rayOrigin, rayDirection));
    }

    private void ExecuteOnStateAuthority(SabotageType type, Ray ray)
    {
        if (IsSpectator() == false || sabotageState.CanUse(type) == false)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[Sabotage] Rejected on authority. type={type}, isSpectator={IsSpectator()}, canUse={sabotageState.CanUse(type)}", this);
            }
            return;
        }

        bool success = type switch
        {
            SabotageType.ShoeToss => TryExecuteShoeToss(ray),
            SabotageType.GhostSmoke => TryExecuteGhostSmoke(ray),
            SabotageType.PhantomDance => TryExecutePhantomDance(ray),
            _ => false
        };

        if (success == false)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[Sabotage] Execution failed. type={type}", this);
            }
            return;
        }

        if (unlimitedUsesForTesting == false)
        {
            sabotageState.Consume(type);
        }
        sabotageState.ArmedType = SabotageType.None;
        if (enableDebugLogs)
        {
            string consumeText = unlimitedUsesForTesting ? "reusable" : "consumed";
            Debug.Log($"[Sabotage] Execution success ({consumeText}). type={type}", this);
        }
    }

    private bool TryExecuteShoeToss(Ray ray)
    {
        var origin = GetSpectatorAimOrigin();
        if (origin == null)
        {
            return false;
        }

        Vector3 spawnPosition = origin.position + origin.forward * Mathf.Max(0f, shoeSpawnForwardOffset);
        Quaternion rotation = Quaternion.LookRotation(ray.direction.normalized, Vector3.up);
        RpcSpawnShoeProjectile(spawnPosition, rotation, ray.direction.normalized, shoeProjectileSpeed, shoeProjectileLifetime);

        if (TryFindSeekerHitBySphereCast(ray.origin, ray.direction, shoeHitDistance, out RaycastHit hit) == false)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[Sabotage] ShoeToss fired: no seeker hit.", this);
            }
            return true;
        }

        var role = hit.collider.GetComponentInParent<PlayerRole>();
        if (role == null || role.IsSeeker == false)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[Sabotage] ShoeToss fired: target is not seeker.", this);
            }
            return true;
        }

        var motor = role.GetComponent<FusionThirdPersonMotor>();
        if (motor == null)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[Sabotage] ShoeToss rejected: seeker motor missing.", this);
            }
            return true;
        }

        Vector3 direction = (role.transform.position - transform.position);
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = role.transform.forward;
        }

        motor.RequestSabotageStun(shoeStunSeconds, direction.normalized, shoeKnockbackSpeed);
        if (enableDebugLogs)
        {
            Debug.Log($"[Sabotage] ShoeToss success: seeker={role.name}", this);
        }
        return true;
    }

    private bool TryExecuteGhostSmoke(Ray ray)
    {
        var origin = GetSpectatorAimOrigin();
        if (origin == null)
        {
            return false;
        }

        Vector3 spawn = origin.position + origin.forward * Mathf.Max(0f, smokeSpawnForwardOffset);
        if (Physics.Raycast(ray, out RaycastHit hit, aimDistance, aimLayerMask, QueryTriggerInteraction.Ignore))
        {
            spawn = hit.point + Vector3.up * 0.1f;
        }

        Quaternion rotation = Quaternion.LookRotation(origin.forward, Vector3.up);
        RpcSpawnGhostSmoke(spawn, rotation);
        if (enableDebugLogs)
        {
            Debug.Log($"[Sabotage] GhostSmoke success at {spawn}", this);
        }
        return true;
    }

    private bool TryExecutePhantomDance(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, aimDistance, aimLayerMask, QueryTriggerInteraction.Ignore) == false)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[Sabotage] PhantomDance miss: no hit.", this);
            }
            return false;
        }

        var npc = hit.collider.GetComponentInParent<NPCController>();
        if (npc == null || npc.Object == null || npc.IsDead)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[Sabotage] PhantomDance rejected: target invalid/dead.", this);
            }
            return false;
        }

        RpcPlayPhantomDance(npc.Object.Id, Random.Range(0, 5), phantomDanceDuration);
        if (enableDebugLogs)
        {
            Debug.Log($"[Sabotage] PhantomDance success: npc={npc.name}", this);
        }
        return true;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcSpawnGhostSmoke(Vector3 position, Quaternion rotation)
    {
        if (TryResolveSmokePrefab(out GameObject smokePrefab) == false || smokePrefab == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[Sabotage] GhostSmoke spawn failed: invalid prefab reference.", this);
            }
            return;
        }

        var spawned = Instantiate((Object)smokePrefab, position, rotation);
        var instance = spawned as GameObject;
        if (instance == null)
        {
            if (enableDebugLogs)
            {
                string typeName = spawned != null ? spawned.GetType().Name : "null";
                Debug.LogWarning($"[Sabotage] GhostSmoke spawn failed: instantiated type {typeName} is not GameObject.", this);
            }
            return;
        }
        instance.SetActive(true);

        var visualEffect = instance.GetComponent<VisualEffect>();
        if (visualEffect != null)
        {
            visualEffect.Reinit();
            visualEffect.Play();
        }

        var fade = instance.GetComponent<SabotageFogAutoFade>();
        if (fade == null)
        {
            fade = instance.AddComponent<SabotageFogAutoFade>();
        }

        fade.Initialize(ghostSmokeLifetime, ghostSmokeFadeDuration);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcSpawnShoeProjectile(Vector3 position, Quaternion rotation, Vector3 direction, float speed, float lifetime)
    {
        if (TryResolveShoeProjectilePrefab(out GameObject prefab) == false || prefab == null)
        {
            return;
        }

        var spawned = Instantiate((Object)prefab, position, rotation) as GameObject;
        if (spawned == null)
        {
            return;
        }

        spawned.SetActive(true);
        StartCoroutine(MoveShoeProjectile(spawned.transform, direction.normalized, speed, lifetime));
    }

    private bool TryResolveSmokePrefab(out GameObject prefab)
    {
        prefab = null;
        if (ghostSmokePrefab == null)
        {
            TryAutoAssignGhostSmokePrefab();
            if (ghostSmokePrefab == null)
            {
                return false;
            }
        }

        if (ghostSmokePrefab is GameObject asGameObject)
        {
            prefab = asGameObject;
            return true;
        }

        if (ghostSmokePrefab is Component asComponent)
        {
            prefab = asComponent.gameObject;
            return prefab != null;
        }

        return false;
    }

    private bool TryResolveShoeProjectilePrefab(out GameObject prefab)
    {
        prefab = null;
        if (shoeProjectilePrefab == null)
        {
            return false;
        }

        if (shoeProjectilePrefab is GameObject asGameObject)
        {
            prefab = asGameObject;
            return true;
        }

        if (shoeProjectilePrefab is Component asComponent)
        {
            prefab = asComponent.gameObject;
            return prefab != null;
        }

        return false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcPlayPhantomDance(NetworkId npcId, int danceIndex, float duration)
    {
        if (Runner == null || Runner.TryFindObject(npcId, out var npcObject) == false || npcObject == null)
        {
            return;
        }

        var npc = npcObject.GetComponent<NPCController>();
        if (npc == null || npc.IsDead)
        {
            return;
        }

        npc.StartDance(Mathf.Clamp(danceIndex, 0, 4));
        StartCoroutine(StopNpcDanceAfter(npc, duration));
    }

    private IEnumerator StopNpcDanceAfter(NPCController npc, float duration)
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, duration));
        if (npc != null && npc.IsDead == false)
        {
            if (DanceEventPublisher.IsGroupDanceActive)
            {
                if (enableDebugLogs)
                {
                    Debug.Log("[Sabotage] PhantomDance stop skipped due to active group dance.", this);
                }
                yield break;
            }

            npc.StopDance();
        }
    }

    private bool TryBuildAimRay(out Ray ray)
    {
        ray = default;
        var origin = GetSpectatorAimOrigin();
        if (origin == null)
        {
            return false;
        }

        ray = new Ray(origin.position, origin.forward);
        return true;
    }

    private bool IsSpectator()
    {
        return elimination != null && elimination.IsEliminated;
    }

    public void SetSpectatorAimOrigin(Transform origin)
    {
        spectatorAimOrigin = origin;
        if (enableDebugLogs)
        {
            string originName = origin != null ? origin.name : "null";
            Debug.Log($"[Sabotage] Aim origin set: {originName}", this);
        }
    }

    private Transform GetSpectatorAimOrigin()
    {
        if (spectatorAimOrigin != null)
        {
            return spectatorAimOrigin;
        }

        var spectator = FindFirstObjectByType<SpectatorController>();
        if (spectator != null && spectator.isActiveAndEnabled)
        {
            spectatorAimOrigin = spectator.transform;
            return spectatorAimOrigin;
        }

        if (Camera.main != null)
        {
            return Camera.main.transform;
        }

        return null;
    }

    private void SetCrosshairVisible(bool visible)
    {
        if (spectatorCrosshairRoot != null)
        {
            spectatorCrosshairRoot.SetActive(visible);
        }
    }

    private void OnGUI()
    {
        if (crosshairVisible == false || Event.current.type != EventType.Repaint)
        {
            return;
        }

        DrawCrosshair(currentCrosshairColor, crosshairSize, crosshairThickness);
    }

    private void ResolveCrosshairReferences()
    {
        if (spectatorCrosshairRoot == null && string.IsNullOrWhiteSpace(spectatorCrosshairObjectName) == false)
        {
            var found = GameObject.Find(spectatorCrosshairObjectName);
            if (found == null)
            {
                found = FindInactiveSceneObjectByName(spectatorCrosshairObjectName);
            }
            if (found != null)
            {
                spectatorCrosshairRoot = found;
            }
        }

        if (spectatorCrosshairRoot != null)
        {
            crosshairImages = spectatorCrosshairRoot.GetComponentsInChildren<Image>(true);
        }
    }

    private void TryAutoAssignGhostSmokePrefab()
    {
        if (ghostSmokePrefab != null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(ghostSmokeTemplateObjectName) == false)
        {
            var template = FindInactiveSceneObjectByName(ghostSmokeTemplateObjectName);
            if (template != null)
            {
                ghostSmokePrefab = template;
                if (enableDebugLogs)
                {
                    Debug.Log($"[Sabotage] Auto-assigned ghost smoke template: {template.name}", this);
                }
            }
        }
    }

    private static GameObject FindInactiveSceneObjectByName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform tr = allTransforms[i];
            if (tr == null || tr.name != objectName)
            {
                continue;
            }

            var go = tr.gameObject;
            if (go.scene.IsValid() == false)
            {
                continue;
            }

            return go;
        }

        return null;
    }

    private IEnumerator MoveShoeProjectile(Transform projectile, Vector3 direction, float speed, float lifetime)
    {
        float remaining = Mathf.Max(0.05f, lifetime);
        Vector3 velocity = (direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward) * Mathf.Max(0.1f, speed);
        float gravity = Mathf.Max(0f, shoeProjectileGravity);
        bool canApplyHit = Object != null && Object.HasStateAuthority;

        while (projectile != null && remaining > 0f)
        {
            float dt = Time.deltaTime;
            Vector3 prevPos = projectile.position;
            velocity += Vector3.down * gravity * dt;
            projectile.position += velocity * dt;
            if (velocity.sqrMagnitude > 0.0001f)
            {
                projectile.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            }

            if (canApplyHit && TryApplyShoeHit(prevPos, projectile.position, velocity))
            {
                if (projectile != null)
                {
                    Destroy(projectile.gameObject);
                }
                yield break;
            }

            remaining -= dt;
            yield return null;
        }

        if (projectile != null)
        {
            Destroy(projectile.gameObject);
        }
    }

    private bool TryApplyShoeHit(Vector3 from, Vector3 to, Vector3 velocity)
    {
        Vector3 delta = to - from;
        float distance = delta.magnitude;
        if (distance <= 0.0001f)
        {
            return false;
        }

        Vector3 dir = delta / distance;
        if (TryFindSeekerHitBySphereCast(from, dir, distance, out RaycastHit hit) == false)
        {
            return false;
        }

        var role = hit.collider.GetComponentInParent<PlayerRole>();
        if (role == null || role.IsSeeker == false)
        {
            return false;
        }

        var motor = role.GetComponent<FusionThirdPersonMotor>();
        if (motor == null)
        {
            return false;
        }

        Vector3 knockbackDir = velocity.sqrMagnitude > 0.0001f ? velocity.normalized : dir;
        knockbackDir.y = 0f;
        if (knockbackDir.sqrMagnitude < 0.0001f)
        {
            knockbackDir = role.transform.forward;
        }

        motor.RequestSabotageStun(shoeStunSeconds, knockbackDir.normalized, shoeKnockbackSpeed);
        if (enableDebugLogs)
        {
            Debug.Log($"[Sabotage] Shoe projectile hit seeker: {role.name}", this);
        }
        return true;
    }

    private bool TryFindSeekerHitBySphereCast(Vector3 origin, Vector3 direction, float distance, out RaycastHit seekerHit)
    {
        seekerHit = default;
        if (distance <= 0f)
        {
            return false;
        }

        var hits = Physics.SphereCastAll(origin, shoeHitRadius, direction.normalized, distance, aimLayerMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null)
            {
                continue;
            }

            var role = hit.collider.GetComponentInParent<PlayerRole>();
            if (role == null || role.IsSeeker == false)
            {
                continue;
            }

            seekerHit = hit;
            return true;
        }

        return false;
    }

    private static Texture2D crosshairTex;
    private static void DrawCrosshair(Color color, float size, float thickness)
    {
        if (crosshairTex == null)
        {
            crosshairTex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            crosshairTex.SetPixel(0, 0, Color.white);
            crosshairTex.Apply();
        }

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;
        var prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(cx - size * 0.5f, cy - thickness * 0.5f, size, thickness), crosshairTex);
        GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy - size * 0.5f, thickness, size), crosshairTex);
        GUI.color = prev;
    }
}
