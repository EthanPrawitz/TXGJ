using UnityEngine;

// Attach this to your Main Camera.
// Assign the player's Transform in the Inspector.

public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    public Transform target;              // the player

    [Header("Follow Feel")]
    public float smoothTime = 0.25f;      // higher = more lag/bounce, lower = snappier
    public Vector3 offset = new Vector3(0f, 1f, -10f); // keep z at -10 for 2D camera

    [Header("Look Ahead")]
    public float lookAheadDistance = 2f;  // how far ahead to shift based on velocity
    public float lookAheadSmoothTime = 0.4f;

    [Header("Aim Look Ahead")]
    public bool useAimLookAhead = true;
    public float aimLookAheadMaxDistance = 3f;  // how far the camera can lean toward the cursor
    public float aimLookAheadWeight = 0.5f;     // 0 = ignore aim, 1 = fully offset toward cursor at max distance

    [Header("Bounds (optional)")]
    public bool useBounds = false;
    public float minX, maxX, minY, maxY;

    private float shakeTimer = 0f;
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0f;

    private Vector3 velocity = Vector3.zero;         // used internally by SmoothDamp
    private Vector3 lookAheadVelocity = Vector3.zero;
    private Vector3 currentLookAhead = Vector3.zero;
    private Rigidbody2D targetRb;                    // optional, for velocity-based look-ahead
    private Camera cam;

    void Start()
    {
        if (target != null)
            targetRb = target.GetComponent<Rigidbody2D>();
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // --- Look-ahead based on target's horizontal velocity ---
        float targetLookAheadX = 0f;
        if (targetRb != null)
        {
            targetLookAheadX = Mathf.Sign(targetRb.linearVelocity.x) * lookAheadDistance
            * Mathf.Clamp01(Mathf.Abs(targetRb.linearVelocity.x) / 8f); // 8f ~= expected max speed, tune to match moveSpeed
        }
        Vector3 targetLookAhead = new Vector3(targetLookAheadX, 0f, 0f);

        // --- Aim-based look-ahead: lean toward the cursor/laser direction ---
        if (useAimLookAhead && cam != null)
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = -cam.transform.position.z;
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(mouseScreenPos);
            mouseWorldPos.z = 0f;

            Vector3 aimDirection = mouseWorldPos - target.position;
            Vector3 aimOffset = Vector3.ClampMagnitude(aimDirection * aimLookAheadWeight, aimLookAheadMaxDistance);
            targetLookAhead += aimOffset;
        }

        currentLookAhead = Vector3.SmoothDamp(currentLookAhead, targetLookAhead, ref lookAheadVelocity, lookAheadSmoothTime);

        // --- Desired position ---
        Vector3 desiredPosition = target.position + offset + currentLookAhead;

        // --- Smooth follow (this is where the "bounce"/lag comes from) ---
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);

        // --- Optional bounds clamp ---
        if (useBounds)
        {
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minX, maxX);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minY, maxY);
        }

        // --- Screen shake: applied last, on top of the smoothed follow position ---
        if (shakeTimer > 0f)
        {
            float falloff = shakeTimer / shakeDuration; // shake eases out over its duration
            Vector2 shakeOffset = Random.insideUnitCircle * shakeMagnitude * falloff;
            smoothedPosition += new Vector3(shakeOffset.x, shakeOffset.y, 0f);
            shakeTimer -= Time.deltaTime;
        }

        transform.position = smoothedPosition;
    }

    // Call this from anything that should trigger camera shake (taking damage,
    // an explosion going off nearby, etc).
    public void Shake(float duration, float magnitude)
    {
        // Take the stronger of the two if a shake is already in progress,
        // rather than always overwriting - so a big hit isn't cancelled by a weaker one.
        if (shakeTimer <= 0f || magnitude > shakeMagnitude)
        {
            shakeDuration = duration;
            shakeTimer = duration;
            shakeMagnitude = magnitude;
        }
    }
}
