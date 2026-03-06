using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class DeathmatchOutsideZoneScreenEffect : MonoBehaviour
{
    [SerializeField] private Color outsideColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private float fadeSpeed = 6f;
    [SerializeField] private bool enableDebugLogs = false;

    private DeathmatchMatchController controller;
    private Canvas canvas;
    private Image overlay;
    private float currentAlpha;

    public void Bind(DeathmatchMatchController matchController)
    {
        controller = matchController;
    }

    private void LateUpdate()
    {
        if (controller == null || controller.IsEnabled == false)
        {
            SetOverlayAlpha(0f);
            return;
        }

        EnsureOverlay();

        float target = IsLocalPlayerOutsideZone() ? outsideColor.a : 0f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, target, Time.unscaledDeltaTime * Mathf.Max(0.1f, fadeSpeed));
        SetOverlayAlpha(currentAlpha);
    }

    private bool IsLocalPlayerOutsideZone()
    {
        NetworkRunner runner = controller.Runner;
        if (runner == null || runner.IsRunning == false)
        {
            return false;
        }

        if (runner.TryGetPlayerObject(runner.LocalPlayer, out var localObject) == false || localObject == null)
        {
            return false;
        }

        Vector3 pos = localObject.transform.position;
        Vector3 center = controller.SafeZoneCenter;
        float radius = Mathf.Max(0f, controller.NetSafeZoneRadius);
        float sqr = (new Vector2(pos.x - center.x, pos.z - center.z)).sqrMagnitude;
        bool outside = sqr > radius * radius;

        if (enableDebugLogs && outside)
        {
            Debug.Log($"[Deathmatch] Outside zone fx on. dist={Mathf.Sqrt(sqr):F2}, radius={radius:F2}");
        }

        return outside;
    }

    private void EnsureOverlay()
    {
        if (overlay != null)
        {
            return;
        }

        var canvasGo = new GameObject("DM_OutsideZoneFxCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var imageGo = new GameObject("DM_OutsideZoneOverlay");
        imageGo.transform.SetParent(canvasGo.transform, false);
        overlay = imageGo.AddComponent<Image>();
        overlay.raycastTarget = false;

        var rect = overlay.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        currentAlpha = 0f;
        SetOverlayAlpha(0f);
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (overlay == null)
        {
            return;
        }

        Color c = outsideColor;
        c.a = Mathf.Clamp01(alpha);
        overlay.color = c;
    }
}
