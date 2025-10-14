using System.Collections;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public static PlayerShooting Instance;

    private void Awake() => Instance = this;

    public Transform firePoint;
    public GameObject bulletPrefab;
    public Camera playerCamera;

    public Recoil recoilScript;

    private WeaponData currentWeapon;
    private string currentWeaponSlotID;
    private int currentAmmo;
    private bool isReloading;
    private float nextFireTime;

    private AudioSource weaponAudio;
    private bool isFiring;

    private Recoil recoil;

    private void Update()
    {
        recoil = transform.Find("cameraHolder/CameraRecoil").GetComponent<Recoil>();

        if (isFiring && currentWeapon != null)
        {
            if (Time.time >= nextFireTime)
            {
                Fire();

                nextFireTime = Time.time + currentWeapon.fireRate;

                if (currentWeapon.fireMode != FireMode.FullAuto)
                    isFiring = false;
            }
        }
    }

    public void EquipWeapon(WeaponData weapon)
    {
        EquipWeapon(weapon, null);
    }

    public void EquipWeapon(WeaponData weapon, string slotID)
    {
        // Save current weapon's ammo
        if (currentWeapon != null && !string.IsNullOrEmpty(currentWeaponSlotID))
        {
            WeaponAmmoTracker.SetAmmo(currentWeaponSlotID, currentAmmo);
        }

        currentWeapon = weapon;
        currentWeaponSlotID = slotID;

        if (weapon == null)
        {
            currentAmmo = 0;
            return;
        }

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

        // Load ammo for this specific weapon slot
        if (!string.IsNullOrEmpty(slotID))
        {
            currentAmmo = WeaponAmmoTracker.GetAmmo(slotID, weapon.magazineSize);
        }
        else
        {
            currentAmmo = weapon.magazineSize;
        }

        if (recoilScript != null)
            recoilScript.SetWeaponData(weapon);

        Debug.Log($"Equipped {weapon.Name} (Slot: {slotID}) - Ammo: {currentAmmo}/{weapon.magazineSize}");
    }

    public void StartFiring()
    {
        if (currentWeapon == null) return;

        if (Time.time < nextFireTime) return;

        if (currentWeapon.fireMode == FireMode.SemiAuto || currentWeapon.fireMode == FireMode.Shotgun)
        {
            Fire();
            nextFireTime = Time.time + currentWeapon.fireRate;
        }
        else
        {
            isFiring = true;
        }
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

        // Save ammo after each shot
        if (!string.IsNullOrEmpty(currentWeaponSlotID))
        {
            WeaponAmmoTracker.SetAmmo(currentWeaponSlotID, currentAmmo);
        }

        // Play shooting sound
        if (weaponAudio != null)
        {
            if (currentWeapon.fireMode == FireMode.FullAuto)
            {
                if (!weaponAudio.isPlaying)
                    weaponAudio.Play();
            }
            else
            {
                weaponAudio.PlayOneShot(currentWeapon.shootSound);
            }
        }

        // Determine number of bullets to fire
        int pellets = currentWeapon.fireMode == FireMode.Shotgun ? currentWeapon.pelletsPerShot : 1;

        for (int i = 0; i < pellets; i++)
        {
            if (bulletPrefab && firePoint && playerCamera)
            {
                GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

                Vector3 shootDirection = playerCamera.transform.forward;

                if (currentWeapon.fireMode == FireMode.Shotgun)
                {
                    float spreadX = Random.Range(-currentWeapon.spreadAngle, currentWeapon.spreadAngle);
                    float spreadY = Random.Range(-currentWeapon.spreadAngle, currentWeapon.spreadAngle);
                    Quaternion spreadRot = Quaternion.Euler(spreadY, spreadX, 0);
                    shootDirection = spreadRot * shootDirection;
                }
                else
                {
                    float horizontalSpread = Random.Range(-currentWeapon.spread, currentWeapon.spread);
                    shootDirection += playerCamera.transform.right * horizontalSpread;
                }

                shootDirection.Normalize();
                bulletObj.transform.forward = shootDirection;

                //  Assign WeaponData directly (dynamic damage)
                Bullet bullet = bulletObj.GetComponent<Bullet>();
                if (bullet != null)
                {
                    bullet.weaponData = currentWeapon;
                }

                // Apply speed from WeaponData
                Rigidbody rb = bulletObj.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.AddForce(shootDirection * currentWeapon.bulletSpeed, ForceMode.Impulse);
            }
        }

        if (currentWeapon.muzzleFlashPrefab)
            Instantiate(currentWeapon.muzzleFlashPrefab, firePoint.position, firePoint.rotation);

        recoil.RecoilFire();
    }

    public void Reload()
    {
        if (currentWeapon == null || isReloading) return;

        // Check if magazine is already full
        if (currentAmmo == currentWeapon.magazineSize)
        {
            Debug.Log("Magazine is already full!");
            return;
        }

        StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        isReloading = true;

        var playerInventory = FindObjectOfType<PlayerInventoryHolder>();
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventoryHolder not found!");
            isReloading = false;
            yield break;
        }

        int ammoNeeded = currentWeapon.magazineSize - currentAmmo;
        int availableAmmo = playerInventory.PrimaryInventorySystem.GetAmmoCount(currentWeapon.requiredAmmoType);

        if (availableAmmo <= 0)
        {
            Debug.Log($"No {currentWeapon.requiredAmmoType} ammo in inventory!");
            isReloading = false;
            yield break;
        }

        Debug.Log($"Reloading {currentWeapon.Name}... Available ammo: {availableAmmo}");

        if (currentWeapon.reloadSound)
            AudioSource.PlayClipAtPoint(currentWeapon.reloadSound, firePoint.position);

        yield return new WaitForSeconds(currentWeapon.reloadTime);

        int ammoToTake = Mathf.Min(ammoNeeded, availableAmmo);

        if (playerInventory.PrimaryInventorySystem.ConsumeAmmo(currentWeapon.requiredAmmoType, ammoToTake))
        {
            currentAmmo += ammoToTake;

            if (!string.IsNullOrEmpty(currentWeaponSlotID))
            {
                WeaponAmmoTracker.SetAmmo(currentWeaponSlotID, currentAmmo);
            }

            Debug.Log($"Reloaded {currentWeapon.Name}, Current Ammo: {currentAmmo}/{currentWeapon.magazineSize}, Remaining in inventory: {playerInventory.PrimaryInventorySystem.GetAmmoCount(currentWeapon.requiredAmmoType)}");
        }
        else
        {
            Debug.LogError("Failed to consume ammo from inventory!");
        }

        isReloading = false;
    }

    private IEnumerator StopWeaponAudioDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (weaponAudio != null && !isFiring)
            weaponAudio.Stop();
    }

    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => currentWeapon?.magazineSize ?? 0;

    public int GetInventoryAmmo()
    {
        if (currentWeapon == null) return 0;

        var playerInventory = FindObjectOfType<PlayerInventoryHolder>();
        if (playerInventory == null) return 0;

        return playerInventory.PrimaryInventorySystem.GetAmmoCount(currentWeapon.requiredAmmoType);
    }
}
