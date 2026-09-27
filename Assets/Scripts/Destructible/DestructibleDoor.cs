using UnityEngine;

// Attach to a door GameObject (needs a Collider2D so explosions/raycasts
// can detect it, and its layer must be included in Pustule's damageMask
// so OverlapCircleAll actually finds it).
//
// Low default health means even weak falloff damage at the edge of an
// explosion's range is enough to destroy it - so being caught anywhere
// in the blast radius breaks it, without needing special-case logic
// in the explosion code itself.

public class DestructibleDoor : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float health = 1f; // low on purpose - see note above

    [Header("Effects (optional - plug in later)")]
    public GameObject debrisEffectPrefab;

    private bool isDestroyed = false;

    public void TakeDamage(float amount, Vector2 sourcePosition = default, DamageType damageType = DamageType.Bullet)
    {
        if (isDestroyed) return;
        if (damageType != DamageType.Explosion) return; // bullets/lasers don't affect the door, only explosions do

        health -= amount;
        if (health <= 0f)
        {
            Break();
        }
    }

    void Break()
    {
        isDestroyed = true;

        if (debrisEffectPrefab != null)
        {
            Instantiate(debrisEffectPrefab, transform.position, Quaternion.identity);
        }

        // Destroys the door outright. If you'd rather it visually stay as an
        // "open/broken" doorway instead of vanishing, swap this for disabling
        // the SpriteRenderer + Collider2D and leaving the GameObject in place.
        Destroy(gameObject);
    }
}
