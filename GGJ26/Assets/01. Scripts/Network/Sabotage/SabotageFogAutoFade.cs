using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SabotageFogAutoFade : MonoBehaviour
{
    private readonly List<Renderer> cachedRenderers = new List<Renderer>();
    private readonly List<MaterialPropertyBlock> propertyBlocks = new List<MaterialPropertyBlock>();
    private readonly List<float> initialAlphas = new List<float>();
    private float lifetime;
    private float fadeDuration;

    public void Initialize(float totalLifetime, float fadeOutDuration)
    {
        lifetime = Mathf.Max(0.1f, totalLifetime);
        fadeDuration = Mathf.Clamp(fadeOutDuration, 0f, lifetime);
        CacheRenderers();
        StartCoroutine(FadeRoutine());
    }

    private void CacheRenderers()
    {
        cachedRenderers.Clear();
        propertyBlocks.Clear();
        initialAlphas.Clear();

        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null || renderer.sharedMaterial == null)
            {
                continue;
            }

            cachedRenderers.Add(renderer);
            propertyBlocks.Add(new MaterialPropertyBlock());

            float alpha = 1f;
            var material = renderer.sharedMaterial;
            if (material.HasProperty("_BaseColor"))
            {
                alpha = material.GetColor("_BaseColor").a;
            }
            else if (material.HasProperty("_Color"))
            {
                alpha = material.GetColor("_Color").a;
            }

            initialAlphas.Add(alpha);
        }
    }

    private IEnumerator FadeRoutine()
    {
        if (fadeDuration < 0.01f)
        {
            yield return new WaitForSeconds(lifetime);
            Destroy(gameObject);
            yield break;
        }

        float wait = Mathf.Max(0f, lifetime - fadeDuration);
        if (wait > 0f)
        {
            yield return new WaitForSeconds(wait);
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            ApplyAlpha(1f - t);
            yield return null;
        }

        ApplyAlpha(0f);
        Destroy(gameObject);
    }

    private void ApplyAlpha(float alphaMultiplier)
    {
        for (int i = 0; i < cachedRenderers.Count; i++)
        {
            var renderer = cachedRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            var block = propertyBlocks[i];
            renderer.GetPropertyBlock(block);

            float alpha = initialAlphas[i] * alphaMultiplier;
            var material = renderer.sharedMaterial;
            if (material != null && material.HasProperty("_BaseColor"))
            {
                Color c = material.GetColor("_BaseColor");
                c.a = alpha;
                block.SetColor("_BaseColor", c);
            }

            if (material != null && material.HasProperty("_Color"))
            {
                Color c = material.GetColor("_Color");
                c.a = alpha;
                block.SetColor("_Color", c);
            }

            renderer.SetPropertyBlock(block);
        }
    }
}

