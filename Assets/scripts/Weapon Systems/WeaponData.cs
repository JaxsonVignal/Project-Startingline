using System.Collections.Generic;
using UnityEngine;

public enum FireMode
{
    SemiAuto,
    FullAuto,
    Burst,
    Shotgun,
    Rocket
}

[System.Serializable]
public class ScopeOffsetData
{
    [Tooltip("The scope attachment")]
    public AttachmentData scope;

    [Tooltip("Position offset for this scope on this weapon")]
    public Vector3 positionOffset = Vector3.zero;

    [Tooltip("Rotation offset for this scope on this weapon")]
    public Vector3 rotationOffset = Vector3.zero;
}

[CreateAssetMenu(menuName = "Inventory System/Weapon")]
public class WeaponData : InventoryItemData
{
    public string weaponId = "";

    [Header("Weapon Stats")]
    public float damage = 30f;
    public float fireRate = 0.1f;
    public float reloadTime = 2.5f;
    public int magazineSize = 30;
    public int bulletSpeed = 50;
    public float FOVChange = 40;
    public float Value = 0;

    [Header("Ammo Configuration")]
    public AmmoType requiredAmmoType;

    [Header("Shotgun Settings")]
    public int pelletsPerShot = 8;
    public float spreadAngle = 5f;

    [Header("Rocket Settings")]
    [Tooltip("Explosion radius for rocket fire mode")]
    public float explosionRadius = 5f;
    [Tooltip("Explosion damage (uses weapon damage if set to 0)")]
    public float explosionDamage = 100f;
    [Tooltip("Explosion particle effect prefab")]
    public GameObject explosionEffectPrefab;
    [Tooltip("Explosion sound effect")]
    public AudioClip explosionSound;
    [Tooltip("Rocket projectile prefab (optional - uses bulletPrefab if not set)")]
    public GameObject rocketPrefab;

    [Header("Fire Settings")]
    public FireMode fireMode = FireMode.SemiAuto;
    [Tooltip("Enable this to allow switching between Semi-Auto and Full-Auto fire modes")]
    public bool canSwitchFireMode = false;

    [Header("Recoil & Accuracy")]
    public float recoilX = 1.5f;
    public float recoilY = 1.5f;
    public float recoilZ = 1.5f;
    public float spread = 0.05f;

    [Header("Attachment Configuration")]
    public List<AttachmentType> allowedAttachmentSlots;
    [Tooltip("List of specific attachments that can be equipped on this weapon. Leave empty to allow all attachments of the allowed slot types.")]
    public List<AttachmentData> allowedAttachments = new List<AttachmentData>();

    [Header("Scope/Sight ADS Offsets for This Weapon")]
    [Tooltip("List of scopes with their position and rotation offsets specific to this weapon")]
    public List<ScopeOffsetData> scopeOffsets = new List<ScopeOffsetData>();

    [Header("Attachment Model Management")]
    [Tooltip("List of child GameObject names or paths to disable when a scope/sight is attached (e.g., 'IronSights', 'Mesh/RearSight', 'FrontSightPost')")]
    public List<string> partsToDisableWithSight = new List<string>();

    [Header("Legacy Support (Deprecated)")]
    [Tooltip("DEPRECATED: Use partsToDisableWithSight list instead. This field is kept for backwards compatibility.")]
    public string partToDisableWithSightPath;

    [Header("3D Model")]
    public GameObject weaponPrefab;

    [Header("Audio & VFX")]
    [Tooltip("Sound for semi-auto fire (single shots). If shootSoundFullAuto is not set, this will be used for both modes.")]
    public AudioClip shootSound;
    [Tooltip("Optional: Different sound for full-auto fire (continuous/looping). Leave empty to use shootSound for both.")]
    public AudioClip shootSoundFullAuto;
    public AudioClip reloadSound;
    public GameObject muzzleFlashPrefab;
    public float ShootingSoundDelay;

    public override void UseItem()
    {
        Debug.Log($"Firing {Name}!");
    }

    public List<string> GetPartsToDisableWithSight()
    {
        List<string> allParts = new List<string>();
        if (partsToDisableWithSight != null && partsToDisableWithSight.Count > 0)
        {
            allParts.AddRange(partsToDisableWithSight);
        }
        if (!string.IsNullOrEmpty(partToDisableWithSightPath) && !allParts.Contains(partToDisableWithSightPath))
        {
            allParts.Add(partToDisableWithSightPath);
        }
        return allParts;
    }

    public bool IsAttachmentAllowed(AttachmentData attachment)
    {
        if (attachment == null)
            return false;
        if (allowedAttachments == null || allowedAttachments.Count == 0)
        {
            return allowedAttachmentSlots != null && allowedAttachmentSlots.Contains(attachment.type);
        }
        return allowedAttachments.Contains(attachment);
    }

    /// <summary>
    /// Gets the scope offset data for a specific attachment on this weapon
    /// </summary>
    public ScopeOffsetData GetScopeOffset(AttachmentData attachment)
    {
        if (scopeOffsets == null || attachment == null)
            return null;

        foreach (var offset in scopeOffsets)
        {
            if (offset.scope == attachment)
                return offset;
        }

        return null;
    }
}