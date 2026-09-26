using UnityEngine;

// Implement this on any script that should be able to take damage
// (enemies, breakable objects, etc). Keeps weapons decoupled from
// specific enemy types.
//
// sourcePosition is optional - pass it when you want the target to be
// able to react directionally (e.g. knockback away from an explosion
// or gunshot). Existing calls that only pass amount still compile fine.

public interface IDamageable
{
    void TakeDamage(float amount, Vector2 sourcePosition = default);
}
