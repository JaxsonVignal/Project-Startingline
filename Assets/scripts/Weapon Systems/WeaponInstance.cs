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
    public string attachmentId;
    public AttachmentType type;
    public Vector3 localPos;
    public Vector3 localEuler;
    public Vector3 localScale = Vector3.one;

    public WeaponAttachmentEntry() { }

    public WeaponAttachmentEntry(string id, AttachmentType t, Vector3 pos, Vector3 euler, Vector3 scale)
    {
        attachmentId = id;
        type = t;
        localPos = pos;
        localEuler = euler;
        localScale = scale;
    }
}
