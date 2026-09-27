using UnityEngine;
using UnityEngine.InputSystem;

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
    public int currentAmmo { get; private set; }
    public bool isReloading { get; private set; }

    [Header("Fire Camera Shake")]
    public float fireShakeDuration = 0.05f;
    public float fireShakeMagnitude = 0.05f;

    private SpriteRenderer spriteRenderer;
    private LineRenderer lineRenderer;
    private Camera cam;
    private CameraFollow2D cameraFollow;
    private bool facingRight = true;
    private float nextFireTime = 0f;
    private float reloadFinishTime = 0f;

    // Input States tracked from PlayerInput callbacks
    private Vector2 rawPointerPosition;
    private bool isFirePressed;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        lineRenderer = GetComponent<LineRenderer>();
        cam = Camera.main;
        currentAmmo = maxAmmo;
        if (cam != null) cameraFollow = cam.GetComponent<CameraFollow2D>();
    }

    #region PlayerInput Callbacks

    // Called by PlayerInput when the Aim/Pointer action updates (Vector2 value type)
    public void OnAim(InputAction.CallbackContext context)
    {
        rawPointerPosition = context.ReadValue<Vector2>();
    }

    // Called by PlayerInput when the Fire action changes (Button type)
    public void OnFire(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isFirePressed = true;
        }
        else if (context.canceled)
        {
            isFirePressed = false;
        }
    }

    // Called by PlayerInput when the Reload action triggers (Button type)
    public void OnReload(InputAction.CallbackContext context)
    {
        if (context.performed && !isReloading && currentAmmo < maxAmmo)
        {
            StartReload();
        }
    }

    #endregion

    void Update()
    {
        // --- Cursor position in world space ---
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(rawPointerPosition.x, rawPointerPosition.y, -cam.transform.position.z));
        mouseWorldPos.z = 0f;

        // --- Flip the whole player based on cursor side ---
        facingRight = mouseWorldPos.x >= transform.position.x;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (facingRight ? 1f : -1f);
        transform.localScale = scale;

        // --- Aim direction from the gun point to the cursor ---
        Vector2 origin = gunPoint.position;
        Vector2 direction = ((Vector2)mouseWorldPos - origin).normalized;

        // --- Reload finish check ---
        if (isReloading && Time.time >= reloadFinishTime)
        {
            isReloading = false;
            currentAmmo = maxAmmo;
        }

        // --- Fire input handling ---
        bool wantsToFire = fullAuto ? isFirePressed : (isFirePressed && Time.time >= nextFireTime);
        if (wantsToFire && !isReloading && currentAmmo > 0 && Time.time >= nextFireTime)
        {
            Fire(origin, direction);
            nextFireTime = Time.time + (1f / fireRate);

            // Semi-auto needs to consume the press so holding doesn't trigger multiple shots
            if (!fullAuto)
            {
                isFirePressed = false;
            }

            if (currentAmmo <= 0)
            {
                StartReload();
            }
        }

        // --- Laser rendering ---
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

        if (hit.collider != null)
        {
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
        }

        if (muzzleFlash != null)
        {
            muzzleFlash.TriggerFlash();
        }

        if (cameraFollow != null)
        {
            cameraFollow.Shake(fireShakeDuration, fireShakeMagnitude);
        }
    }

    public void SetControlEnabled(bool value)
    {
        enabled = value;
        if (lineRenderer != null)
        {
            lineRenderer.enabled = value;
        }
    }
}