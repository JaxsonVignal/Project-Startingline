using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WeaponInstance
{
    public string weaponId;           // matches WeaponData.weaponId or name
    public string instanceId;         // unique GUID
    public string displayName;
    public List<WeaponAttachmentEntry> attachments = new List<WeaponAttachmentEntry>();
    public int currentAmmo = -1;      // optional: -1 = full mag

    public WeaponInstance()
    {
        instanceId = Guid.NewGuid().ToString();
    }
}

[Serializable]
public class WeaponAttachmentEntry
{
    public string attachmentId;   // matches AttachmentData.id
    public AttachmentType type;

    // Transform offsets relative to the weapon
    public float posX, posY, posZ;
    public float rotX, rotY, rotZ;
    public float scaleX = 1f, scaleY = 1f, scaleZ = 1f;

    public WeaponAttachmentEntry() { }

    public WeaponAttachmentEntry(string id, AttachmentType t, Vector3 localPos, Vector3 localRot, Vector3 localScale)
    {
        attachmentId = id;
        type = t;

        posX = localPos.x;
        posY = localPos.y;
        posZ = localPos.z;

        rotX = localRot.x;
        rotY = localRot.y;
        rotZ = localRot.z;

        scaleX = localScale.x;
        scaleY = localScale.y;
        scaleZ = localScale.z;
    }
}
