using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Bridges the gap between inventory items and weapon configurations
/// Tracks which attachments belong to which weapons in inventory
/// Attach to Player
/// </summary>
public class InventoryWeaponConfigBridge : MonoBehaviour
{
    [Header("References")]
    public PlayerInventoryHolder inventoryHolder;
    public WeaponConfigurationInventory configInventory;
    
    [Header("Weapon Build Tracking")]
    [Tooltip("When you finish building a weapon, it gets stored here with its attachments")]
    public List<WeaponBuildData> weaponBuilds = new List<WeaponBuildData>();
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    private void Awake()
    {
        if (inventoryHolder == null)
            inventoryHolder = GetComponent<PlayerInventoryHolder>();
        
        if (configInventory == null)
            configInventory = GetComponent<WeaponConfigurationInventory>();
        
        if (configInventory == null)
        {
            configInventory = gameObject.AddComponent<WeaponConfigurationInventory>();
            Debug.Log("[InventoryWeaponConfigBridge] Added WeaponConfigurationInventory component");
        }
    }
    
    private void Start()
    {
        // Sync existing builds to config inventory
        SyncAllBuildsToConfigInventory();
    }
    
    /// <summary>
    /// Register a weapon build when player finishes customizing it
    /// Call this from your weapon builder UI when player saves/finishes building
    /// </summary>
    public void RegisterWeaponBuild(WeaponAttachmentSystem weapon)
    {
        if (weapon == null || weapon.weaponData == null)
        {
            Debug.LogError("[InventoryWeaponConfigBridge] Cannot register null weapon!");
            return;
        }
        
        // Create build data
        WeaponBuildData buildData = new WeaponBuildData
        {
            buildId = System.Guid.NewGuid().ToString(),
            weaponData = weapon.weaponData,
            sightAttachment = weapon.GetEquippedAttachment(AttachmentType.Sight),
            underbarrelAttachment = weapon.GetEquippedAttachment(AttachmentType.Underbarrel),
            barrelAttachment = weapon.GetEquippedAttachment(AttachmentType.Barrel),
            magazineAttachment = weapon.GetEquippedAttachment(AttachmentType.Magazine),
            sideRailAttachment = weapon.GetEquippedAttachment(AttachmentType.SideRail)
        };
        
        // Check if this exact build already exists
        if (weaponBuilds.Any(b => BuildsMatch(b, buildData)))
        {
            if (showDebugLogs)
                Debug.Log($"[InventoryWeaponConfigBridge] Build already registered: {weapon.weaponData.Name}");
            return;
        }
        
        // Add to builds list
        weaponBuilds.Add(buildData);
        
        // Create configuration for delivery tracking
        WeaponConfiguration config = BuildDataToConfiguration(buildData);
        configInventory.AddWeaponConfiguration(config);
        
        if (showDebugLogs)
        {
            Debug.Log($"[InventoryWeaponConfigBridge] ✓ Registered weapon build:");
            Debug.Log($"  Weapon: {buildData.weaponData.Name}");
            Debug.Log($"  Build ID: {buildData.buildId}");
            if (buildData.sightAttachment != null)
                Debug.Log($"  • Sight: {buildData.sightAttachment.Name}");
            if (buildData.underbarrelAttachment != null)
                Debug.Log($"  • Underbarrel: {buildData.underbarrelAttachment.Name}");
            if (buildData.barrelAttachment != null)
                Debug.Log($"  • Barrel: {buildData.barrelAttachment.Name}");
            if (buildData.magazineAttachment != null)
                Debug.Log($"  • Magazine: {buildData.magazineAttachment.Name}");
            if (buildData.sideRailAttachment != null)
                Debug.Log($"  • SideRail: {buildData.sideRailAttachment.Name}");
        }
    }
    
    /// <summary>
    /// Register a weapon build from inventory items
    /// Call this when player adds weapon + attachments to inventory
    /// </summary>
    public void RegisterWeaponBuildFromData(
        WeaponData weapon,
        AttachmentData sight = null,
        AttachmentData underbarrel = null,
        AttachmentData barrel = null,
        AttachmentData magazine = null,
        AttachmentData sideRail = null)
    {
        if (weapon == null)
        {
            Debug.LogError("[InventoryWeaponConfigBridge] Cannot register null weapon!");
            return;
        }
        
        WeaponBuildData buildData = new WeaponBuildData
        {
            buildId = System.Guid.NewGuid().ToString(),
            weaponData = weapon,
            sightAttachment = sight,
            underbarrelAttachment = underbarrel,
            barrelAttachment = barrel,
            magazineAttachment = magazine,
            sideRailAttachment = sideRail
        };
        
        // Check if this exact build already exists
        if (weaponBuilds.Any(b => BuildsMatch(b, buildData)))
        {
            if (showDebugLogs)
                Debug.Log($"[InventoryWeaponConfigBridge] Build already exists: {weapon.Name}");
            return;
        }
        
        weaponBuilds.Add(buildData);
        
        WeaponConfiguration config = BuildDataToConfiguration(buildData);
        configInventory.AddWeaponConfiguration(config);
        
        if (showDebugLogs)
            Debug.Log($"[InventoryWeaponConfigBridge] ✓ Registered weapon build from data: {weapon.Name}");
    }
    
