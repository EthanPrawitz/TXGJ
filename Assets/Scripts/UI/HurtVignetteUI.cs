using UnityEngine;
using UnityEngine.UI;

// Attach to a full-screen UI Image (stretched to fill the whole Canvas,
// placed above your other UI so it renders on top). Generates a radial
// vignette texture at runtime, so no external art asset is needed.
//
// Setup: Canvas -> create an Image, anchor/stretch it to fill the entire
// screen (anchor preset: stretch-stretch, all offsets 0), set Raycast
// Target to false (so it doesn't block clicks), then add this script.

[RequireComponent(typeof(Image))]
public class HurtVignetteUI : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;

    [Header("Look")]
    public Color vignetteColor = Color.red;
    public float maxAlpha = 0.5f;      // how visible the flash is at its peak
    public float fadeDuration = 0.4f;
    public int textureSize = 256;      // resolution of the generated gradient - 256 is plenty

    private Image image;
    private float currentAlpha = 0f;
    private float lastHealth = -1f;

    void Awake()
    {
        image = GetComponent<Image>();
        if (playerHealth == null) playerHealth = FindObjectOfType<PlayerHealth>();

        image.sprite = GenerateVignetteSprite();
        SetAlpha(0f);
    }

    void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += HandleHealthChanged;
        }
    }

    void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= HandleHealthChanged;
        }
    }

    void Start()
    {
        if (playerHealth != null)
        {
            lastHealth = playerHealth.currentHealth;
        }
    }

    void HandleHealthChanged(float current, float max)
    {
        // Only flash on damage (health going down), not on healing
        if (current < lastHealth)
        {
            currentAlpha = maxAlpha;
            SetAlpha(currentAlpha);
        }
        lastHealth = current;
    }

    void Update()
    {
        if (currentAlpha > 0f)
        {
            currentAlpha -= (maxAlpha / fadeDuration) * Time.deltaTime;
            currentAlpha = Mathf.Max(currentAlpha, 0f);
            SetAlpha(currentAlpha);
        }
    }

    void SetAlpha(float alpha)
    {
        Color c = vignetteColor;
        c.a = alpha;
        image.color = c;
    }

    // Builds a radial gradient texture: transparent center, fully opaque at
    // the screen edges/corners. The Image's own tint color (vignetteColor)
    // controls the actual hue - this texture only carries the alpha shape.
    Sprite GenerateVignetteSprite()
    {
        Texture2D tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);
        float maxDist = center.magnitude; // distance to a corner - so corners reach full alpha

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float t = Mathf.Clamp01(dist / maxDist);
                // Ease so the center stays clear longer, edges ramp up faster
                float alpha = t * t;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f));
    }
}
