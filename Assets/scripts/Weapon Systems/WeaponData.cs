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

    [Header("Ammo Configuration")]
    public AmmoType requiredAmmoType; // What ammo type this weapon uses

    [Header("Shotgun Settings")]
    public int pelletsPerShot = 8;
    public float spreadAngle = 5f;

    [Header("Fire Settings")]
    public FireMode fireMode = FireMode.SemiAuto;

    [Header("Recoil & Accuracy")]
    public float recoilX = 1.5f;
    public float recoilY = 1.5f;
    public float recoilZ = 1.5f;
    public float spread = 0.05f;

    [Header("Attachment Configuration")]
    public List<AttachmentType> allowedAttachmentSlots;

    [Header("Attachment Model Management")]
    [Tooltip("List of child GameObject names or paths to disable when a scope/sight is attached (e.g., 'IronSights', 'Mesh/RearSight', 'FrontSightPost')")]
    public List<string> partsToDisableWithSight = new List<string>();

    [Header("Legacy Support (Deprecated)")]
    [Tooltip("DEPRECATED: Use partsToDisableWithSight list instead. This field is kept for backwards compatibility.")]
    public string partToDisableWithSightPath;

    [Header("3D Model")]
    public GameObject weaponPrefab;

    [Header("Audio & VFX")]
    public AudioClip shootSound;
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
}