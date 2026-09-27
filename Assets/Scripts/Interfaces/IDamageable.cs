using UnityEngine;

// What kind of damage this is - lets a target react differently depending
// on the source (e.g. a door that only breaks from explosions, not bullets).
public enum DamageType
{
    Bullet,
    Explosion
}

// Implement this on any script that should be able to take damage
// (enemies, breakable objects, etc). Keeps weapons decoupled from
// specific enemy types.
//
// sourcePosition is optional - pass it when you want the target to be
// able to react directionally (e.g. knockback away from an explosion
// or gunshot). damageType defaults to Bullet, so existing calls that
// only pass amount still compile fine and behave as before.

public interface IDamageable
{
    void TakeDamage(float amount, Vector2 sourcePosition = default, DamageType damageType = DamageType.Bullet);
}
