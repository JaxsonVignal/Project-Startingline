using UnityEngine;

[RequireComponent(typeof(NPCManager))]
public class NPCShooting : MonoBehaviour
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

    private NPCManager npcManager;
    private float fireCooldown;

    private void Start()
    {
        npcManager = GetComponent<NPCManager>();

        if (firePoint == null)
            Debug.LogWarning($"{name} has no firePoint assigned for shooting.");

        if (!npcManager.enableCombat)
            Debug.LogWarning($"{name} has NPCShooting but enableCombat is false on NPCManager!");
    }

    private void Update()
    {
        // Don't shoot if stunned
        if (npcManager.currentState == NPCManager.NPCState.Stunned)
            return;

        if (npcManager.currentState == NPCManager.NPCState.Attack)
            TryShootAtPlayer();
    }

    private void TryShootAtPlayer()
    {
        if (!npcManager.player) return;

        fireCooldown -= Time.deltaTime;
        if (fireCooldown > 0f) return;

        Vector3 targetPos = npcManager.player.position + Vector3.up * 1.5f; // aim near chest/head
        Vector3 direction = (targetPos - firePoint.position).normalized;

        // Apply random aim offset
        direction.x += Random.Range(-aimRandomness, aimRandomness);
        direction.y += Random.Range(-aimRandomness, aimRandomness);
        direction.z += Random.Range(-aimRandomness, aimRandomness);
        direction.Normalize();

        // Check distance
        if (Vector3.Distance(firePoint.position, npcManager.player.position) > maxFireDistance)
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