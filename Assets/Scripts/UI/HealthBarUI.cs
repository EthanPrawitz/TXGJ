using UnityEngine;
using UnityEngine.UI;

// Attach to a UI GameObject (e.g. a "HealthBar" object under your Canvas).
// Requires: a UI Slider assigned in the Inspector
//           a reference to the player's PlayerHealth component

public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    public Slider slider;
    public PlayerHealth playerHealth;

    [Header("Smoothing (optional)")]
    public bool smoothFill = true;
    public float smoothSpeed = 8f;

    private float targetValue;

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
        if (playerHealth != null && slider != null)
        {
            // Match the slider's range to actual health values (e.g. 0-100),
            // rather than normalizing to 0-1 - keeps it matching whatever
            // Min/Max you've set on the Slider component in the Inspector.
            slider.minValue = 0f;
            slider.maxValue = playerHealth.maxHealth;

            targetValue = playerHealth.currentHealth;
            slider.value = targetValue;
        }
    }

    void HandleHealthChanged(float current, float max)
    {
        slider.maxValue = max; // stays in sync if max health ever changes (upgrades, etc.)
        targetValue = current;

        if (!smoothFill)
        {
            slider.value = targetValue;
        }
    }

    void Update()
    {
        // Smoothly animate the bar toward the target value instead of snapping instantly
        if (smoothFill && slider != null)
        {
            slider.value = Mathf.MoveTowards(slider.value, targetValue, smoothSpeed * Time.deltaTime * slider.maxValue);
        }
    }
}
