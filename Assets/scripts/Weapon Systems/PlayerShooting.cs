using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    public static PlayerShooting Instance;

    private void Awake() => Instance = this;

    public Transform firePoint;
    public GameObject bulletPrefab;
    public Camera playerCamera;

    public Recoil recoilScript;

    private WeaponData currentWeapon;
    private WeaponAttachmentSystem currentAttachmentSystem;
    private string currentWeaponSlotID;
    private int currentAmmo;
    private bool isReloading;
    private float nextFireTime;
    private bool isUIMode = false;

    private AudioSource weaponAudio;
    private bool isFiring;

    private Recoil recoil;

    // Fire mode switching
    private FireMode currentFireMode;

    private void Update()
    {
        recoil = transform.Find("cameraHolder/CameraRecoil").GetComponent<Recoil>();

        if (isFiring && currentWeapon != null)
        {
            if (Time.time >= nextFireTime)
            {
                Fire();

                // Use modified fire rate from attachments
                float fireRate = currentAttachmentSystem != null
                    ? currentAttachmentSystem.CurrentFireRate
                    : currentWeapon.fireRate;
                nextFireTime = Time.time + fireRate;

                if (currentFireMode != FireMode.FullAuto)
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
        currentAttachmentSystem = null;

        if (weapon == null)
        {
            currentAmmo = 0;
            return;
        }

        // Set initial fire mode
        currentFireMode = weapon.fireMode;

        if (firePoint != null)
        {
            weaponAudio = firePoint.GetComponent<AudioSource>();
            if (weaponAudio == null)
                weaponAudio = firePoint.gameObject.AddComponent<AudioSource>();

            // Use appropriate audio clip based on fire mode
            AudioClip clipToUse = GetCurrentShootSound();
            weaponAudio.clip = clipToUse;
            weaponAudio.loop = currentFireMode == FireMode.FullAuto;
            weaponAudio.playOnAwake = false;
            weaponAudio.spatialBlend = 1f;
        }

        // Load ammo for this specific weapon slot
        int maxMagazine = weapon.magazineSize;
        if (!string.IsNullOrEmpty(slotID))
        {
            currentAmmo = WeaponAmmoTracker.GetAmmo(slotID, maxMagazine);
        }
        else
        {
            currentAmmo = maxMagazine;
        }

        if (recoilScript != null)
            recoilScript.SetWeaponData(weapon);

        Debug.Log($"Equipped {weapon.Name} (Slot: {slotID}) - Ammo: {currentAmmo}/{maxMagazine} - Fire Mode: {currentFireMode}");
    }

    public void SetAttachmentSystem(WeaponAttachmentSystem attachmentSystem)
    {
        currentAttachmentSystem = attachmentSystem;

        if (recoilScript != null && attachmentSystem != null)
        {
            // Update recoil with attachment modifiers
            recoilScript.SetAttachmentSystem(attachmentSystem);
        }
    }

    public void SetUIMode(bool uiMode)
    {
        isUIMode = uiMode;
        if (isUIMode)
        {
            StopFiring(); // Stop firing if entering UI mode
        }
    }

    public void StartFiring()
    {
        if (currentWeapon == null) return;

        if (Time.time < nextFireTime) return;

        if (currentFireMode == FireMode.SemiAuto || currentFireMode == FireMode.Shotgun)
        {
            Fire();
            float fireRate = currentAttachmentSystem != null
                ? currentAttachmentSystem.CurrentFireRate
                : currentWeapon.fireRate;
            nextFireTime = Time.time + fireRate;
        }
        else
        {
            isFiring = true;
        }
    }

    public void StopFiring()
    {
        isFiring = false;

        if (weaponAudio != null && currentWeapon != null && currentFireMode == FireMode.FullAuto)
            StartCoroutine(StopWeaponAudioDelayed(currentWeapon.ShootingSoundDelay));
    }

    /// <summary>
    /// Switches between Semi-Auto and Full-Auto fire modes if the weapon supports it
    /// </summary>
    public void SwitchFireMode(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (currentWeapon == null) return;

        if (!currentWeapon.canSwitchFireMode)
        {
            Debug.Log($"{currentWeapon.Name} does not support fire mode switching");
            return;
        }

        // Only switch between Semi and Full Auto
        if (currentFireMode == FireMode.SemiAuto)
        {
            currentFireMode = FireMode.FullAuto;
            Debug.Log($"Fire mode switched to: Full Auto");
        }
        else if (currentFireMode == FireMode.FullAuto)
        {
            currentFireMode = FireMode.SemiAuto;
            Debug.Log($"Fire mode switched to: Semi Auto");
        }

        // Update audio settings for the new fire mode
        if (weaponAudio != null && currentWeapon != null)
        {
            AudioClip clipToUse = GetCurrentShootSound();
            weaponAudio.clip = clipToUse;
            weaponAudio.loop = currentFireMode == FireMode.FullAuto;
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

        // Save ammo after each shot
        if (!string.IsNullOrEmpty(currentWeaponSlotID))
        {
            WeaponAmmoTracker.SetAmmo(currentWeaponSlotID, currentAmmo);
        }

        // Play shooting sound
        if (weaponAudio != null)
        {
            AudioClip clipToUse = GetCurrentShootSound();

            if (currentFireMode == FireMode.FullAuto)
            {
                if (!weaponAudio.isPlaying)
                    weaponAudio.Play();
            }
            else
            {
                weaponAudio.PlayOneShot(clipToUse);
            }
        }

        // Determine number of bullets to fire
        int pellets = currentFireMode == FireMode.Shotgun ? currentWeapon.pelletsPerShot : 1;

        for (int i = 0; i < pellets; i++)
        {
            if (bulletPrefab && firePoint && playerCamera)
            {
                GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

                // Raycast from camera center to get accurate aim point
                Vector3 targetPoint;
                Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    // Hit something - aim at that point
                    targetPoint = hit.point;
                }
                else
                {
                    // Didn't hit anything - aim far away in that direction
                    targetPoint = ray.GetPoint(1000f);
                }

                // Calculate direction from firePoint to target
                Vector3 shootDirection = (targetPoint - firePoint.position).normalized;

                // Use modified spread from attachments
                float spread = currentAttachmentSystem != null
                    ? currentAttachmentSystem.CurrentSpread
                    : currentWeapon.spread;

                if (currentFireMode == FireMode.Shotgun)
                {
                    float spreadX = Random.Range(-currentWeapon.spreadAngle, currentWeapon.spreadAngle);
                    float spreadY = Random.Range(-currentWeapon.spreadAngle, currentWeapon.spreadAngle);
                    Quaternion spreadRot = Quaternion.Euler(spreadY, spreadX, 0);
                    shootDirection = spreadRot * shootDirection;
                }
                else
                {
                    float horizontalSpread = Random.Range(-spread, spread);
                    shootDirection += playerCamera.transform.right * horizontalSpread;
                }

                shootDirection.Normalize();
                bulletObj.transform.forward = shootDirection;

                // Assign WeaponData with modified damage from attachments
                Bullet bullet = bulletObj.GetComponent<Bullet>();
                if (bullet != null)
                {
                    bullet.weaponData = currentWeapon;

                    // Override damage if attachments are present
                    if (currentAttachmentSystem != null)
                    {
                        currentWeapon.damage = currentAttachmentSystem.CurrentDamage;
                    }
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

        // Use modified magazine size from attachments
        int maxMagazine = currentAttachmentSystem != null
            ? currentAttachmentSystem.CurrentMagazineSize
            : currentWeapon.magazineSize;

        // Check if magazine is already full
        if (currentAmmo == maxMagazine)
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

        int maxMagazine = currentAttachmentSystem != null
            ? currentAttachmentSystem.CurrentMagazineSize
            : currentWeapon.magazineSize;
        int ammoNeeded = maxMagazine - currentAmmo;
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

        // Use modified reload time from attachments
        float reloadTime = currentAttachmentSystem != null
            ? currentAttachmentSystem.CurrentReloadTime
            : currentWeapon.reloadTime;

        yield return new WaitForSeconds(reloadTime);

        int ammoToTake = Mathf.Min(ammoNeeded, availableAmmo);

        if (playerInventory.PrimaryInventorySystem.ConsumeAmmo(currentWeapon.requiredAmmoType, ammoToTake))
        {
            currentAmmo += ammoToTake;

            if (!string.IsNullOrEmpty(currentWeaponSlotID))
            {
                WeaponAmmoTracker.SetAmmo(currentWeaponSlotID, currentAmmo);
            }

            Debug.Log($"Reloaded {currentWeapon.Name}, Current Ammo: {currentAmmo}/{maxMagazine}, Remaining in inventory: {playerInventory.PrimaryInventorySystem.GetAmmoCount(currentWeapon.requiredAmmoType)}");
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
    public int GetMaxAmmo() => currentAttachmentSystem != null
        ? currentAttachmentSystem.CurrentMagazineSize
        : currentWeapon?.magazineSize ?? 0;

    public int GetInventoryAmmo()
    {
        if (currentWeapon == null) return 0;

        var playerInventory = FindObjectOfType<PlayerInventoryHolder>();
        if (playerInventory == null) return 0;

        return playerInventory.PrimaryInventorySystem.GetAmmoCount(currentWeapon.requiredAmmoType);
    }

    public FireMode GetCurrentFireMode() => currentFireMode;

    /// <summary>
    /// Gets the appropriate shoot sound based on current fire mode
    /// </summary>
    private AudioClip GetCurrentShootSound()
    {
        if (currentWeapon == null) return null;

        // If in full-auto and a separate full-auto sound exists, use it
        if (currentFireMode == FireMode.FullAuto && currentWeapon.shootSoundFullAuto != null)
        {
            return currentWeapon.shootSoundFullAuto;
        }

        // Otherwise use the default shoot sound
        return currentWeapon.shootSound;
    }
}