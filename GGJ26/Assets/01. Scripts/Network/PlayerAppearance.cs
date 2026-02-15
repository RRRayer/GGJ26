using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;

public class PlayerAppearance : MonoBehaviour
{
    [SerializeField] private PlayerRole role;
    [SerializeField] private StunGun stunGun;

    [Header("Visual Roots (optional)")]
    [SerializeField] private GameObject seekerVisualRoot;
    [SerializeField] private GameObject[] seekerSkinRoots = new GameObject[0];
    [SerializeField] private Transform seekerSkinContainer;
    [SerializeField] private GameObject[] seekerSkinPrefabs = new GameObject[0];
    [SerializeField] private GameObject normalVisualRoot;

    [Header("Mask Objects (optional)")]
    [SerializeField] private GameObject[] maskObjects = new GameObject[3];

    [Header("Mask Materials (optional)")]
    [SerializeField] private Renderer maskRenderer;
    [SerializeField] private Material[] maskMaterials = new Material[3];
    
    [Header("연출 효과")]
    [SerializeField] private MaskEffect maskEffectPrefab;
    [SerializeField] private Transform effectSpawnPoint;

    [Header("이벤트 채널")]
    [SerializeField] private VoidEventChannelSO stopDiscoEvent; // 종료 '요청' 이벤트
    [SerializeField] private VoidEventChannelSO confirmStopDiscoEvent; // 종료 '확정' 이벤트

    [Header("Mask Change SFX (optional)")]
    [SerializeField] private AudioCueEventChannelSO sfxEventChannel;
    [SerializeField] private AudioConfigurationSO sfxConfiguration;
    [SerializeField] private AudioCueSO maskChangeSfxCue;

    private bool lastIsSeeker;
    private int lastMaskIndex = -2;
    private int lastSeekerSkinIndex = -1;
    private bool hasInitializedMask;
    private SkinnedMeshRenderer[] seekerSkinBoneTemplates = new SkinnedMeshRenderer[0];

    private void Awake()
    {
        if (role == null)
        {
            role = GetComponent<PlayerRole>();
        }

        if (stunGun == null)
        {
            stunGun = GetComponent<StunGun>();
        }

        AutoAssignMaskObjects();
        BuildSeekerSkinRootsFromPrefabs();
    }

    private void OnEnable()
    {
        if (stopDiscoEvent != null)
        {
            stopDiscoEvent.OnEventRaised += OnStopDiscoRequested;
        }
    }

    private void OnDisable()
    {
        if (stopDiscoEvent != null)
        {
            stopDiscoEvent.OnEventRaised -= OnStopDiscoRequested;
        }
    }

    private async void OnStopDiscoRequested()
    {
        if (maskEffectPrefab == null || effectSpawnPoint == null)
        {
            confirmStopDiscoEvent?.RaiseEvent();
            return;
        }

        // 연출용 마스크 생성 및 애니메이션 재생, 그리고 끝날 때까지 대기
        MaskEffect effectInstance = Instantiate(maskEffectPrefab, effectSpawnPoint.position, effectSpawnPoint.rotation, effectSpawnPoint);
        await effectInstance.PlayEffectSequence();
        
        // 연출이 끝났으므로, 디스코볼이 종료되어도 좋다는 '확정' 신호를 보냄
        confirmStopDiscoEvent?.RaiseEvent();
    }

    private void LateUpdate()
    {
        if (role == null || role.HasRoleAssigned() == false)
        {
            return;
        }

        bool isSeeker = role.IsSeeker;
        int maskIndex = role.GetMaskColorIndex();
        int seekerSkinIndex = role.GetSeekerSkinIndex();

        if (isSeeker != lastIsSeeker || seekerSkinIndex != lastSeekerSkinIndex)
        {
            ApplyRoleVisual(isSeeker, seekerSkinIndex);
            lastIsSeeker = isSeeker;
            lastSeekerSkinIndex = seekerSkinIndex;
        }

        if (maskIndex != lastMaskIndex)
        {
            ApplyMaskVisual(maskIndex);
            if (hasInitializedMask)
            {
                PlayMaskChangeSfx();
            }
            else
            {
                hasInitializedMask = true;
            }
            lastMaskIndex = maskIndex;
        }

        if (stunGun != null)
        {
            stunGun.enabled = isSeeker;
        }
    }

    private void ApplyRoleVisual(bool isSeeker, int seekerSkinIndex)
    {
        if (seekerSkinRoots != null && seekerSkinRoots.Length > 0)
        {
            for (int i = 0; i < seekerSkinRoots.Length; i++)
            {
                if (seekerSkinRoots[i] == null)
                {
                    continue;
                }

                seekerSkinRoots[i].SetActive(isSeeker && i == Mathf.Clamp(seekerSkinIndex, 0, seekerSkinRoots.Length - 1));
            }
        }
        else if (seekerVisualRoot != null)
        {
            seekerVisualRoot.SetActive(isSeeker);
        }

        if (normalVisualRoot != null)
        {
            normalVisualRoot.SetActive(!isSeeker);
        }
    }

