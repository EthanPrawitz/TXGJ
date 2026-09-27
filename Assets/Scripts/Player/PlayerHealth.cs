using UnityEngine;
using System;

// Attach to the Player GameObject. Implements IDamageable so it works
// automatically with anything that deals damage (gun hits, explosions, etc).

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth { get; private set; }

    [Header("Invulnerability")]
    public float invulnerabilityDuration = 0.5f; // brief i-frames after taking a hit, common in roguelites
    private float invulnerableUntil = 0f;

    [Header("Knockback")]
    public float knockbackForce = 12f;
    public float knockbackDuration = 0.2f;
    public PlayerMovement2D movement; // drag the Player's PlayerMovement2D component here (usually same object)
    public GunAim2D gunAim;           // drag the Player's GunAim2D component here (usually same object)

    [Header("Camera Shake")]
    public CameraFollow2D cameraFollow; // drag your Main Camera (with CameraFollow2D) here
    public float shakeDuration = 0.15f;
    public float shakeMagnitude = 0.2f;

    // Events - hook OnHealthChanged up to UI, OnDeath to your game-over/respawn flow
    public event Action<float, float> OnHealthChanged; // (current, max)
    public event Action OnDeath;

    void Awake()
    {
        currentHealth = maxHealth;
        if (movement == null) movement = GetComponent<PlayerMovement2D>();
        if (gunAim == null) gunAim = GetComponent<GunAim2D>();
        if (cameraFollow == null && Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow2D>();
    }

    public void TakeDamage(float amount, Vector2 sourcePosition = default, DamageType damageType = DamageType.Bullet)
    {
        if (Time.time < invulnerableUntil) return; // still invulnerable, ignore this hit
        if (currentHealth <= 0f) return;            // already dead

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);
        invulnerableUntil = Time.time + invulnerabilityDuration;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // --- Knockback: push away from the source of the damage ---
        if (movement != null)
        {
            Vector2 knockbackDir = ((Vector2)transform.position - sourcePosition);
            knockbackDir = knockbackDir.sqrMagnitude > 0.01f ? knockbackDir.normalized : Vector2.up;
            movement.ApplyKnockback(knockbackDir * knockbackForce, knockbackDuration);
        }

        // --- Camera shake ---
        if (cameraFollow != null)
        {
            cameraFollow.Shake(shakeDuration, shakeMagnitude);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Die()
    {
        // --- Disable player control: no movement, no aiming/firing, laser hidden ---
        // zeroVelocityOnDisable = false so the fatal hit's knockback still plays out
        // instead of being instantly wiped by disabling the script.
        if (movement != null) movement.SetControlEnabled(false, false);
        if (gunAim != null) gunAim.SetControlEnabled(false);
        if (cameraFollow != null) cameraFollow.useAimLookAhead = false;

        OnDeath?.Invoke();
        // Further death handling can go here later: death animation,
        // game-over screen, respawn timer, etc. Call movement.SetControlEnabled(true),
        // gunAim.SetControlEnabled(true), and cameraFollow.useAimLookAhead = true
        // (plus reset currentHealth) on respawn.
    }
}
