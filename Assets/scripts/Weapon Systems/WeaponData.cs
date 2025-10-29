using System.Collections.Generic;
using UnityEngine;

public enum FireMode
{
    SemiAuto,
    FullAuto,
    Burst,
    Shotgun
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
    public AmmoType requiredAmmoType; // What ammo type this weapon uses

    [Header("Shotgun Settings")]
    public int pelletsPerShot = 8;
    public float spreadAngle = 5f;

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

    /// <summary>
    /// Gets all parts that should be disabled when a sight is attached.
    /// Combines the new list with the legacy single path for backwards compatibility.
    /// </summary>
    public List<string> GetPartsToDisableWithSight()
    {
        List<string> allParts = new List<string>();

        // Add all parts from the new list
        if (partsToDisableWithSight != null && partsToDisableWithSight.Count > 0)
        {
            allParts.AddRange(partsToDisableWithSight);
        }

        // Add legacy path if it exists and isn't already in the list
        if (!string.IsNullOrEmpty(partToDisableWithSightPath) && !allParts.Contains(partToDisableWithSightPath))
        {
            allParts.Add(partToDisableWithSightPath);
        }

        return allParts;
    }

    /// <summary>
    /// Check if a specific attachment is allowed on this weapon.
    /// If allowedAttachments list is empty, all attachments of allowed slot types are permitted.
    /// </summary>
    public bool IsAttachmentAllowed(AttachmentData attachment)
    {
        if (attachment == null)
            return false;

        // If no specific attachments are configured, allow all attachments of the allowed slot types
        if (allowedAttachments == null || allowedAttachments.Count == 0)
        {
            // Check if the attachment type is in the allowed slots
            return allowedAttachmentSlots != null && allowedAttachmentSlots.Contains(attachment.type);
        }

        // If specific attachments are configured, only allow those
        return allowedAttachments.Contains(attachment);
    }
}