using UnityEngine;

[RequireComponent(typeof(PoliceManager))]
public class PoliceShooting : MonoBehaviour
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

    private PoliceManager police;
    private float fireCooldown;

    private void Start()
    {
        police = GetComponent<PoliceManager>();

        if (firePoint == null)
            Debug.LogWarning($"{name} has no firePoint assigned for shooting.");
    }

    private void Update()
    {
        if (police.currentState == PoliceManager.PoliceState.Aggro)
            TryShootAtPlayer();
    }

    private void TryShootAtPlayer()
    {
        if (!police.player) return;

        fireCooldown -= Time.deltaTime;
        if (fireCooldown > 0f) return;

        // Aim at the same height as the firePoint to shoot straight
        Vector3 targetPos = police.player.transform.position;
        targetPos.y = firePoint.position.y;
        Vector3 direction = (targetPos - firePoint.position).normalized;

        // Apply random aim offset
        direction.x += Random.Range(-aimRandomness, aimRandomness);
        direction.y += Random.Range(-aimRandomness, aimRandomness);
        direction.z += Random.Range(-aimRandomness, aimRandomness);
        direction.Normalize();

        // Check distance
        if (Vector3.Distance(firePoint.position, police.player.transform.position) > maxFireDistance)
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