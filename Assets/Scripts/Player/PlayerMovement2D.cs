using UnityEngine;

// Attach this to your player GameObject.
// Requires: Rigidbody2D (Gravity Scale ~3-5, Freeze Rotation Z checked)
//           A BoxCollider2D or CapsuleCollider2D
//           A child empty GameObject named "GroundCheck" positioned at the player's feet
//           A ground layer assigned in the Inspector

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float jumpForce = 14f;
    public float accelerationRate = 60f;   // units/sec^2 while a direction is held
    public float decelerationRate = 80f;   // units/sec^2 while no input (snappier stop)

    [Header("Jump Feel")]
    public float lowJumpMultiplier = 2.5f;   // extra gravity when rising but button released (short hop)
    public float fallMultiplier = 2.2f;      // extra gravity when falling (snappier descent)

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("Optional Jam Extras")]
    public float coyoteTime = 0.1f;          // grace period to jump after leaving a ledge
    public float jumpBufferTime = 0.1f;      // early jump press registers if grounded shortly after

    private float knockbackTimer = 0f;       // while > 0, normal horizontal control is suspended

    private Rigidbody2D rb;
    private Animator animator;               // optional, safe if null
    private float moveInput;
    private bool isGrounded;
    private bool jumpPressed;
    private bool jumpHeld;
    private float coyoteTimer;
    private float jumpBufferTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>(); // optional
    }

    void Update()
    {
        // --- Input ---
        moveInput = Input.GetAxisRaw("Horizontal"); // -1, 0, or 1

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferTimer = jumpBufferTime;
        }
        jumpHeld = Input.GetButton("Jump");

        // --- Ground check ---
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // --- Timers ---
        if (isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.deltaTime;

        // Facing direction is now controlled by GunAim2D (based on aim/cursor),
        // not movement input, since the gun is part of the player sprite.

        // --- Animator params (safe no-op if no Animator attached) ---
        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(moveInput));
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
        }
    }

    void FixedUpdate()
    {
        // --- Knockback: while active, skip normal horizontal control and let physics carry the impulse ---
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            return;
        }

        // --- Horizontal movement (accelerate toward target speed, decelerate toward 0) ---
        float targetSpeed = moveInput * moveSpeed;
        float rate = Mathf.Abs(moveInput) > 0.01f ? accelerationRate : decelerationRate;
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);

        // --- Jump (uses coyote time + jump buffer) ---
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        // --- Better jump arc: extra gravity when falling or short-hopping ---
        if (rb.linearVelocity.y < 0f && !isGrounded)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0f && !jumpHeld)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    // Call this from anything that damages the player (e.g. PlayerHealth) to
    // apply a knockback impulse. Movement control is suspended for `duration`
    // seconds so this velocity isn't instantly overwritten by normal movement.
    // Read-only access for other scripts (e.g. an animation state controller)
    public bool IsGrounded => isGrounded;
    public Vector2 Velocity => rb.linearVelocity;
    public float MoveInput => moveInput;

    public void ApplyKnockback(Vector2 velocity, float duration)
    {
        rb.linearVelocity = velocity;
        knockbackTimer = duration;
    }

    // Call with false to fully disable player control (e.g. on death) - stops
    // Update/FixedUpdate from running and freezes physics so the character
    // doesn't keep sliding/falling under whatever velocity it had. Call with
    // true to restore control (e.g. on respawn).
    public void SetControlEnabled(bool value, bool zeroVelocityOnDisable = true)
    {
        enabled = value;
        if (!value && zeroVelocityOnDisable && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
