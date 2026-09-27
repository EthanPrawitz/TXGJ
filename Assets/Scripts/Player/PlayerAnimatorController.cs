using UnityEngine;

// Attach to the Player GameObject (alongside PlayerMovement2D, PlayerHealth,
// and the Animator). Drives a single Animator integer parameter ("AnimState")
// so your Animator Controller can use simple "Any State -> X" transitions
// with a condition of AnimState == (int)PlayerAnimState.X, Has Exit Time OFF.
//
// PlayerAnimState values (set these as the actual int in your Animator too):
//   0 = Idle, 1 = Running, 2 = Jumping, 3 = Falling, 4 = Dead

public enum PlayerAnimState
{
    Idle = 0,
    Running = 1,
    Jumping = 2,
    Falling = 3,
    Dead = 4
}

public class PlayerAnimatorController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public PlayerMovement2D movement;
    public PlayerHealth health;

    [Header("Tuning")]
    public float runSpeedThreshold = 0.1f; // horizontal speed above this counts as "Running"

    private const string AnimStateParam = "AnimState";
    private PlayerAnimState currentState = PlayerAnimState.Idle;
    private bool isDead = false;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (movement == null) movement = GetComponent<PlayerMovement2D>();
        if (health == null) health = GetComponent<PlayerHealth>();
    }

    void OnEnable()
    {
        if (health != null)
        {
            health.OnDeath += HandleDeath;
        }
    }

    void OnDisable()
    {
        if (health != null)
        {
            health.OnDeath -= HandleDeath;
        }
    }

    void HandleDeath()
    {
        isDead = true;
        SetState(PlayerAnimState.Dead);
    }

    void Update()
    {
        // Once dead, stay dead - don't let any other state override it
        // (e.g. call ResetForRespawn() below when you build a respawn flow).
        if (isDead) return;

        PlayerAnimState desiredState;

        if (!movement.IsGrounded)
        {
            desiredState = movement.Velocity.y > 0f ? PlayerAnimState.Jumping : PlayerAnimState.Falling;
        }
        else if (Mathf.Abs(movement.Velocity.x) > runSpeedThreshold)
        {
            desiredState = PlayerAnimState.Running;
        }
        else
        {
            desiredState = PlayerAnimState.Idle;
        }

        SetState(desiredState);
    }

    void SetState(PlayerAnimState newState)
    {
        if (newState == currentState) return; // avoid redundant Animator calls every frame

        currentState = newState;
        if (animator != null)
        {
            animator.SetInteger(AnimStateParam, (int)newState);
        }
    }

    // Call this when building your respawn flow, alongside resetting health,
    // re-enabling PlayerMovement2D/GunAim2D, etc.
    public void ResetForRespawn()
    {
        isDead = false;
        SetState(PlayerAnimState.Idle);
    }
}
