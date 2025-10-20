using UnityEngine;

[RequireComponent(typeof(GuardManager))]
public class GuardShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 1f;               // shots per second
    public float bulletSpeed = 30f;
    public float aimRandomness = 0.05f;       // deviation in aim (higher = less accurate)
    public float maxFireDistance = 25f;

    [Header("Audio & FX")]
    public AudioSource shootSound;
    public ParticleSystem muzzleFlash;

    private GuardManager guard;
    private float fireCooldown;

    private void Start()
    {
        guard = GetComponent<GuardManager>();
        if (firePoint == null)
            Debug.LogWarning($"{name} has no firePoint assigned for shooting.");
    }

    private void Update()
    {
        if (guard.currentState == GuardManager.GuardState.Attack)
            TryShootAtPlayer();
    }

    private void TryShootAtPlayer()
    {
        if (!guard.player) return;

        fireCooldown -= Time.deltaTime;
        if (fireCooldown > 0f) return;

        Vector3 targetPos = guard.player.position + Vector3.up * 1.5f; // aim near chest/head
        Vector3 direction = (targetPos - firePoint.position).normalized;

        // Apply random aim offset
        direction.x += Random.Range(-aimRandomness, aimRandomness);
        direction.y += Random.Range(-aimRandomness, aimRandomness);
        direction.z += Random.Range(-aimRandomness, aimRandomness);
        direction.Normalize();

        // Check distance
        if (Vector3.Distance(firePoint.position, guard.player.position) > maxFireDistance)
            return;

        // Fire projectile
        Shoot(direction);
        fireCooldown = 1f / fireRate;
    }

    private void Shoot(Vector3 direction)
    {
        if (bulletPrefab == null) return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(direction));
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb)
            rb.velocity = direction * bulletSpeed;

        if (shootSound)
            shootSound.Play();

        if (muzzleFlash)
            muzzleFlash.Play();
    }
}
