using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeaponAttachmentSaveData
{
    public string attachmentId;
    public AttachmentType type;
    public float posX, posY, posZ;
    public float rotX, rotY, rotZ;
    public float scaleX, scaleY, scaleZ;

    public WeaponAttachmentSaveData(WeaponAttachmentEntry entry)
    {
        attachmentId = entry.attachmentId;
        type = entry.type;
        posX = entry.posX;
        posY = entry.posY;
        posZ = entry.posZ;
        rotX = entry.rotX;
        rotY = entry.rotY;
        rotZ = entry.rotZ;
        scaleX = entry.scaleX;
        scaleY = entry.scaleY;
        scaleZ = entry.scaleZ;
    }

    public WeaponAttachmentEntry ToEntry()
    {
        return new WeaponAttachmentEntry(
            attachmentId,
            type,
            new Vector3(posX, posY, posZ),
            new Vector3(rotX, rotY, rotZ),
            new Vector3(scaleX, scaleY, scaleZ)
        );
    }
}

[System.Serializable]
public class WeaponInstanceSaveData
{
    public string weaponId;
    public string instanceId;
    public string displayName;
    public List<WeaponAttachmentSaveData> attachments;
    public int currentAmmo;

    public WeaponInstanceSaveData(WeaponInstance instance)
    {
        weaponId = instance.weaponId;
        instanceId = instance.instanceId;
        displayName = instance.displayName;
        currentAmmo = instance.currentAmmo;

        attachments = new List<WeaponAttachmentSaveData>();
        foreach (var att in instance.attachments)
        {
            attachments.Add(new WeaponAttachmentSaveData(att));
        }
    }

    public WeaponInstance ToInstance()
    {
        var instance = new WeaponInstance
        {
            weaponId = weaponId,
            instanceId = instanceId,
            displayName = displayName,
            currentAmmo = currentAmmo
        };

        foreach (var attData in attachments)
        {
            instance.attachments.Add(attData.ToEntry());
        }

        return instance;
    }
}

public class SaveData
{
    public List<string> collectedItems;
    public SerializableDictionary<string, InventorySaveData> ChestDictionary;
    public SerializableDictionary<string, ItemPickUpSaveData> activeItems;
    public InventorySaveData playerInventory;
    public SerializableDictionary<string, int> weaponAmmoData;

    // NEW: Store weapon instances with all attachment data per slot
    public SerializableDictionary<string, WeaponInstanceSaveData> weaponInstances;

    public SaveData()
    {
        collectedItems = new List<string>();
        ChestDictionary = new SerializableDictionary<string, InventorySaveData>();
        activeItems = new SerializableDictionary<string, ItemPickUpSaveData>();
        playerInventory = new InventorySaveData();
        weaponAmmoData = new SerializableDictionary<string, int>();
        weaponInstances = new SerializableDictionary<string, WeaponInstanceSaveData>(); // NEW
    }
}