    private void BuildSeekerSkinRootsFromPrefabs()
    {
        if (seekerSkinPrefabs == null || seekerSkinPrefabs.Length == 0)
        {
            return;
        }

        CacheSeekerSkinBoneTemplates();

        if (seekerSkinContainer == null)
        {
            seekerSkinContainer = transform.Find("Geometry");
            if (seekerSkinContainer == null)
            {
                seekerSkinContainer = transform;
            }
        }

        if (seekerSkinRoots != null)
        {
            for (int i = 0; i < seekerSkinRoots.Length; i++)
            {
                if (seekerSkinRoots[i] != null)
                {
                    seekerSkinRoots[i].SetActive(false);
                }
            }
        }

        var runtimeRoots = new List<GameObject>(seekerSkinPrefabs.Length);
        for (int i = 0; i < seekerSkinPrefabs.Length; i++)
        {
            GameObject skinPrefab = seekerSkinPrefabs[i];
            if (skinPrefab == null)
            {
                continue;
            }

            GameObject skinInstance = Instantiate(skinPrefab, seekerSkinContainer);
            skinInstance.name = skinPrefab.name;
            RebindSeekerSkinBones(skinInstance);
            skinInstance.SetActive(false);
            runtimeRoots.Add(skinInstance);
        }

        if (runtimeRoots.Count > 0)
        {
            seekerSkinRoots = runtimeRoots.ToArray();

            if (seekerVisualRoot != null)
            {
                seekerVisualRoot.SetActive(false);
            }
        }
    }

    private void CacheSeekerSkinBoneTemplates()
    {
        var templates = new List<SkinnedMeshRenderer>();

        if (seekerSkinRoots != null)
        {
            for (int i = 0; i < seekerSkinRoots.Length; i++)
            {
                if (seekerSkinRoots[i] == null)
                {
                    continue;
                }

                var renderers = seekerSkinRoots[i].GetComponentsInChildren<SkinnedMeshRenderer>(true);
                for (int j = 0; j < renderers.Length; j++)
                {
                    if (renderers[j] != null)
                    {
                        templates.Add(renderers[j]);
                    }
                }
            }
        }

        if (templates.Count == 0 && seekerVisualRoot != null)
        {
            var renderers = seekerVisualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    templates.Add(renderers[i]);
                }
            }
        }

        seekerSkinBoneTemplates = templates.ToArray();
    }

    private void RebindSeekerSkinBones(GameObject skinRoot)
    {
        if (skinRoot == null || seekerSkinBoneTemplates == null || seekerSkinBoneTemplates.Length == 0)
        {
            return;
        }

        var renderers = skinRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var target = renderers[i];
            if (target == null)
            {
                continue;
            }

            SkinnedMeshRenderer template = FindSeekerTemplateRenderer(target.name);
            if (template == null)
            {
                template = seekerSkinBoneTemplates[0];
            }

            target.rootBone = template.rootBone;
            target.bones = template.bones;
            target.localBounds = template.localBounds;
            target.updateWhenOffscreen = template.updateWhenOffscreen;
        }
    }

    private SkinnedMeshRenderer FindSeekerTemplateRenderer(string rendererName)
    {
        for (int i = 0; i < seekerSkinBoneTemplates.Length; i++)
        {
            var candidate = seekerSkinBoneTemplates[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.name == rendererName)
            {
                return candidate;
            }
        }

        return null;
    }

    private void ApplyMaskVisual(int maskIndex)
    {
        for (int i = 0; i < maskObjects.Length; i++)
        {
            if (maskObjects[i] != null)
            {
                maskObjects[i].SetActive(i == maskIndex);
            }
        }

        if (maskRenderer != null && maskMaterials != null && maskIndex >= 0 && maskIndex < maskMaterials.Length)
        {
            var material = maskMaterials[maskIndex];
            if (material != null)
            {
                maskRenderer.sharedMaterial = material;
            }
        }
    }

    private void PlayMaskChangeSfx()
    {
        if (sfxEventChannel == null || sfxConfiguration == null || maskChangeSfxCue == null)
        {
            return;
        }

        sfxEventChannel.RaisePlayEvent(maskChangeSfxCue, sfxConfiguration, transform.position);
    }

    private void AutoAssignMaskObjects()
    {
        if (maskObjects == null || maskObjects.Length < 3)
        {
            maskObjects = new GameObject[3];
        }

        bool allAssigned = true;
        for (int i = 0; i < 3; i++)
        {
            if (maskObjects[i] == null)
            {
                allAssigned = false;
                break;
            }
        }

        if (allAssigned)
        {
            return;
        }

        var transforms = GetComponentsInChildren<Transform>(true);
        foreach (var t in transforms)
        {
            if (t == null)
            {
                continue;
            }

            string name = t.gameObject.name;
            if (string.Equals(name, "Mask_Red", System.StringComparison.OrdinalIgnoreCase))
            {
                maskObjects[0] = t.gameObject;
            }
            else if (string.Equals(name, "Mask_Blue", System.StringComparison.OrdinalIgnoreCase))
            {
                maskObjects[1] = t.gameObject;
            }
            else if (string.Equals(name, "Mask_Green", System.StringComparison.OrdinalIgnoreCase))
            {
                maskObjects[2] = t.gameObject;
            }
        }
    }
}
