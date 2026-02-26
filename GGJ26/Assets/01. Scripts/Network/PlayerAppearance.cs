using UnityEngine;
using System.Threading.Tasks;

public class PlayerAppearance : MonoBehaviour
{
    [SerializeField] private PlayerRole role;
    [SerializeField] private StunGun stunGun;

    [Header("Visual Roots (optional)")]
    [SerializeField] private GameObject seekerVisualRoot;
    [SerializeField] private GameObject normalVisualRoot;
    [SerializeField] private GameObject[] seekerMaskObjects = new GameObject[0];

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
    private bool previewMode;
    private int previewSeekerSkinIndex;

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
        AutoAssignSeekerMaskObjects();
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
        if (previewMode)
        {
            ApplyRoleVisual(true, previewSeekerSkinIndex);
            return;
        }

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

    public void SetPreviewMode(bool enabled)
    {
        previewMode = enabled;

        if (enabled == false)
        {
            if (seekerVisualRoot != null)
            {
                seekerVisualRoot.SetActive(false);
            }

            if (normalVisualRoot != null)
            {
                normalVisualRoot.SetActive(false);
            }
        }
    }

    public void SetPreviewSeekerSkinIndex(int index)
    {
        previewSeekerSkinIndex = index;

        if (previewMode)
        {
            ApplyRoleVisual(true, previewSeekerSkinIndex);
        }
    }

    public int GetSeekerMaskCount()
    {
        return seekerMaskObjects != null ? seekerMaskObjects.Length : 0;
    }

    private void ApplyRoleVisual(bool isSeeker, int seekerSkinIndex)
    {
        if (seekerVisualRoot != null)
        {
            seekerVisualRoot.SetActive(isSeeker);
        }

        if (normalVisualRoot != null)
        {
            normalVisualRoot.SetActive(!isSeeker);
        }
        
        ApplySeekerMaskVisual(isSeeker, seekerSkinIndex);
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

    private void ApplySeekerMaskVisual(bool isSeeker, int seekerSkinIndex)
    {
        if (seekerMaskObjects == null || seekerMaskObjects.Length == 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(seekerSkinIndex, 0, seekerMaskObjects.Length - 1);
        for (int i = 0; i < seekerMaskObjects.Length; i++)
        {
            if (seekerMaskObjects[i] == null)
            {
                continue;
            }

            seekerMaskObjects[i].SetActive(isSeeker && i == clampedIndex);
        }
    }

    private void AutoAssignSeekerMaskObjects()
    {
        if (seekerMaskObjects != null && seekerMaskObjects.Length >= 2)
        {
            return;
        }

        var transforms = GetComponentsInChildren<Transform>(true);
        GameObject dinosaur = null;
        for (int i = 0; i < transforms.Length; i++)
        {
            var t = transforms[i];
            if (t == null)
            {
                continue;
            }

            if (string.Equals(t.gameObject.name, "Dinosaur", System.StringComparison.OrdinalIgnoreCase))
            {
                dinosaur = t.gameObject;
                break;
            }
        }

        if (dinosaur != null)
        {
            seekerMaskObjects = new[] { dinosaur, null };
        }
        else if (seekerMaskObjects == null || seekerMaskObjects.Length == 0)
        {
            seekerMaskObjects = new GameObject[2];
        }
    }
}
