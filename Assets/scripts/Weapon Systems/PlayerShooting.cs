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

    // Grenade Launcher System
    private GrenadeLauncherData currentGrenadeLauncher;
    private int currentGrenadeAmmo;
    private bool isGrenadeLauncherMode = false;
    private bool isReloadingGrenadeLauncher = false;
    private float nextGrenadeLauncherFireTime;
    private float lastToggleTime = 0f;
    private const float toggleCooldown = 0.3f;

    // Underbarrel Shotgun System
    private UnderbarrelShotgunData currentUnderbarrelShotgun;
    private int currentUnderbarrelShotgunAmmo;
    private bool isUnderbarrelShotgunMode = false;
    private bool isReloadingUnderbarrelShotgun = false;
    private float nextUnderbarrelShotgunFireTime;

    // Generic underbarrel mode flag
    private bool IsUnderbarrelMode => isGrenadeLauncherMode || isUnderbarrelShotgunMode;

    // Modifier System - now just passes data to bullets
    private ModifierData currentModifier;

    private void Update()
    {
        recoil = transform.Find("cameraHolder/CameraRecoil").GetComponent<Recoil>();

        if (isFiring && currentWeapon != null)
        {
            if (isGrenadeLauncherMode && currentGrenadeLauncher != null)
            {
                if (Time.time >= nextGrenadeLauncherFireTime)
                {
                    FireGrenade();
                    nextGrenadeLauncherFireTime = Time.time + currentGrenadeLauncher.fireRate;
                    isFiring = false;
                }
            }
            else if (isUnderbarrelShotgunMode && currentUnderbarrelShotgun != null)
            {
                if (Time.time >= nextUnderbarrelShotgunFireTime)
                {
                    FireUnderbarrelShotgun();
                    nextUnderbarrelShotgunFireTime = Time.time + currentUnderbarrelShotgun.fireRate;
                    isFiring = false;
                }
            }
            else
            {
                if (Time.time >= nextFireTime)
                {
                    Fire();

                    float fireRate = currentAttachmentSystem != null
                        ? currentAttachmentSystem.CurrentFireRate
                        : currentWeapon.fireRate;
                    nextFireTime = Time.time + fireRate;

                    if (currentFireMode != FireMode.FullAuto)
                        isFiring = false;
                }
            }
        }
    }

    private void ReportGunshot()
    {
        if (firePoint != null)
        {
            // Use the NPCManager's static method instead of GunshotDetectionSystem
            NPCManager.NotifyGunshotFired(firePoint.position);
        }
    }

    public void EquipWeapon(WeaponData weapon)
    {
        EquipWeapon(weapon, null);
    }

    public void EquipWeapon(WeaponData weapon, string slotID)
    {
        if (currentWeapon != null && !string.IsNullOrEmpty(currentWeaponSlotID))
        {
            WeaponAmmoTracker.SetAmmo(currentWeaponSlotID, currentAmmo);
            if (currentGrenadeLauncher != null)
            {
                WeaponAmmoTracker.SetAmmo(currentWeaponSlotID + "_GL", currentGrenadeAmmo);
            }
            if (currentUnderbarrelShotgun != null)
            {
                WeaponAmmoTracker.SetAmmo(currentWeaponSlotID + "_UBS", currentUnderbarrelShotgunAmmo);
            }
        }

        currentWeapon = weapon;
        currentWeaponSlotID = slotID;
        currentAttachmentSystem = null;
        currentGrenadeLauncher = null;
        currentUnderbarrelShotgun = null;
        currentModifier = null;
        isGrenadeLauncherMode = false;
        isUnderbarrelShotgunMode = false;

        if (weapon == null)
        {
            currentAmmo = 0;
            currentGrenadeAmmo = 0;
            currentUnderbarrelShotgunAmmo = 0;
            return;
        }

        currentFireMode = weapon.fireMode;

        if (firePoint != null)
        {
            weaponAudio = firePoint.GetComponent<AudioSource>();
            if (weaponAudio == null)
                weaponAudio = firePoint.gameObject.AddComponent<AudioSource>();

            AudioClip clipToUse = GetCurrentShootSound();
            weaponAudio.clip = clipToUse;
            weaponAudio.loop = currentFireMode == FireMode.FullAuto;
            weaponAudio.playOnAwake = false;
            weaponAudio.spatialBlend = 1f;
        }

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
            recoilScript.SetAttachmentSystem(attachmentSystem);
        }

        UpdateGrenadeLauncherFromAttachments();
        UpdateModifierFromAttachments();

        if (attachmentSystem != null && currentWeapon != null)
        {
            int newMaxMagazine = attachmentSystem.CurrentMagazineSize;
            int oldMaxMagazine = currentWeapon.magazineSize;

            Debug.Log($"[SetAttachmentSystem] Magazine capacity changed from {oldMaxMagazine} to {newMaxMagazine}");

            if (newMaxMagazine > oldMaxMagazine && currentAmmo == oldMaxMagazine)
            {
                currentAmmo = newMaxMagazine;
                Debug.Log($"[SetAttachmentSystem] Filled magazine to new capacity: {currentAmmo}");
            }
            else if (newMaxMagazine < oldMaxMagazine && currentAmmo > newMaxMagazine)
            {
                currentAmmo = newMaxMagazine;
                Debug.Log($"[SetAttachmentSystem] Capped ammo to new magazine capacity: {currentAmmo}");
            }

            if (!string.IsNullOrEmpty(currentWeaponSlotID))
            {
                WeaponAmmoTracker.SetAmmo(currentWeaponSlotID, currentAmmo);
            }
        }
    }

    private void UpdateModifierFromAttachments()
    {
        Debug.Log($"[UpdateModifierFromAttachments] Called");

        if (currentAttachmentSystem == null)
        {
            Debug.Log($"[UpdateModifierFromAttachments] No attachment system, clearing modifier");
            currentModifier = null;
            return;
        }

        var modifierAttachment = currentAttachmentSystem.equippedAttachments.Find(a => a.type == AttachmentType.Modifier);

        if (modifierAttachment == null || modifierAttachment.modifierData == null)
        {
            Debug.Log($"[UpdateModifierFromAttachments] No modifier attachment found");
            currentModifier = null;
            return;
        }

        currentModifier = modifierAttachment.modifierData;

        Debug.Log($"Modifier equipped:");
        if (currentModifier.antiGravityRounds)
        {
            Debug.Log($"  - Anti-Gravity Rounds (Force: {currentModifier.antiGravityForce}, Duration: {currentModifier.antiGravityDuration}s)");
        }
    }

    private void UpdateGrenadeLauncherFromAttachments()
    {
        Debug.Log($"[UpdateGrenadeLauncherFromAttachments] Called");
        Debug.Log($"[UpdateGrenadeLauncherFromAttachments] currentAttachmentSystem: {(currentAttachmentSystem != null ? "EXISTS" : "NULL")}");

        if (currentAttachmentSystem == null)
        {
            Debug.Log($"[UpdateGrenadeLauncherFromAttachments] No attachment system, clearing underbarrel weapons");
            currentGrenadeLauncher = null;
            currentGrenadeAmmo = 0;
            isGrenadeLauncherMode = false;
            currentUnderbarrelShotgun = null;
            currentUnderbarrelShotgunAmmo = 0;
            isUnderbarrelShotgunMode = false;
            return;
        }

        Debug.Log($"[UpdateGrenadeLauncherFromAttachments] Equipped attachments count: {currentAttachmentSystem.equippedAttachments.Count}");
        foreach (var att in currentAttachmentSystem.equippedAttachments)
        {
            Debug.Log($"[UpdateGrenadeLauncherFromAttachments]   - {att.name} (Type: {att.type}, HasGL: {att.grenadeLauncherData != null}, HasUBS: {att.underbarrelShotgunData != null})");
        }

        var underbarrelAttachment = currentAttachmentSystem.equippedAttachments.Find(a => a.type == AttachmentType.Underbarrel);

        if (underbarrelAttachment == null)
        {
            Debug.Log($"[UpdateGrenadeLauncherFromAttachments] No underbarrel attachment found");
            currentGrenadeLauncher = null;
            currentGrenadeAmmo = 0;
            isGrenadeLauncherMode = false;
            currentUnderbarrelShotgun = null;
            currentUnderbarrelShotgunAmmo = 0;
            isUnderbarrelShotgunMode = false;
            return;
        }

        if (underbarrelAttachment.grenadeLauncherData != null)
        {
            currentGrenadeLauncher = underbarrelAttachment.grenadeLauncherData;
            currentUnderbarrelShotgun = null;
            currentUnderbarrelShotgunAmmo = 0;
            isUnderbarrelShotgunMode = false;

            if (!string.IsNullOrEmpty(currentWeaponSlotID))
            {
                currentGrenadeAmmo = WeaponAmmoTracker.GetAmmo(
                    currentWeaponSlotID + "_GL",
                    currentGrenadeLauncher.magazineSize
                );
            }
            else
            {
                currentGrenadeAmmo = currentGrenadeLauncher.magazineSize;
            }

            Debug.Log($"Grenade Launcher equipped: {currentGrenadeLauncher.launcherName} - Ammo: {currentGrenadeAmmo}/{currentGrenadeLauncher.magazineSize}");
        }
        else if (underbarrelAttachment.underbarrelShotgunData != null)
        {
            currentUnderbarrelShotgun = underbarrelAttachment.underbarrelShotgunData;
            currentGrenadeLauncher = null;
            currentGrenadeAmmo = 0;
            isGrenadeLauncherMode = false;

            if (!string.IsNullOrEmpty(currentWeaponSlotID))
            {
                currentUnderbarrelShotgunAmmo = WeaponAmmoTracker.GetAmmo(
                    currentWeaponSlotID + "_UBS",
                    currentUnderbarrelShotgun.magazineSize
                );
            }
            else
            {
                currentUnderbarrelShotgunAmmo = currentUnderbarrelShotgun.magazineSize;
            }

            Debug.Log($"Underbarrel Shotgun equipped: {currentUnderbarrelShotgun.shotgunName} - Ammo: {currentUnderbarrelShotgunAmmo}/{currentUnderbarrelShotgun.magazineSize}");
        }
        else
        {
            Debug.Log($"Regular underbarrel attachment equipped: {underbarrelAttachment.name}");
            currentGrenadeLauncher = null;
            currentGrenadeAmmo = 0;
            isGrenadeLauncherMode = false;
            currentUnderbarrelShotgun = null;
            currentUnderbarrelShotgunAmmo = 0;
            isUnderbarrelShotgunMode = false;
        }
    }

    public void ToggleGrenadeLauncher(InputAction.CallbackContext context)
    {
        if (Time.time - lastToggleTime < toggleCooldown)
        {
            return;
        }

        if (currentGrenadeLauncher == null && currentUnderbarrelShotgun == null)
        {
            Debug.Log("No underbarrel weapon attached!");
            return;
        }

        lastToggleTime = Time.time;

        if (isGrenadeLauncherMode || isUnderbarrelShotgunMode)
        {
            isGrenadeLauncherMode = false;
            isUnderbarrelShotgunMode = false;
            Debug.Log($"Switched to Primary Weapon mode - Ammo: {currentAmmo}/{GetMaxAmmo()}");
        }
        else
        {
            if (currentGrenadeLauncher != null)
            {
                isGrenadeLauncherMode = true;
                isUnderbarrelShotgunMode = false;
                Debug.Log($"Switched to Grenade Launcher mode - Ammo: {currentGrenadeAmmo}/{currentGrenadeLauncher.magazineSize}");
            }
            else if (currentUnderbarrelShotgun != null)
            {
                isUnderbarrelShotgunMode = true;
                isGrenadeLauncherMode = false;
                Debug.Log($"Switched to Underbarrel Shotgun mode - Ammo: {currentUnderbarrelShotgunAmmo}/{currentUnderbarrelShotgun.magazineSize}");
            }
        }

        UpdateWeaponAudio();
    }

    private void UpdateWeaponAudio()
    {
        if (weaponAudio == null) return;

        if (isGrenadeLauncherMode && currentGrenadeLauncher != null)
        {
            weaponAudio.clip = currentGrenadeLauncher.launchSound;
            weaponAudio.loop = false;
        }
        else if (isUnderbarrelShotgunMode && currentUnderbarrelShotgun != null)
        {
            weaponAudio.clip = currentUnderbarrelShotgun.shootSound;
            weaponAudio.loop = false;
        }
        else if (currentWeapon != null)
        {
            AudioClip clipToUse = GetCurrentShootSound();
            weaponAudio.clip = clipToUse;
            weaponAudio.loop = currentFireMode == FireMode.FullAuto;
        }
    }

    public void SetUIMode(bool uiMode)
    {
        isUIMode = uiMode;
        if (isUIMode)
        {
            StopFiring();
        }
    }

    public void StartFiring()
    {
        if (isGrenadeLauncherMode && currentGrenadeLauncher != null)
        {
            if (Time.time < nextGrenadeLauncherFireTime) return;
            FireGrenade();
            nextGrenadeLauncherFireTime = Time.time + currentGrenadeLauncher.fireRate;
            return;
        }

        if (isUnderbarrelShotgunMode && currentUnderbarrelShotgun != null)
        {
            if (Time.time < nextUnderbarrelShotgunFireTime) return;
            FireUnderbarrelShotgun();
            nextUnderbarrelShotgunFireTime = Time.time + currentUnderbarrelShotgun.fireRate;
            return;
        }

        if (currentWeapon == null) return;
        if (Time.time < nextFireTime) return;

        if (currentFireMode == FireMode.SemiAuto || currentFireMode == FireMode.Shotgun || currentFireMode == FireMode.Rocket)
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

        if (weaponAudio != null && currentWeapon != null && currentFireMode == FireMode.FullAuto && !isGrenadeLauncherMode)
            StartCoroutine(StopWeaponAudioDelayed(currentWeapon.ShootingSoundDelay));
    }

    public void SwitchFireMode(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (IsUnderbarrelMode)
        {
            Debug.Log("Cannot switch fire mode while in underbarrel weapon mode");
            return;
        }

        if (currentWeapon == null) return;

        if (!currentWeapon.canSwitchFireMode)
        {
            Debug.Log($"{currentWeapon.Name} does not support fire mode switching");
            return;
        }

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

        UpdateWeaponAudio();
    }

    private void FireGrenade()
    {
        if (isReloadingGrenadeLauncher || currentGrenadeAmmo <= 0)
        {
            Debug.Log("Out of grenades! Reload first.");
            return;
        }

        currentGrenadeAmmo--;
        ReportGunshot();

        if (!string.IsNullOrEmpty(currentWeaponSlotID))
        {
            WeaponAmmoTracker.SetAmmo(currentWeaponSlotID + "_GL", currentGrenadeAmmo);
        }

        if (weaponAudio != null && currentGrenadeLauncher.launchSound != null)
        {
            weaponAudio.PlayOneShot(currentGrenadeLauncher.launchSound);
        }

        if (currentGrenadeLauncher.grenadePrefab && firePoint && playerCamera)
        {
            GameObject grenadeObj = Instantiate(currentGrenadeLauncher.grenadePrefab, firePoint.position, Quaternion.identity);

            // Use direct camera forward direction
            Vector3 shootDirection = playerCamera.transform.forward;
            grenadeObj.transform.forward = shootDirection;

            GrenadeProjectile grenade = grenadeObj.GetComponent<GrenadeProjectile>();
            if (grenade == null)
            {
                grenade = grenadeObj.AddComponent<GrenadeProjectile>();
            }
            grenade.grenadeLauncherData = currentGrenadeLauncher;

            Rigidbody rb = grenadeObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(shootDirection * currentGrenadeLauncher.grenadeSpeed, ForceMode.Impulse);
            }
        }

        if (currentGrenadeLauncher.muzzleFlashPrefab)
            Instantiate(currentGrenadeLauncher.muzzleFlashPrefab, firePoint.position, firePoint.rotation);

        if (recoil != null)
        {
            float oldX = currentWeapon.recoilX;
            float oldY = currentWeapon.recoilY;
            float oldZ = currentWeapon.recoilZ;

            currentWeapon.recoilX = currentGrenadeLauncher.recoilX;
            currentWeapon.recoilY = currentGrenadeLauncher.recoilY;
            currentWeapon.recoilZ = currentGrenadeLauncher.recoilZ;

            recoil.RecoilFire();

            currentWeapon.recoilX = oldX;
            currentWeapon.recoilY = oldY;
            currentWeapon.recoilZ = oldZ;
        }

        Debug.Log($"Fired grenade! Remaining: {currentGrenadeAmmo}/{currentGrenadeLauncher.magazineSize}");
    }

    private void FireUnderbarrelShotgun()
    {
        if (isReloadingUnderbarrelShotgun || currentUnderbarrelShotgunAmmo <= 0)
        {
            ReportGunshot();
            Debug.Log("Out of shotgun shells! Reload first.");
            return;
        }

        currentUnderbarrelShotgunAmmo--;

        if (!string.IsNullOrEmpty(currentWeaponSlotID))
        {
            WeaponAmmoTracker.SetAmmo(currentWeaponSlotID + "_UBS", currentUnderbarrelShotgunAmmo);
        }

        if (weaponAudio != null && currentUnderbarrelShotgun.shootSound != null)
        {
            weaponAudio.PlayOneShot(currentUnderbarrelShotgun.shootSound);
        }

        for (int i = 0; i < currentUnderbarrelShotgun.pelletsPerShot; i++)
        {
            if (bulletPrefab && firePoint && playerCamera)
            {
                GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

                // Use direct camera forward direction
                Vector3 shootDirection = playerCamera.transform.forward;

                // Apply shotgun spread
                float spreadX = Random.Range(-currentUnderbarrelShotgun.spreadAngle, currentUnderbarrelShotgun.spreadAngle);
                float spreadY = Random.Range(-currentUnderbarrelShotgun.spreadAngle, currentUnderbarrelShotgun.spreadAngle);
                Quaternion spreadRot = Quaternion.Euler(spreadY, spreadX, 0);
                shootDirection = spreadRot * shootDirection;

                shootDirection.Normalize();
                bulletObj.transform.forward = shootDirection;

                Bullet bullet = bulletObj.GetComponent<Bullet>();
                if (bullet != null)
                {
                    bullet.weaponData = currentWeapon;
                    if (currentWeapon != null)
                    {
                        currentWeapon.damage = currentUnderbarrelShotgun.damage;
                    }
                }

                Rigidbody rb = bulletObj.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.AddForce(shootDirection * currentUnderbarrelShotgun.bulletSpeed, ForceMode.Impulse);
            }
        }

        if (currentUnderbarrelShotgun.muzzleFlashPrefab)
            Instantiate(currentUnderbarrelShotgun.muzzleFlashPrefab, firePoint.position, firePoint.rotation);

        if (recoil != null)
        {
            float oldX = currentWeapon.recoilX;
            float oldY = currentWeapon.recoilY;
            float oldZ = currentWeapon.recoilZ;

            currentWeapon.recoilX = currentUnderbarrelShotgun.recoilX;
            currentWeapon.recoilY = currentUnderbarrelShotgun.recoilY;
            currentWeapon.recoilZ = currentUnderbarrelShotgun.recoilZ;

            recoil.RecoilFire();

            currentWeapon.recoilX = oldX;
            currentWeapon.recoilY = oldY;
            currentWeapon.recoilZ = oldZ;
        }

        Debug.Log($"Fired underbarrel shotgun! Remaining: {currentUnderbarrelShotgunAmmo}/{currentUnderbarrelShotgun.magazineSize}");
    }

    private void Fire()
    {
        if (isReloading || currentAmmo <= 0)
        {
            Debug.Log("Out of ammo! Reload first.");
            return;
        }

        currentAmmo--;
        ReportGunshot();

        if (!string.IsNullOrEmpty(currentWeaponSlotID))
        {
            WeaponAmmoTracker.SetAmmo(currentWeaponSlotID, currentAmmo);
        }

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

        if (currentFireMode == FireMode.Rocket)
        {
            FireRocket();
            recoil.RecoilFire();
            return;
        }

        int pellets = currentFireMode == FireMode.Shotgun ? currentWeapon.pelletsPerShot : 1;

        for (int i = 0; i < pellets; i++)
        {
            if (bulletPrefab && firePoint && playerCamera)
            {
                GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

                // Use direct camera forward direction
                Vector3 shootDirection = playerCamera.transform.forward;

                // Apply spread based on fire mode
                if (currentFireMode == FireMode.Shotgun)
                {
                    float spreadX = Random.Range(-currentWeapon.spreadAngle, currentWeapon.spreadAngle);
                    float spreadY = Random.Range(-currentWeapon.spreadAngle, currentWeapon.spreadAngle);
                    Quaternion spreadRot = Quaternion.Euler(spreadY, spreadX, 0);
                    shootDirection = spreadRot * shootDirection;
                }
                else
                {
                    float spread = currentAttachmentSystem != null
                        ? currentAttachmentSystem.CurrentSpread
                        : currentWeapon.spread;

                    float spreadX = Random.Range(-spread, spread);
                    float spreadY = Random.Range(-spread, spread);
                    shootDirection += playerCamera.transform.up * spreadY + playerCamera.transform.right * spreadX;
                }

                shootDirection.Normalize();
                bulletObj.transform.forward = shootDirection;

                Bullet bullet = bulletObj.GetComponent<Bullet>();
                if (bullet != null)
                {
                    bullet.weaponData = currentWeapon;

                    if (currentAttachmentSystem != null)
                    {
                        currentWeapon.damage = currentAttachmentSystem.CurrentDamage;
                    }
                }

                // PASS MODIFIER TO BULLET
                BulletModifier bulletModifier = bulletObj.GetComponent<BulletModifier>();
                if (bulletModifier != null && currentModifier != null)
                {
                    // Pass both modifier data AND weapon damage
                    float currentDamage = currentAttachmentSystem != null ? currentAttachmentSystem.CurrentDamage : currentWeapon.damage;
                    bulletModifier.Initialize(currentModifier, currentDamage);
                }

                Rigidbody rb = bulletObj.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.AddForce(shootDirection * currentWeapon.bulletSpeed, ForceMode.Impulse);
            }
        }

       
        if (currentWeapon.muzzleFlashPrefab)
            Instantiate(currentWeapon.muzzleFlashPrefab, firePoint.position, firePoint.rotation);

        recoil.RecoilFire();

        

    }

    private void FireRocket()
    {
        if (firePoint == null || playerCamera == null) return;

        GameObject projectilePrefab = currentWeapon.rocketPrefab != null ? currentWeapon.rocketPrefab : bulletPrefab;

        if (projectilePrefab == null)
        {
            Debug.LogWarning("No rocket or bullet prefab assigned!");
            return;
        }

        GameObject rocketObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        // Use direct camera forward direction
        Vector3 shootDirection = playerCamera.transform.forward;
        rocketObj.transform.forward = shootDirection;

        RocketProjectile rocket = rocketObj.GetComponent<RocketProjectile>();
        if (rocket != null)
        {
            rocket.weaponData = currentWeapon;
        }
        else
        {
            rocket = rocketObj.AddComponent<RocketProjectile>();
            rocket.weaponData = currentWeapon;
        }

        Rigidbody rb = rocketObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(shootDirection * currentWeapon.bulletSpeed, ForceMode.Impulse);
        }

        if (currentWeapon.muzzleFlashPrefab)
            Instantiate(currentWeapon.muzzleFlashPrefab, firePoint.position, firePoint.rotation);
    }

    public void Reload()
    {
        if (isGrenadeLauncherMode && currentGrenadeLauncher != null)
        {
            ReloadGrenadeLauncher();
            return;
        }

        if (isUnderbarrelShotgunMode && currentUnderbarrelShotgun != null)
        {
            ReloadUnderbarrelShotgun();
            return;
        }

        if (currentWeapon == null || isReloading) return;

        int maxMagazine = currentAttachmentSystem != null
            ? currentAttachmentSystem.CurrentMagazineSize
            : currentWeapon.magazineSize;

        if (currentAmmo == maxMagazine)
        {
            Debug.Log("Magazine is already full!");
            return;
        }

        StartCoroutine(ReloadCoroutine());
    }

    private void ReloadGrenadeLauncher()
    {
        if (isReloadingGrenadeLauncher) return;

        if (currentGrenadeAmmo == currentGrenadeLauncher.magazineSize)
        {
            Debug.Log("Grenade launcher is already full!");
            return;
        }

        StartCoroutine(ReloadGrenadeLauncherCoroutine());
    }

    private IEnumerator ReloadGrenadeLauncherCoroutine()
    {
        isReloadingGrenadeLauncher = true;

        var playerInventory = FindObjectOfType<PlayerInventoryHolder>();
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventoryHolder not found!");
            isReloadingGrenadeLauncher = false;
            yield break;
        }

        int ammoNeeded = currentGrenadeLauncher.magazineSize - currentGrenadeAmmo;
        int availableAmmo = playerInventory.PrimaryInventorySystem.GetAmmoCount(currentGrenadeLauncher.requiredAmmoType);

        if (availableAmmo <= 0)
        {
            Debug.Log($"No {currentGrenadeLauncher.requiredAmmoType} ammo in inventory!");
            isReloadingGrenadeLauncher = false;
            yield break;
        }

        Debug.Log($"Reloading {currentGrenadeLauncher.launcherName}... Available ammo: {availableAmmo}");

        if (currentGrenadeLauncher.reloadSound)
            AudioSource.PlayClipAtPoint(currentGrenadeLauncher.reloadSound, firePoint.position);

        yield return new WaitForSeconds(currentGrenadeLauncher.reloadTime);

        int ammoToTake = Mathf.Min(ammoNeeded, availableAmmo);

        if (playerInventory.PrimaryInventorySystem.ConsumeAmmo(currentGrenadeLauncher.requiredAmmoType, ammoToTake))
        {
            currentGrenadeAmmo += ammoToTake;

            if (!string.IsNullOrEmpty(currentWeaponSlotID))
            {
                WeaponAmmoTracker.SetAmmo(currentWeaponSlotID + "_GL", currentGrenadeAmmo);
            }

            Debug.Log($"Reloaded {currentGrenadeLauncher.launcherName}, Current Ammo: {currentGrenadeAmmo}/{currentGrenadeLauncher.magazineSize}");
        }
        else
        {
            Debug.LogError("Failed to consume grenade ammo from inventory!");
        }

        isReloadingGrenadeLauncher = false;
    }

    private void ReloadUnderbarrelShotgun()
    {
        if (isReloadingUnderbarrelShotgun) return;

        if (currentUnderbarrelShotgunAmmo == currentUnderbarrelShotgun.magazineSize)
        {
            Debug.Log("Underbarrel shotgun is already full!");
            return;
        }

        StartCoroutine(ReloadUnderbarrelShotgunCoroutine());
    }

    private IEnumerator ReloadUnderbarrelShotgunCoroutine()
    {
        isReloadingUnderbarrelShotgun = true;

        var playerInventory = FindObjectOfType<PlayerInventoryHolder>();
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventoryHolder not found!");
            isReloadingUnderbarrelShotgun = false;
            yield break;
        }

        int ammoNeeded = currentUnderbarrelShotgun.magazineSize - currentUnderbarrelShotgunAmmo;
        int availableAmmo = playerInventory.PrimaryInventorySystem.GetAmmoCount(currentUnderbarrelShotgun.requiredAmmoType);

        if (availableAmmo <= 0)
        {
            Debug.Log($"No {currentUnderbarrelShotgun.requiredAmmoType} ammo in inventory!");
            isReloadingUnderbarrelShotgun = false;
            yield break;
        }

        Debug.Log($"Reloading {currentUnderbarrelShotgun.shotgunName}... Available ammo: {availableAmmo}");

        if (currentUnderbarrelShotgun.reloadSound)
            AudioSource.PlayClipAtPoint(currentUnderbarrelShotgun.reloadSound, firePoint.position);

        yield return new WaitForSeconds(currentUnderbarrelShotgun.reloadTime);

        int ammoToTake = Mathf.Min(ammoNeeded, availableAmmo);

        if (playerInventory.PrimaryInventorySystem.ConsumeAmmo(currentUnderbarrelShotgun.requiredAmmoType, ammoToTake))
        {
            currentUnderbarrelShotgunAmmo += ammoToTake;

            if (!string.IsNullOrEmpty(currentWeaponSlotID))
            {
                WeaponAmmoTracker.SetAmmo(currentWeaponSlotID + "_UBS", currentUnderbarrelShotgunAmmo);
            }

            Debug.Log($"Reloaded {currentUnderbarrelShotgun.shotgunName}, Current Ammo: {currentUnderbarrelShotgunAmmo}/{currentUnderbarrelShotgun.magazineSize}");
        }
        else
        {
            Debug.LogError("Failed to consume shotgun ammo from inventory!");
        }

        isReloadingUnderbarrelShotgun = false;
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

    public int GetCurrentAmmo()
    {
        if (isGrenadeLauncherMode && currentGrenadeLauncher != null)
            return currentGrenadeAmmo;
        if (isUnderbarrelShotgunMode && currentUnderbarrelShotgun != null)
            return currentUnderbarrelShotgunAmmo;
        return currentAmmo;
    }

    public int GetMaxAmmo()
    {
        if (isGrenadeLauncherMode && currentGrenadeLauncher != null)
            return currentGrenadeLauncher.magazineSize;
        if (isUnderbarrelShotgunMode && currentUnderbarrelShotgun != null)
            return currentUnderbarrelShotgun.magazineSize;

        return currentAttachmentSystem != null
            ? currentAttachmentSystem.CurrentMagazineSize
            : currentWeapon?.magazineSize ?? 0;
    }

    public int GetInventoryAmmo()
    {
        if (isGrenadeLauncherMode && currentGrenadeLauncher != null)
        {
            var playerInventory = FindObjectOfType<PlayerInventoryHolder>();
            if (playerInventory == null) return 0;
            return playerInventory.PrimaryInventorySystem.GetAmmoCount(currentGrenadeLauncher.requiredAmmoType);
        }

        if (isUnderbarrelShotgunMode && currentUnderbarrelShotgun != null)
        {
            var playerInventory = FindObjectOfType<PlayerInventoryHolder>();
            if (playerInventory == null) return 0;
            return playerInventory.PrimaryInventorySystem.GetAmmoCount(currentUnderbarrelShotgun.requiredAmmoType);
        }

        if (currentWeapon == null) return 0;

        var inventory = FindObjectOfType<PlayerInventoryHolder>();
        if (inventory == null) return 0;

        return inventory.PrimaryInventorySystem.GetAmmoCount(currentWeapon.requiredAmmoType);
    }

    public FireMode GetCurrentFireMode() => currentFireMode;

    public bool IsGrenadeLauncherMode() => isGrenadeLauncherMode;

    public bool HasGrenadeLauncher() => currentGrenadeLauncher != null;

    public bool HasUnderbarrelShotgun() => currentUnderbarrelShotgun != null;

    public bool HasUnderbarrelWeapon() => currentGrenadeLauncher != null || currentUnderbarrelShotgun != null;

    public bool HasModifier() => currentModifier != null;

    public ModifierData GetCurrentModifier() => currentModifier;

    private AudioClip GetCurrentShootSound()
    {
        if (currentWeapon == null) return null;

        if (currentFireMode == FireMode.FullAuto && currentWeapon.shootSoundFullAuto != null)
        {
            return currentWeapon.shootSoundFullAuto;
        }

        return currentWeapon.shootSound;
    }
}