    /// <summary>
    /// Remove a weapon build (when delivered or sold)
    /// </summary>
    public bool RemoveWeaponBuild(WeaponConfiguration config)
    {
        if (config == null)
            return false;
        
        // Find matching build
        WeaponBuildData build = weaponBuilds.FirstOrDefault(b => 
            b.weaponData == config.weaponData &&
            b.sightAttachment == config.sightAttachment &&
            b.underbarrelAttachment == config.underbarrelAttachment &&
            b.barrelAttachment == config.barrelAttachment &&
            b.magazineAttachment == config.magazineAttachment &&
            b.sideRailAttachment == config.sideRailAttachment);
        
        if (build != null)
        {
            weaponBuilds.Remove(build);
            
            if (showDebugLogs)
                Debug.Log($"[InventoryWeaponConfigBridge] Removed build: {build.weaponData.Name}");
            
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Sync all weapon builds to configuration inventory
    /// </summary>
    public void SyncAllBuildsToConfigInventory()
    {
        if (configInventory == null)
            return;
        
        // Clear and rebuild
        configInventory.WeaponConfigurations.Clear();
        
        foreach (var build in weaponBuilds)
        {
            WeaponConfiguration config = BuildDataToConfiguration(build);
            configInventory.WeaponConfigurations.Add(config);
        }
        
        if (showDebugLogs)
            Debug.Log($"[InventoryWeaponConfigBridge] Synced {weaponBuilds.Count} builds to config inventory");
    }
    
    /// <summary>
    /// Convert WeaponBuildData to WeaponConfiguration
    /// </summary>
    private WeaponConfiguration BuildDataToConfiguration(WeaponBuildData build)
    {
        WeaponConfiguration config = new WeaponConfiguration(build.weaponData)
        {
            configId = build.buildId,
            sightAttachment = build.sightAttachment,
            underbarrelAttachment = build.underbarrelAttachment,
            barrelAttachment = build.barrelAttachment,
            magazineAttachment = build.magazineAttachment,
            sideRailAttachment = build.sideRailAttachment
        };
        
        return config;
    }
    
    /// <summary>
    /// Check if two builds are identical
    /// </summary>
    private bool BuildsMatch(WeaponBuildData a, WeaponBuildData b)
    {
        return a.weaponData == b.weaponData &&
               a.sightAttachment == b.sightAttachment &&
               a.underbarrelAttachment == b.underbarrelAttachment &&
               a.barrelAttachment == b.barrelAttachment &&
               a.magazineAttachment == b.magazineAttachment &&
               a.sideRailAttachment == b.sideRailAttachment;
    }
    
    /// <summary>
    /// Get all weapon builds
    /// </summary>
    public List<WeaponBuildData> GetAllWeaponBuilds()
    {
        return new List<WeaponBuildData>(weaponBuilds);
    }
    
    /// <summary>
    /// Get builds for a specific weapon type
    /// </summary>
    public List<WeaponBuildData> GetBuildsForWeapon(WeaponData weapon)
    {
        return weaponBuilds.Where(b => b.weaponData == weapon).ToList();
    }
    
    /// <summary>
    /// Check if inventory has the items needed for a build
    /// </summary>
    public bool InventoryHasBuildItems(WeaponBuildData build)
    {
        if (inventoryHolder == null)
            return false;
        
        var inventory = inventoryHolder.PrimaryInventorySystem;
        
        // Check weapon
        if (!inventory.ContainsItem(build.weaponData, 1))
            return false;
        
        // Check attachments
        if (build.sightAttachment != null && !inventory.ContainsItem(build.sightAttachment, 1))
            return false;
        if (build.underbarrelAttachment != null && !inventory.ContainsItem(build.underbarrelAttachment, 1))
            return false;
        if (build.barrelAttachment != null && !inventory.ContainsItem(build.barrelAttachment, 1))
            return false;
        if (build.magazineAttachment != null && !inventory.ContainsItem(build.magazineAttachment, 1))
            return false;
        if (build.sideRailAttachment != null && !inventory.ContainsItem(build.sideRailAttachment, 1))
            return false;
        
        return true;
    }
}

/// <summary>
/// Data structure to store weapon builds in inventory
/// </summary>
[System.Serializable]
public class WeaponBuildData
{
    public string buildId;
    public WeaponData weaponData;
    public AttachmentData sightAttachment;
    public AttachmentData underbarrelAttachment;
    public AttachmentData barrelAttachment;
    public AttachmentData magazineAttachment;
    public AttachmentData sideRailAttachment;
    
    public override string ToString()
    {
        string result = $"Build: {weaponData?.Name ?? "None"} (ID: {buildId})";
        
        if (sightAttachment != null)
            result += $"\n  • Sight: {sightAttachment.Name}";
        if (underbarrelAttachment != null)
            result += $"\n  • Underbarrel: {underbarrelAttachment.Name}";
        if (barrelAttachment != null)
            result += $"\n  • Barrel: {barrelAttachment.Name}";
        if (magazineAttachment != null)
            result += $"\n  • Magazine: {magazineAttachment.Name}";
        if (sideRailAttachment != null)
            result += $"\n  • SideRail: {sideRailAttachment.Name}";
        
        return result;
    }
}