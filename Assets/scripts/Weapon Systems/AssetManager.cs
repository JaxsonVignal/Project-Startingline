using System.Collections.Generic;
using UnityEngine;

public class AssetManager : MonoBehaviour
{
    public static AssetManager Instance { get; private set; }

    public List<AttachmentData> allAttachments; // assign in inspector (drag all your AttachmentData assets)
    public List<WeaponData> allWeaponData;      // assign in inspector

    private Dictionary<string, AttachmentData> attachmentLookup = new Dictionary<string, AttachmentData>();
    private Dictionary<string, WeaponData> weaponLookup = new Dictionary<string, WeaponData>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        foreach (var a in allAttachments) if (a != null && !string.IsNullOrEmpty(a.id)) attachmentLookup[a.id] = a;
        foreach (var w in allWeaponData) if (w != null && !string.IsNullOrEmpty(w.weaponId)) weaponLookup[w.weaponId] = w;
    }

    public bool TryGetAttachment(string id, out AttachmentData att) => attachmentLookup.TryGetValue(id, out att);
    public bool TryGetWeapon(string id, out WeaponData weapon) => weaponLookup.TryGetValue(id, out weapon);

    public Dictionary<string, AttachmentData> GetAttachmentLookup() => new Dictionary<string, AttachmentData>(attachmentLookup);
    public Dictionary<string, WeaponData> GetWeaponLookup() => new Dictionary<string, WeaponData>(weaponLookup);
}
