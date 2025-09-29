using System.Collections;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public static PlayerShooting Instance;

    private void Awake() => Instance = this;

    public Transform firePoint;
    public GameObject bulletPrefab;
    public Camera playerCamera;

    // Current weapon info
    private WeaponData currentWeapon;
    private int currentAmmo;
    private bool isReloading;
    private float nextFireTime;

    private AudioSource weaponAudio;

    private bool isFiring;

    [Header("Recoil Settings")]
    [SerializeField] private float recoilRecoverySpeed = 6f;
    private Vector3 currentRecoil;
    private Vector3 targetRecoil;

    private void Update()
    {
        if (isFiring && currentWeapon != null)
        {
            if (Time.time >= nextFireTime)
            {
                Fire();

                if (currentWeapon.fireMode == FireMode.FullAuto)
                    nextFireTime = Time.time + currentWeapon.fireRate;
                else
                    isFiring = false;
            }
        }

        // Smoothly recover camera from recoil
        currentRecoil = Vector3.Lerp(currentRecoil, Vector3.zero, recoilRecoverySpeed * Time.deltaTime);

        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(currentRecoil);
        }
    }

    public void EquipWeapon(WeaponData weapon)
    {
        currentWeapon = weapon;
        if (weapon == null) return;

        if (firePoint != null)
        {
            weaponAudio = firePoint.GetComponent<AudioSource>();
            if (weaponAudio == null)
                weaponAudio = firePoint.gameObject.AddComponent<AudioSource>();

            weaponAudio.clip = weapon.shootSound;
            weaponAudio.loop = weapon.fireMode == FireMode.FullAuto;
            weaponAudio.playOnAwake = false;
            weaponAudio.spatialBlend = 1f;
        }

        currentAmmo = weapon.magazineSize;
    }

    public void StartFiring()
    {
        if (currentWeapon == null) return;

        if (currentWeapon.fireMode == FireMode.SemiAuto)
            Fire();
        else
            isFiring = true;
    }

    public void StopFiring()
    {
        isFiring = false;

        if (weaponAudio != null && currentWeapon != null && currentWeapon.fireMode == FireMode.FullAuto)
            StartCoroutine(StopWeaponAudioDelayed(currentWeapon.ShootingSoundDelay));
    }

    private void Fire()
    {
        if (isReloading || currentAmmo <= 0)
        {
            Debug.Log("Out of ammo! Reload first.");
            return;
        }

        currentAmmo--;

        // Play shooting sound
        if (weaponAudio != null)
        {
            if (currentWeapon.fireMode == FireMode.FullAuto)
            {
                if (!weaponAudio.isPlaying)
                    weaponAudio.Play();
            }
            else
                weaponAudio.PlayOneShot(currentWeapon.shootSound);
        }

        // Spawn bullet
        if (bulletPrefab && firePoint && playerCamera)
        {
            GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

            float horizontalSpread = Random.Range(-currentWeapon.spread, currentWeapon.spread);
            Vector3 shootDirection = playerCamera.transform.forward + playerCamera.transform.right * horizontalSpread;
            shootDirection.Normalize();

            bulletObj.transform.forward = shootDirection;

            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.damage = currentWeapon.damage;
                bullet.speed = currentWeapon.bulletSpeed;
            }

            Rigidbody rb = bulletObj.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(shootDirection * currentWeapon.bulletSpeed, ForceMode.Impulse);
        }

        if (currentWeapon.muzzleFlashPrefab)
            Instantiate(currentWeapon.muzzleFlashPrefab, firePoint.position, firePoint.rotation);

        // --- RELATIVE CAMERA RECOIL ---
        float verticalRecoil = Random.Range(currentWeapon.recoilAmount * 0.5f, currentWeapon.recoilAmount);
        float horizontalRecoil = Random.Range(-currentWeapon.spread, currentWeapon.spread);

        targetRecoil += new Vector3(-verticalRecoil, horizontalRecoil, 0f); // only relative
        currentRecoil += targetRecoil;
    }

    public void Reload()
    {
        if (currentWeapon == null || isReloading || currentAmmo == currentWeapon.magazineSize) return;

        StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        Debug.Log($"Reloading {currentWeapon.Name}...");

        if (currentWeapon.reloadSound)
            AudioSource.PlayClipAtPoint(currentWeapon.reloadSound, firePoint.position);

        yield return new WaitForSeconds(currentWeapon.reloadTime);

        currentAmmo = currentWeapon.magazineSize;
        isReloading = false;

        Debug.Log($"Reloaded {currentWeapon.Name}, Ammo: {currentAmmo}");
    }

    private IEnumerator StopWeaponAudioDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (weaponAudio != null && !isFiring)
            weaponAudio.Stop();
    }
}
