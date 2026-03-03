using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.VFX;

public class MaskEffect : MonoBehaviour
{
    [Header("Setup")]
    public VisualEffect flashVFX;

    [Header("Lifecycle")]
    public float selfDestructDelay = 10f;
    [SerializeField] private float stopVfxAfterSeconds = -1f;

    [Header("Firework")]
    [SerializeField] private string fireworkGradientPropertyName = "Color";

    private Gradient overrideGradient;
    private Color? overrideColor;

    public void SetFireworkGradient(Gradient gradient)
    {
        overrideGradient = gradient;
    }

    public void SetFireworkColor(Color color)
    {
        overrideColor = color;
    }

    public async Task PlayEffectSequence()
    {
        if (flashVFX != null)
        {
            string gradientProperty = ResolveGradientPropertyName();
            Color representativeColor = ResolveRepresentativeColor();

            if (overrideGradient != null)
            {
                if (flashVFX.HasGradient(gradientProperty) == false)
                {
                    Debug.LogWarning($"[MaskEffect] Gradient property not found on VFX: {gradientProperty}", this);
                }
                flashVFX.SetGradient(gradientProperty, overrideGradient);
            }

            flashVFX.SetVector4("Color", representativeColor);
            flashVFX.SetVector4("color", representativeColor);
            flashVFX.SetVector4("_Color", representativeColor);

            flashVFX.Reinit();

            if (overrideGradient != null)
            {
                flashVFX.SetGradient(gradientProperty, overrideGradient);
            }

            // Some VFX graphs read color from event attributes, not property sheets.
            // Send both gradient + event color to cover both authoring patterns.
            VFXEventAttribute eventAttribute = flashVFX.CreateVFXEventAttribute();
            eventAttribute.SetVector4("Color", representativeColor);
            eventAttribute.SetVector4("color", representativeColor);
            eventAttribute.SetVector4("_Color", representativeColor);
            flashVFX.SendEvent("OnPlay", eventAttribute);

            // Stop first so live particles can naturally fade by VFX graph lifetime.
            if (stopVfxAfterSeconds > 0f && stopVfxAfterSeconds < selfDestructDelay)
            {
                await Task.Delay(Mathf.RoundToInt(stopVfxAfterSeconds * 1000f));
                flashVFX.Stop();
            }
        }

        Destroy(gameObject, selfDestructDelay);
        await Task.Delay(Mathf.Max(100, Mathf.RoundToInt(selfDestructDelay * 1000f)));
    }

    private string ResolveGradientPropertyName()
    {
        if (flashVFX == null)
        {
            return fireworkGradientPropertyName;
        }

        if (flashVFX.HasGradient(fireworkGradientPropertyName))
        {
            return fireworkGradientPropertyName;
        }

        if (flashVFX.HasGradient("Color"))
        {
            return "Color";
        }

        if (flashVFX.HasGradient("color"))
        {
            return "color";
        }

        return fireworkGradientPropertyName;
    }

    private Color ResolveRepresentativeColor()
    {
        if (overrideColor.HasValue)
        {
            return overrideColor.Value;
        }

        if (overrideGradient != null)
        {
            return overrideGradient.Evaluate(0.5f);
        }

        return Color.white;
    }
}
