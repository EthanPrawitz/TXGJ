using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal; // Required for Light2D

public class MuzzleFlash : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Light2D flashLight2D; // Reference to Light2D component
    [SerializeField] private float maxIntensity = 3f;
    [SerializeField] private float fadeDuration = 0.05f;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        // Automatically grab Light2D if not assigned in Inspector
        if (flashLight2D == null)
            flashLight2D = GetComponent<Light2D>();

        if (flashLight2D != null)
            flashLight2D.intensity = 0f;
    }

    public void TriggerFlash()
    {
        if (flashLight2D == null)
        {
            Debug.LogError("flashLight2D is NULL on " + gameObject.name);
            return;
        }

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeLight());
    }

    private IEnumerator FadeLight()
    {
        flashLight2D.intensity = maxIntensity;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            flashLight2D.intensity = Mathf.Lerp(maxIntensity, 0f, elapsed / fadeDuration);
            yield return null;
        }

        flashLight2D.intensity = 0f;
    }
}
