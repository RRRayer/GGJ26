using UnityEngine;

public class DeathmatchOutsideZoneWorldTint : MonoBehaviour
{
    [Header("Outside World Tint")]
    [SerializeField] private float outsideRadius = 120f;
    [SerializeField] private float groundYOffset = 0.04f;
    [SerializeField] private Color outsideTintColor = new Color(1f, 0f, 0f, 0.16f);
    [SerializeField] private Color boundaryColor = new Color(1f, 0.1f, 0.1f, 0.95f);
    [SerializeField] private float boundaryWidth = 0.22f;
    [SerializeField] private int segments = 128;

    private DeathmatchMatchController controller;
    private MeshFilter ringMeshFilter;
    private MeshRenderer ringRenderer;
    private LineRenderer boundaryLine;
    private Mesh ringMesh;
    private Material ringMaterial;
    private Material lineMaterial;

    public void Bind(DeathmatchMatchController matchController)
    {
        controller = matchController;
    }

    private void LateUpdate()
    {
        if (controller == null || controller.IsEnabled == false)
        {
            SetActive(false);
            return;
        }

        EnsureVisuals();
        SetActive(true);

        Vector3 center = controller.SafeZoneCenter;
        float innerRadius = Mathf.Max(0f, controller.NetSafeZoneRadius);
        float outerRadius = Mathf.Max(innerRadius + 0.1f, outsideRadius);

        UpdateRingMesh(center, innerRadius, outerRadius);
        UpdateBoundary(center, innerRadius);
    }

    private void EnsureVisuals()
    {
        if (ringMeshFilter == null || ringRenderer == null)
        {
            var ringGo = new GameObject("DM_OutsideZoneRing");
            ringGo.transform.SetParent(transform, false);
            ringMeshFilter = ringGo.AddComponent<MeshFilter>();
            ringRenderer = ringGo.AddComponent<MeshRenderer>();
            ringMesh = new Mesh { name = "DM_OutsideRingMesh" };
            ringMesh.MarkDynamic();
            ringMeshFilter.sharedMesh = ringMesh;

            ringMaterial = CreateMaterial(outsideTintColor);
            ringRenderer.sharedMaterial = ringMaterial;
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;
        }

        if (boundaryLine == null)
        {
            var lineGo = new GameObject("DM_ZoneBoundary");
            lineGo.transform.SetParent(transform, false);
            boundaryLine = lineGo.AddComponent<LineRenderer>();
            boundaryLine.loop = true;
            boundaryLine.useWorldSpace = true;
            boundaryLine.positionCount = Mathf.Max(24, segments);
            boundaryLine.widthMultiplier = Mathf.Max(0.02f, boundaryWidth);
            boundaryLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            boundaryLine.receiveShadows = false;
            boundaryLine.alignment = LineAlignment.View;
            boundaryLine.startColor = boundaryColor;
            boundaryLine.endColor = boundaryColor;
            lineMaterial = CreateMaterial(boundaryColor);
            boundaryLine.sharedMaterial = lineMaterial;
        }
    }

    private void UpdateRingMesh(Vector3 center, float innerRadius, float outerRadius)
    {
        int seg = Mathf.Max(24, segments);
        int vertCount = seg * 2;
        int triCount = seg * 6;

        var vertices = new Vector3[vertCount];
        var colors = new Color[vertCount];
        var triangles = new int[triCount];

        float y = center.y + groundYOffset;
        for (int i = 0; i < seg; i++)
        {
            float t = (float)i / seg;
            float angle = t * Mathf.PI * 2f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            int vi = i * 2;
            vertices[vi] = new Vector3(center.x + cos * innerRadius, y, center.z + sin * innerRadius);
            vertices[vi + 1] = new Vector3(center.x + cos * outerRadius, y, center.z + sin * outerRadius);
            colors[vi] = outsideTintColor;
            colors[vi + 1] = outsideTintColor;

            int ni = (i + 1) % seg;
            int vni = ni * 2;

            int ti = i * 6;
            triangles[ti] = vi;
            triangles[ti + 1] = vni;
            triangles[ti + 2] = vi + 1;
            triangles[ti + 3] = vi + 1;
            triangles[ti + 4] = vni;
            triangles[ti + 5] = vni + 1;
        }

        ringMesh.Clear();
        ringMesh.vertices = vertices;
        ringMesh.triangles = triangles;
        ringMesh.colors = colors;
        ringMesh.RecalculateNormals();
        ringMesh.RecalculateBounds();
    }

    private void UpdateBoundary(Vector3 center, float radius)
    {
        if (boundaryLine == null)
        {
            return;
        }

        int count = boundaryLine.positionCount;
        float y = center.y + groundYOffset + 0.06f;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float angle = t * Mathf.PI * 2f;
            float x = center.x + Mathf.Cos(angle) * radius;
            float z = center.z + Mathf.Sin(angle) * radius;
            boundaryLine.SetPosition(i, new Vector3(x, y, z));
        }
    }

    private void SetActive(bool active)
    {
        if (ringMeshFilter != null && ringMeshFilter.gameObject.activeSelf != active)
        {
            ringMeshFilter.gameObject.SetActive(active);
        }

        if (boundaryLine != null && boundaryLine.gameObject.activeSelf != active)
        {
            boundaryLine.gameObject.SetActive(active);
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
        if (ringMesh != null)
        {
            Destroy(ringMesh);
        }

        if (ringMaterial != null)
        {
            Destroy(ringMaterial);
        }

        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }
    }
}
