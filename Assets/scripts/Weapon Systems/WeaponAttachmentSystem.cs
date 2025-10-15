using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(AttachmentSlotMap))]
public class WeaponAttachmentSystem : MonoBehaviour
{
    public WeaponData weaponData;
    public List<AttachmentData> equippedAttachments = new List<AttachmentData>();
    private Dictionary<string, GameObject> spawned = new Dictionary<string, GameObject>();

    private AttachmentSlotMap slotMap;

    // Cached stats - recalculated whenever attachments change
    private float cachedDamage;
    private float cachedFireRate;
    private float cachedReloadTime;
    private float cachedSpread;
    private float cachedRecoilX;
    private float cachedRecoilY;
    private float cachedRecoilZ;
    private int cachedMagazineSize;

    public float CurrentDamage => cachedDamage;
    public float CurrentFireRate => cachedFireRate;
    public float CurrentReloadTime => cachedReloadTime;
    public float CurrentSpread => cachedSpread;
    public float CurrentRecoilX => cachedRecoilX;
    public float CurrentRecoilY => cachedRecoilY;
    public float CurrentRecoilZ => cachedRecoilZ;
    public int CurrentMagazineSize => cachedMagazineSize;

    void Awake()
    {
        slotMap = GetComponent<AttachmentSlotMap>();
        RecalculateStats();
    }

    void Start()
    {
        RecalculateStats();
    }

    public void EquipAttachment(AttachmentData att, WeaponAttachmentEntry entry)
    {
        if (att == null || entry == null) return;

        // Remove any existing attachment of same type
        var removed = equippedAttachments.Where(a => a.type == att.type).ToList();
        foreach (var r in removed) DetachById(r.id);

        equippedAttachments.Add(att);

        if (att.prefab != null)
        {
            Transform socket = slotMap.GetSocket(att.type);
            var go = Instantiate(att.prefab, socket ? socket : transform);
            go.name = $"ATT_{att.id}";

            // Apply the entry transforms
            go.transform.localPosition = new Vector3(entry.posX, entry.posY, entry.posZ);
            go.transform.localRotation = Quaternion.Euler(entry.rotX, entry.rotY, entry.rotZ);
            go.transform.localScale = new Vector3(entry.scaleX, entry.scaleY, entry.scaleZ);
            spawned[att.id] = go;
        }

        RecalculateStats();
    }

    public void DetachById(string attId)
    {
        var att = equippedAttachments.FirstOrDefault(a => a.id == attId);
        if (att != null) equippedAttachments.Remove(att);

        if (spawned.TryGetValue(attId, out var go))
        {
            Destroy(go);
            spawned.Remove(attId);
        }

        RecalculateStats();
    }

    public void UnequipType(AttachmentType type)
    {
        var list = equippedAttachments.Where(a => a.type == type).ToList();
        foreach (var a in list) DetachById(a.id);
    }

    public void ClearAll()
    {
        foreach (var kv in spawned) if (kv.Value != null) Destroy(kv.Value);
        spawned.Clear();
        equippedAttachments.Clear();
        RecalculateStats();
    }

    public void RecalculateStats()
    {
        // Start with base weapon stats
        cachedDamage = weaponData.damage;
        cachedFireRate = weaponData.fireRate;
        cachedReloadTime = weaponData.reloadTime;
        cachedSpread = weaponData.spread;
        cachedRecoilX = weaponData.recoilX;
        cachedRecoilY = weaponData.recoilY;
        cachedRecoilZ = weaponData.recoilZ;
        cachedMagazineSize = weaponData.magazineSize;

        // Apply modifiers from all equipped attachments
        foreach (var att in equippedAttachments)
        {
            // Additive modifiers
            cachedDamage += att.damageBonus;
            cachedMagazineSize += att.magazineBonus;

            // Multiplicative modifiers
            cachedFireRate *= att.fireRateMultiplier;
            cachedReloadTime *= att.reloadTimeMultiplier;
            cachedSpread *= att.spreadMultiplier;
            cachedRecoilX *= att.recoilMultiplier;
            cachedRecoilY *= att.recoilMultiplier;
            cachedRecoilZ *= att.recoilMultiplier;
        }

        Debug.Log($"Stats recalculated - Damage: {cachedDamage}, FireRate: {cachedFireRate}, Spread: {cachedSpread}, Recoil: ({cachedRecoilX}, {cachedRecoilY}, {cachedRecoilZ})");
    }
}