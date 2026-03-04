using UnityEngine;

public class DeathmatchSafeZoneVisualizer : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private Color zoneFillColor = new Color(0.12f, 0.7f, 1f, 0.12f);
    [SerializeField] private Color zoneBoundaryColor = new Color(0.2f, 0.85f, 1f, 0.95f);
    [SerializeField] private float groundYOffset = 0.03f;
    [SerializeField] private float boundaryYOffset = 0.2f;
    [SerializeField] private float boundaryWidth = 0.2f;
    [SerializeField] private int boundarySegments = 96;

    private DeathmatchMatchController controller;
    private Transform zoneFill;
    private LineRenderer boundaryLine;
    private Material fillMaterial;
    private Material lineMaterial;

    private void LateUpdate()
    {
        if (controller == null || controller.IsEnabled == false)
        {
            SetVisualActive(false);
            return;
        }

        EnsureVisualObjects();
        SetVisualActive(true);

        Vector3 center = controller.SafeZoneCenter;
        float radius = Mathf.Max(0f, controller.NetSafeZoneRadius);
        UpdateFill(center, radius);
        UpdateBoundary(center, radius);
    }

    public void Bind(DeathmatchMatchController matchController)
    {
        controller = matchController;
    }

    private void EnsureVisualObjects()
    {
        if (zoneFill == null)
        {
            var fillGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fillGo.name = "DM_SafeZoneFill";
            fillGo.transform.SetParent(transform, false);
            var fillCol = fillGo.GetComponent<Collider>();
            if (fillCol != null)
            {
                Destroy(fillCol);
            }

            var renderer = fillGo.GetComponent<MeshRenderer>();
            fillMaterial = CreateMaterial(zoneFillColor);
            renderer.sharedMaterial = fillMaterial;
            zoneFill = fillGo.transform;
        }

        if (boundaryLine == null)
        {
            var lineGo = new GameObject("DM_SafeZoneBoundary");
            lineGo.transform.SetParent(transform, false);
            boundaryLine = lineGo.AddComponent<LineRenderer>();
            boundaryLine.loop = true;
            boundaryLine.useWorldSpace = true;
            boundaryLine.positionCount = Mathf.Max(24, boundarySegments);
            boundaryLine.widthMultiplier = Mathf.Max(0.01f, boundaryWidth);
            boundaryLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            boundaryLine.receiveShadows = false;
            boundaryLine.alignment = LineAlignment.View;
            lineMaterial = CreateMaterial(zoneBoundaryColor);
            boundaryLine.sharedMaterial = lineMaterial;
            boundaryLine.startColor = zoneBoundaryColor;
            boundaryLine.endColor = zoneBoundaryColor;
        }
    }

    private void UpdateFill(Vector3 center, float radius)
    {
        if (zoneFill == null)
        {
            return;
        }

        zoneFill.position = center + Vector3.up * groundYOffset;
        zoneFill.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
    }

    private void UpdateBoundary(Vector3 center, float radius)
    {
        if (boundaryLine == null)
        {
            return;
        }

        int count = boundaryLine.positionCount;
        float y = center.y + boundaryYOffset;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float angle = t * Mathf.PI * 2f;
            float x = center.x + Mathf.Cos(angle) * radius;
            float z = center.z + Mathf.Sin(angle) * radius;
            boundaryLine.SetPosition(i, new Vector3(x, y, z));
        }
    }

    private void SetVisualActive(bool isActive)
    {
        if (zoneFill != null && zoneFill.gameObject.activeSelf != isActive)
        {
            zoneFill.gameObject.SetActive(isActive);
        }

        if (boundaryLine != null && boundaryLine.gameObject.activeSelf != isActive)
        {
            boundaryLine.gameObject.SetActive(isActive);
        }
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        var mat = new Material(shader);
        mat.color = color;
        return mat;
    }

    private void OnDestroy()
    {
        if (fillMaterial != null)
        {
            Destroy(fillMaterial);
        }

        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }
    }
}
