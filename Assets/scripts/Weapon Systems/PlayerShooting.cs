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

    public void EquipWeapon(WeaponData weapon)
    {
        currentWeapon = weapon;

        if (weapon == null) return;

        // Get or add AudioSource from the weapon prefab
        if (PlayerShooting.Instance.firePoint != null)
        {
            weaponAudio = PlayerShooting.Instance.firePoint.GetComponent<AudioSource>();
            if (weaponAudio == null)
                weaponAudio = PlayerShooting.Instance.firePoint.gameObject.AddComponent<AudioSource>();

            weaponAudio.clip = weapon.shootSound;
            weaponAudio.loop = weapon.fireMode == FireMode.FullAuto; // only loop for auto
            weaponAudio.playOnAwake = false;
            weaponAudio.spatialBlend = 1f;
        }

        currentAmmo = weapon.magazineSize;
    }

    public void StartFiring()
    {
        if (currentWeapon == null) return;

        if (currentWeapon.fireMode == FireMode.SemiAuto)
            Fire(); // single shot immediately
        else
            isFiring = true;
    }

    public void StopFiring()
    {
        isFiring = false;

        if (weaponAudio != null && currentWeapon != null && currentWeapon.fireMode == FireMode.FullAuto)
        {
            // Only full-auto needs delayed stopping
            StartCoroutine(StopWeaponAudioDelayed(currentWeapon.ShootingSoundDelay));
        }
    }

    private bool isFiring;

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
                    isFiring = false; // stop firing for semi-auto
            }
        }
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
                // Looping for auto
                if (!weaponAudio.isPlaying)
                    weaponAudio.Play();
            }
            else
            {
                // Semi-auto: always play over current sound
                weaponAudio.PlayOneShot(currentWeapon.shootSound);
            }
        }

        // Spawn bullet
        if (bulletPrefab && firePoint && playerCamera)
        {
            GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            Vector3 shootDirection = playerCamera.transform.forward;
            bulletObj.transform.forward = shootDirection;

            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.damage = currentWeapon.damage;
                bullet.speed = currentWeapon.bulletSpeed;
            }

            Rigidbody rb = bulletObj.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(shootDirection * 50f, ForceMode.Impulse);
        }

        if (currentWeapon.muzzleFlashPrefab)
            Instantiate(currentWeapon.muzzleFlashPrefab, firePoint.position, firePoint.rotation);

        Debug.Log($"Fired {currentWeapon.Name}, Ammo left: {currentAmmo}");
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

        if (weaponAudio != null && !isFiring) // still not firing
            weaponAudio.Stop();
    }
}
