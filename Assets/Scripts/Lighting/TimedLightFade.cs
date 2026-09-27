using UnityEngine;
using UnityEngine.Rendering.Universal; // Required for Light2D

// Attach to a GameObject with a Light2D (e.g. your explosion particle prefab,
// or a child of it). Automatically flashes to full intensity on spawn and
// fades out over fadeDuration - no manual trigger call needed, unlike
// MuzzleFlash which is fired on demand from the gun script.

[RequireComponent(typeof(Light2D))]
public class TimedLightFade : MonoBehaviour
{
    [Header("Intensity")]
    public float startIntensity = 5f;
    public float endIntensity = 0f;

    [Header("Outer Radius (shrinks alongside intensity for a more natural falloff)")]
    public bool fadeRadius = true;
    public float startOuterRadius = 3f;
    public float endOuterRadius = 0.5f;

    public float fadeDuration = 0.25f;

    private Light2D light2D;
    private float elapsed = 0f;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
    }

    void OnEnable()
    {
        elapsed = 0f;
        light2D.intensity = startIntensity;
        if (fadeRadius)
        {
            light2D.pointLightOuterRadius = startOuterRadius;
        }
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / fadeDuration);

        // Ease-out curve (t^2 inverted) instead of a linear fade. With Bloom
        // enabled, a linear fade spends most of its time still above the
        // bloom threshold (looks fully blown-out), then drops off suddenly
        // right at the end. This front-loads the falloff so it reads as a
        // smooth fade instead of "stuck bright, then sudden cutoff."
        float eased = 1f - (1f - t) * (1f - t);

        light2D.intensity = Mathf.Lerp(startIntensity, endIntensity, eased);

        if (fadeRadius)
        {
            light2D.pointLightOuterRadius = Mathf.Lerp(startOuterRadius, endOuterRadius, eased);
        }
    }
}
