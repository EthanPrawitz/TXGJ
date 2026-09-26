using UnityEngine;

// Attach to any pustule/sac-type object. Implements IDamageable so it
// automatically works with GunAim2D's existing hit-detection - no
// changes needed to the gun script.

public class Pustule : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float health = 1f;          // default 1 = pops on a single hit

    [Header("Explosion")]
    public float explosionRadius = 2f;
    public float explosionDamage = 15f;      // max damage, dealt at the edge of the instakill radius
    public float minDamageMultiplier = 0.2f; // damage multiplier at the very edge of explosionRadius (0 = none, 1 = same as explosionDamage)
    public float instaKillRadius = 0.5f;     // being this close guarantees a kill, regardless of target's health
    public LayerMask damageMask;             // what the explosion can hurt (enemies, player, etc.)

    [Header("Cover")]
    public LayerMask obstacleMask;     // walls/geometry that can block explosion damage (line-of-sight)

    [Header("Chain Reaction")]
    public float chainRadius = 3f;     // range that can set off other pustules - separate from explosionRadius (damage range)
    public float chainDelay = 0.08f;   // small delay before a chained pustule pops, so it ripples outward visibly

    [Header("Camera Shake")]
    public float shakeDuration = 0.15f;
    public float shakeMagnitude = 0.2f;

    [Header("Effects (optional - plug in later)")]
    public GameObject explosionEffectPrefab;

    private bool hasExploded = false;
    private bool chainQueued = false;  // prevents a pustule being scheduled to explode twice by overlapping chains
    private CameraFollow2D cameraFollow;

    void Awake()
    {
        if (Camera.main != null)
        {
            cameraFollow = Camera.main.GetComponent<CameraFollow2D>();
        }
    }

    public void TakeDamage(float amount, Vector2 sourcePosition = default)
    {
        health -= amount;
        if (health <= 0f)
        {
            Explode();
        }
    }

    void Explode()
    {
        if (hasExploded) return; // guard against being triggered twice (e.g. two chains reaching it at once)
        hasExploded = true;

        // --- Query at whichever radius is larger, then filter per-target below ---
        float queryRadius = Mathf.Max(explosionRadius, chainRadius);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, queryRadius, damageMask);
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject) continue; // don't hit self

            Vector2 explosionPos = transform.position;
            Vector2 targetPos = hit.bounds.center;
            float distance = Vector2.Distance(explosionPos, targetPos);

            Vector2 dirToTarget = (targetPos - explosionPos).normalized;

            // --- Other pustules: use chainRadius, not explosionRadius, to decide range ---
            Pustule otherPustule = hit.GetComponent<Pustule>();
            if (otherPustule != null)
            {
                if (distance > chainRadius) continue;

                // Cover check still applies - a pustule behind a wall shouldn't be chained
                RaycastHit2D chainCoverHit = Physics2D.Raycast(explosionPos, dirToTarget, distance, obstacleMask);
                if (chainCoverHit.collider != null) continue;

                otherPustule.TriggerChainExplosion(chainDelay);
                continue;
            }

            // --- Everything else uses explosionRadius (the damage range) ---
            if (distance > explosionRadius) continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable == null) continue;

            // --- Cover check: is there an obstacle between the blast and this target? ---
            RaycastHit2D coverHit = Physics2D.Raycast(explosionPos, dirToTarget, distance, obstacleMask);
            if (coverHit.collider != null)
            {
                // Something solid is in the way - target has cover, blast doesn't reach it.
                continue;
            }

            // --- Inner instakill radius: guaranteed lethal regardless of target's health ---
            if (distance <= instaKillRadius)
            {
                damageable.TakeDamage(float.MaxValue, explosionPos);
                continue;
            }

            // --- Proximity falloff beyond the instakill radius: full damage tapering to minDamageMultiplier at the edge ---
            float falloffRange = explosionRadius - instaKillRadius;
            float distanceIntoFalloff = distance - instaKillRadius;
            float falloff = Mathf.Lerp(1f, minDamageMultiplier, distanceIntoFalloff / falloffRange);
            damageable.TakeDamage(explosionDamage * falloff, explosionPos);
        }

        // --- Camera shake: every explosion shakes the camera, regardless of range or what it hit ---
        if (cameraFollow != null)
        {
            cameraFollow.Shake(shakeDuration, shakeMagnitude);
        }

        // --- Visual effect placeholder ---
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    // Called by a neighboring pustule's explosion. Public so other Pustule
    // instances can trigger it, with a short delay for a visible ripple effect.
    public void TriggerChainExplosion(float delay)
    {
        if (hasExploded || chainQueued) return;
        chainQueued = true; // mark immediately so it can't be double-scheduled by overlapping chains
        Invoke(nameof(Explode), delay);
    }

    // Visualize both radii in the editor for easy tuning
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.4f);       // orange = damage radius
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.3f);     // yellow = chain-trigger radius
        Gizmos.DrawWireSphere(transform.position, chainRadius);
    }
}
