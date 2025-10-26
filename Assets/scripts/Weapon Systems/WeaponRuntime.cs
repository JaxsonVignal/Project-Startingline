using UnityEngine;
using System.Collections.Generic;

public class WeaponRuntime : MonoBehaviour
{
    public WeaponInstance instance;
    public WeaponData weaponData; // assigned by InitFromInstance
    public WeaponAttachmentSystem attachmentSystem;

    // lookups: you must fill these at game start (AssetManager below helps)
    private Dictionary<string, AttachmentData> attachmentLookup;

    public void InitFromInstance(WeaponInstance inst, WeaponData baseWeapon, Dictionary<string, AttachmentData> attLookup)
    {
        instance = inst;
        weaponData = baseWeapon;
        attachmentLookup = attLookup;

        if (attachmentSystem == null) attachmentSystem = gameObject.GetComponent<WeaponAttachmentSystem>();
        if (attachmentSystem == null) attachmentSystem = gameObject.AddComponent<WeaponAttachmentSystem>();
        attachmentSystem.weaponData = weaponData;

        // clear old attachments
        attachmentSystem.ClearAll();

        // equip attachments in the saved order, applying transform overrides if present
        foreach (var entry in inst.attachments)
        {
            if (attachmentLookup != null && attachmentLookup.TryGetValue(entry.attachmentId, out var att))
            {
                // Pass both AttachmentData and WeaponAttachmentEntry
                attachmentSystem.EquipAttachment(att, entry);
            }
            else
            {
                Debug.LogWarning($"Attachment id {entry.attachmentId} not found in lookup.");
            }
        }

        // Handle iron sight visibility after all attachments are equipped
        UpdateIronSightVisibility();
    }

    /// <summary>
    /// Updates iron sight visibility based on whether a sight attachment is equipped
    /// </summary>
    private void UpdateIronSightVisibility()
    {
        if (weaponData == null || instance == null || attachmentLookup == null)
            return;

        // Check if weapon has a sight attachment equipped
        bool hasSightAttachment = false;
        foreach (var entry in instance.attachments)
        {
            if (attachmentLookup.TryGetValue(entry.attachmentId, out var att))
            {
                if (att.type == AttachmentType.Sight)
                {
                    hasSightAttachment = true;
                    break;
                }
            }
        }

        // Disable iron sights if a scope/sight is equipped
        if (hasSightAttachment && !string.IsNullOrEmpty(weaponData.partToDisableWithSightPath))
        {
            Transform partToDisable = transform.Find(weaponData.partToDisableWithSightPath);
            if (partToDisable != null)
            {
                partToDisable.gameObject.SetActive(false);
                Debug.Log($"[WeaponRuntime] Disabled iron sight part: {weaponData.partToDisableWithSightPath}");
            }
            else
            {
                Debug.LogWarning($"[WeaponRuntime] Could not find part to disable at path: {weaponData.partToDisableWithSightPath}");
            }
        }
        else if (!hasSightAttachment && !string.IsNullOrEmpty(weaponData.partToDisableWithSightPath))
        {
            // Re-enable iron sights if no sight is equipped
            Transform partToEnable = transform.Find(weaponData.partToDisableWithSightPath);
            if (partToEnable != null)
            {
                partToEnable.gameObject.SetActive(true);
                Debug.Log($"[WeaponRuntime] Re-enabled iron sight part: {weaponData.partToDisableWithSightPath}");
            }
        }
    }
}