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
    }
}
