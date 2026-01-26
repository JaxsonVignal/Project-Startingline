using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(AttachmentSlotMap))]
public class WeaponAttachmentSystem : MonoBehaviour
{
    public WeaponData weaponData;
    public List<AttachmentData> equippedAttachments = new();
    private Dictionary<string, GameObject> spawned = new();

    private AttachmentSlotMap slotMap;

    // Cached stats
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

    private void Awake()
    {
        slotMap = GetComponent<AttachmentSlotMap>();
        RecalculateStats();
    }

    private void Start()
    {
        RecalculateStats();
    }

    // --------------------------------------------------
    // ATTACHMENT LOOKUP (IMPORTANT FIX)
    // --------------------------------------------------

    public bool HasAttachment(string attachmentId)
    {
        return equippedAttachments.Any(a => a != null && a.id == attachmentId);
    }

    public AttachmentData GetAttachmentById(string attachmentId)
    {
        return equippedAttachments.FirstOrDefault(a => a != null && a.id == attachmentId);
    }

    // --------------------------------------------------
    // EQUIP / UNEQUIP
    // --------------------------------------------------

    public void EquipAttachment(AttachmentData att, WeaponAttachmentEntry entry)
    {
        if (att == null || entry == null)
        {
            Debug.LogError("[WeaponAttachmentSystem] EquipAttachment called with null args");
            return;
        }

        Debug.Log($"[WeaponAttachmentSystem] Equipping: {att.name} (Type={att.type}, ID={att.id})");

        // Remove existing attachment of same type
        var existing = equippedAttachments.Where(a => a.type == att.type).ToList();
        foreach (var e in existing)
            DetachById(e.id);

        equippedAttachments.Add(att);

        // Spawn prefab if exists
        if (att.prefab != null)
        {
            Transform socket = slotMap.GetSocket(att.type);
            var go = Instantiate(att.prefab, socket ? socket : transform);
            go.name = $"ATT_{att.id}";

            go.transform.localPosition = new(entry.posX, entry.posY, entry.posZ);
            go.transform.localRotation = Quaternion.Euler(entry.rotX, entry.rotY, entry.rotZ);
            go.transform.localScale = new(entry.scaleX, entry.scaleY, entry.scaleZ);

            spawned[att.id] = go;
        }

        // Flashlight special handling
        if (att.type == AttachmentType.SideRail && att.flashlightData != null)
        {
            if (FlashlightController.Instance != null && spawned.ContainsKey(att.id))
                FlashlightController.Instance.EquipFlashlight(att.flashlightData, spawned[att.id]);
        }

        RecalculateStats();
    }

    public void DetachById(string attId)
    {
        var att = GetAttachmentById(attId);
        if (att != null)
        {
            Debug.Log($"[WeaponAttachmentSystem] Detaching: {att.name}");

            // Flashlight cleanup
            if (att.type == AttachmentType.SideRail && att.flashlightData != null)
                FlashlightController.Instance?.UnequipFlashlight();

            equippedAttachments.Remove(att);
        }

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
        foreach (var a in list)
            DetachById(a.id);
    }

    public void ClearAll()
    {
        foreach (var kv in spawned)
            if (kv.Value != null)
                Destroy(kv.Value);

        spawned.Clear();
        equippedAttachments.Clear();

        FlashlightController.Instance?.UnequipFlashlight();

        RecalculateStats();
    }

    // --------------------------------------------------
    // STATS
    // --------------------------------------------------

    public void RecalculateStats()
    {
        cachedDamage = weaponData.damage;
        cachedFireRate = weaponData.fireRate;
        cachedReloadTime = weaponData.reloadTime;
        cachedSpread = weaponData.spread;
        cachedRecoilX = weaponData.recoilX;
        cachedRecoilY = weaponData.recoilY;
        cachedRecoilZ = weaponData.recoilZ;
        cachedMagazineSize = weaponData.magazineSize;

        foreach (var att in equippedAttachments)
        {
            if (att == null) continue;

            // Magazine logic (replace size)
            if (att.type == AttachmentType.Magazine)
                cachedMagazineSize = att.magazineBonus;
            else
                cachedMagazineSize += att.magazineBonus;

            cachedDamage += att.damageBonus;
            cachedFireRate *= att.fireRateMultiplier;
            cachedReloadTime *= att.reloadTimeMultiplier;
            cachedSpread *= att.spreadMultiplier;
            cachedRecoilX *= att.recoilMultiplier;
            cachedRecoilY *= att.recoilMultiplier;
            cachedRecoilZ *= att.recoilMultiplier;
        }

        Debug.Log($"[WeaponAttachmentSystem] Stats Updated ? DMG:{cachedDamage} FR:{cachedFireRate} MAG:{cachedMagazineSize}");
    }
}
