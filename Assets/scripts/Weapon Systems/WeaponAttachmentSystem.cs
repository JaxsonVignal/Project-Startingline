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
        if (att == null || entry == null)
        {
            Debug.LogError("[WeaponAttachmentSystem] EquipAttachment called with null attachment or entry!");
            return;
        }

        Debug.Log($"[WeaponAttachmentSystem] EquipAttachment called for: {att.name} (Type: {att.type}, ID: {att.id})");

        // Check if it's a grenade launcher
        if (att.type == AttachmentType.Underbarrel && att.grenadeLauncherData != null)
        {
            Debug.Log($"[WeaponAttachmentSystem] EQUIPPING GRENADE LAUNCHER: {att.grenadeLauncherData.launcherName}");
            Debug.Log($"[WeaponAttachmentSystem]   - Launcher Name: {att.grenadeLauncherData.launcherName}");
            Debug.Log($"[WeaponAttachmentSystem]   - Damage: {att.grenadeLauncherData.damage}");
            Debug.Log($"[WeaponAttachmentSystem]   - Explosion Radius: {att.grenadeLauncherData.explosionRadius}");
            Debug.Log($"[WeaponAttachmentSystem]   - Magazine Size: {att.grenadeLauncherData.magazineSize}");
        }

        // Remove any existing attachment of same type
        var removed = equippedAttachments.Where(a => a.type == att.type).ToList();
        foreach (var r in removed)
        {
            Debug.Log($"[WeaponAttachmentSystem] Removing existing {r.type} attachment: {r.name}");
            DetachById(r.id);
        }

        Debug.Log($"[WeaponAttachmentSystem] Adding {att.name} to equippedAttachments list");
        equippedAttachments.Add(att);

        if (att.prefab != null)
        {
            Transform socket = slotMap.GetSocket(att.type);
            Debug.Log($"[WeaponAttachmentSystem] Socket for {att.type}: {(socket != null ? socket.name : "NULL")}");

            var go = Instantiate(att.prefab, socket ? socket : transform);
            go.name = $"ATT_{att.id}";

            // Apply the entry transforms
            go.transform.localPosition = new Vector3(entry.posX, entry.posY, entry.posZ);
            go.transform.localRotation = Quaternion.Euler(entry.rotX, entry.rotY, entry.rotZ);
            go.transform.localScale = new Vector3(entry.scaleX, entry.scaleY, entry.scaleZ);

            spawned[att.id] = go;
            Debug.Log($"[WeaponAttachmentSystem] Instantiated prefab for {att.name} at socket");
        }
        else
        {
            Debug.LogWarning($"[WeaponAttachmentSystem] No prefab assigned for {att.name}");
        }

        // If this is a flashlight attachment, notify the FlashlightController
        if (att.type == AttachmentType.SideRail && att.flashlightData != null)
        {
            Debug.Log($"[WeaponAttachmentSystem] This is a FLASHLIGHT attachment!");
            if (FlashlightController.Instance != null && spawned.ContainsKey(att.id))
            {
                FlashlightController.Instance.EquipFlashlight(att.flashlightData, spawned[att.id]);
                Debug.Log($"[WeaponAttachmentSystem] Notified FlashlightController");
            }
        }

        Debug.Log($"[WeaponAttachmentSystem] Current equipped attachments count: {equippedAttachments.Count}");

        // List all equipped attachments
        Debug.Log($"[WeaponAttachmentSystem] All equipped attachments:");
        foreach (var a in equippedAttachments)
        {
            Debug.Log($"[WeaponAttachmentSystem]   - {a.name} (Type: {a.type}, HasGL: {(a.grenadeLauncherData != null)})");
        }

        RecalculateStats();
    }

    public void DetachById(string attId)
    {
        var att = equippedAttachments.FirstOrDefault(a => a.id == attId);
        if (att != null)
        {
            Debug.Log($"[WeaponAttachmentSystem] Detaching: {att.name}");

            // If it's a flashlight, notify the controller
            if (att.type == AttachmentType.SideRail && att.flashlightData != null)
            {
                if (FlashlightController.Instance != null)
                {
                    FlashlightController.Instance.UnequipFlashlight();
                    Debug.Log($"[WeaponAttachmentSystem] Notified FlashlightController of removal");
                }
            }

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
        foreach (var a in list) DetachById(a.id);
    }

    public void ClearAll()
    {
        Debug.Log($"[WeaponAttachmentSystem] ClearAll called - removing {equippedAttachments.Count} attachments");

        // Unequip flashlight if present
        var flashlightAtt = equippedAttachments.FirstOrDefault(a => a.type == AttachmentType.SideRail && a.flashlightData != null);
        if (flashlightAtt != null && FlashlightController.Instance != null)
        {
            FlashlightController.Instance.UnequipFlashlight();
        }

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

        // Check if there's a magazine attachment - if so, it replaces the magazine size
        bool hasMagazineAttachment = false;

        // Apply modifiers from all equipped attachments
        foreach (var att in equippedAttachments)
        {
            // Special handling for Magazine type - it REPLACES magazine size, not adds to it
            if (att.type == AttachmentType.Magazine)
            {
                cachedMagazineSize = att.magazineBonus; // Replace, don't add
                hasMagazineAttachment = true;
                Debug.Log($"[WeaponAttachmentSystem] Magazine attachment found: {att.name}, setting capacity to {att.magazineBonus}");
            }
            else
            {
                // For non-magazine attachments, add the bonus
                cachedMagazineSize += att.magazineBonus;
            }

            // Additive modifiers
            cachedDamage += att.damageBonus;

            // Multiplicative modifiers
            cachedFireRate *= att.fireRateMultiplier;
            cachedReloadTime *= att.reloadTimeMultiplier;
            cachedSpread *= att.spreadMultiplier;
            cachedRecoilX *= att.recoilMultiplier;
            cachedRecoilY *= att.recoilMultiplier;
            cachedRecoilZ *= att.recoilMultiplier;
        }

        Debug.Log($"[WeaponAttachmentSystem] Stats recalculated - Damage: {cachedDamage}, FireRate: {cachedFireRate}, MagazineSize: {cachedMagazineSize}, Spread: {cachedSpread}, Recoil: ({cachedRecoilX}, {cachedRecoilY}, {cachedRecoilZ})");
    }
}