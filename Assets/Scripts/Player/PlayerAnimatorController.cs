using UnityEngine;

// Attach to Player GameObject.
// Drives an Animator integer parameter ("AnimState").
// Set up transitions in your Animator Controller as:
// Any State -> State (Condition: AnimState Equals X, Has Exit Time: OFF, Duration: 0)

public enum PlayerAnimState
{
    Idle = 0,
    Running = 1,
    Jumping = 2,
    Falling = 3,
    Dead = 4
}

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerMovement2D))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerAnimatorController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public PlayerMovement2D movement;
    public PlayerHealth health;

    [Header("Tuning")]
    public float runSpeedThreshold = 0.1f;

    private static readonly int AnimStateHash = Animator.StringToHash("AnimState");
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
        // Block state updates if dead
        if (isDead) return;

        PlayerAnimState desiredState;

        // Airborne evaluation
        if (!movement.IsGrounded)
        {
            desiredState = movement.Velocity.y > 0f ? PlayerAnimState.Jumping : PlayerAnimState.Falling;
        }
        // Grounded movement evaluation (reads velocity instead of raw input to sync with physics)
        else if (Mathf.Abs(movement.Velocity.x) > runSpeedThreshold)
        {
            desiredState = PlayerAnimState.Running;
        }
        // Idle evaluation
        else
        {
            desiredState = PlayerAnimState.Idle;
        }

        SetState(desiredState);
    }

    void SetState(PlayerAnimState newState)
    {
        if (newState == currentState) return; // Prevent redundant calls

        currentState = newState;
        if (animator != null)
        {
            animator.SetInteger(AnimStateHash, (int)newState);
        }
    }

    public void ResetForRespawn()
    {
        isDead = false;
        SetState(PlayerAnimState.Idle);
    }
}