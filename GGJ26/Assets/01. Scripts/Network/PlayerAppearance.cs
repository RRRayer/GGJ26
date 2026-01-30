using UnityEngine;

public class PlayerAppearance : MonoBehaviour
{
    [SerializeField] private PlayerRole role;
    [SerializeField] private StunGun stunGun;

    [Header("Visual Roots (optional)")]
    [SerializeField] private GameObject seekerVisualRoot;
    [SerializeField] private GameObject normalVisualRoot;

    [Header("Mask Objects (optional)")]
    [SerializeField] private GameObject[] maskObjects = new GameObject[3];

    [Header("Mask Materials (optional)")]
    [SerializeField] private Renderer maskRenderer;
    [SerializeField] private Material[] maskMaterials = new Material[3];

    private bool lastIsSeeker;
    private int lastMaskIndex = -2;

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
    }

    private void LateUpdate()
    {
        if (role == null || role.HasRoleAssigned() == false)
        {
            return;
        }

        bool isSeeker = role.IsSeeker;
        int maskIndex = role.GetMaskColorIndex();

        if (isSeeker != lastIsSeeker)
        {
            ApplyRoleVisual(isSeeker);
            lastIsSeeker = isSeeker;
        }

        if (maskIndex != lastMaskIndex)
        {
            ApplyMaskVisual(maskIndex);
            lastMaskIndex = maskIndex;
        }

        if (stunGun != null)
        {
            stunGun.enabled = isSeeker;
        }
    }

    private void ApplyRoleVisual(bool isSeeker)
    {
        if (seekerVisualRoot != null)
        {
            seekerVisualRoot.SetActive(isSeeker);
        }

        if (normalVisualRoot != null)
        {
            normalVisualRoot.SetActive(!isSeeker);
        }
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
