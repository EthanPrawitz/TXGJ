using UnityEngine;

// Attach this to the Player GameObject (the one with the SpriteRenderer
// that has the gun baked into it).
// Requires: a SpriteRenderer on this same object
//           a LineRenderer on this same object (for the laser)
//           an empty child Transform named "GunPoint" placed roughly where
//           the gun barrel is drawn on the sprite (facing right by default)

[RequireComponent(typeof(LineRenderer))]
public class GunAim2D : MonoBehaviour
{
    [SerializeField] private MuzzleFlash muzzleFlash;

    [Header("Aim Point")]
    public Transform gunPoint;            // child marking the barrel position (default: facing right)

    [Header("Laser")]
    public float maxLaserDistance = 20f;
    public LayerMask hitMask;              // what the laser can hit (walls, enemies); leave empty to always go full length

    [Header("Shooting")]
    public float damage = 10f;
    public float fireRate = 5f;            // shots per second
    public bool fullAuto = true;           // true = hold to fire, false = click each shot

    [Header("Ammo")]
    public int maxAmmo = 30;
    public float reloadTime = 1.5f;
    public KeyCode reloadKey = KeyCode.R;
    public int currentAmmo { get; private set; }
    public bool isReloading { get; private set; }

    [Header("Fire Camera Shake")]
    public float fireShakeDuration = 0.05f;
    public float fireShakeMagnitude = 0.05f; // kept small - this fires often, unlike explosion shake

    private SpriteRenderer spriteRenderer;
    private LineRenderer lineRenderer;
    private Camera cam;
    private CameraFollow2D cameraFollow;
    private bool facingRight = true;
    private float nextFireTime = 0f;
    private float reloadFinishTime = 0f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        lineRenderer = GetComponent<LineRenderer>();
        cam = Camera.main;
        currentAmmo = maxAmmo;
        if (cam != null) cameraFollow = cam.GetComponent<CameraFollow2D>();
    }

    void Update()
    {
        // --- Cursor position in world space ---
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = -cam.transform.position.z;
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;

        // --- Flip the whole player based on cursor side (body stays upright) ---
        // Uses a 180-degree Y-axis ROTATION, not a scale flip or spriteRenderer.flipX.
        // Both of those are reflections (they invert coordinate handedness), and
        // normal-mapped lighting (Sprite-Lit-Default) doesn't recompute correctly
        // across a reflection - the sprite renders unlit/dark on the flipped side
        // regardless of which reflection method is used. A Y rotation is a true
        // rotation instead, so it produces the same mirrored look on screen while
        // keeping normal map lighting correct on both sides. It also auto-mirrors
        // gunPoint's child position, so no manual offset is needed.
        facingRight = mouseWorldPos.x >= transform.position.x;
        transform.rotation = Quaternion.Euler(0f, facingRight ? 0f : 180f, 0f);

        // --- Aim direction from the gun point to the cursor ---
        Vector2 origin = gunPoint.position;
        Vector2 direction = ((Vector2)mouseWorldPos - origin).normalized;

        // --- Reload finish check ---
        if (isReloading && Time.time >= reloadFinishTime)
        {
            isReloading = false;
            currentAmmo = maxAmmo;
        }

        // --- Reload input (manual, only if not already full/reloading) ---
        if (Input.GetKeyDown(reloadKey) && !isReloading && currentAmmo < maxAmmo)
        {
            StartReload();
        }

        // --- Fire input ---
        bool wantsToFire = fullAuto ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
        if (wantsToFire && !isReloading && currentAmmo > 0 && Time.time >= nextFireTime)
        {
            Fire(origin, direction);
            nextFireTime = Time.time + (1f / fireRate);

            // Auto-reload once the mag empties
            if (currentAmmo <= 0)
            {
                StartReload();
            }
        }

        // --- Laser is always drawn (sight/attachment beam) regardless of firing ---
        DrawLaser(origin, direction);
    }

    void StartReload()
    {
        isReloading = true;
        reloadFinishTime = Time.time + reloadTime;
    }

    void DrawLaser(Vector2 origin, Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, maxLaserDistance, hitMask);

        Vector2 endPoint = hit.collider != null
        ? hit.point
        : origin + direction * maxLaserDistance;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);
    }

    void Fire(Vector2 origin, Vector2 direction)
    {
        currentAmmo--;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, maxLaserDistance, hitMask);

        // --- Apply damage if we hit something damageable ---
        if (hit.collider != null)
        {
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
        }

        // --- Muzzle flash effect ---
        if (muzzleFlash != null)
        {
            muzzleFlash.TriggerFlash();
        }

        // --- Camera shake on fire ---
        // No dead-check needed here: SetControlEnabled(false) disables this whole
        // script on death, so Update (and therefore Fire) can't run at all while dead.
        if (cameraFollow != null)
        {
            cameraFollow.Shake(fireShakeDuration, fireShakeMagnitude);
        }

        // Bullet impact effects (sparks, hit markers, etc.) can be spawned here later,
        // using hit.point and hit.normal if hit.collider != null.
    }

    // Call with false to fully disable aiming/firing (e.g. on death) - stops
    // Update from running and immediately hides the laser. Call with true
    // to restore control (e.g. on respawn).
    public void SetControlEnabled(bool value)
    {
        enabled = value;
        if (lineRenderer != null)
        {
            lineRenderer.enabled = value;
        }
    }
}
