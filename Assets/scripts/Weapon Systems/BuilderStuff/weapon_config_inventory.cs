using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages weapon configurations (built weapons with attachments)
/// Add this component to the same GameObject as PlayerInventoryHolder
/// </summary>
public class WeaponConfigurationInventory : MonoBehaviour
{
    [SerializeField] private List<WeaponConfiguration> weaponConfigurations = new List<WeaponConfiguration>();
    
    public List<WeaponConfiguration> WeaponConfigurations => weaponConfigurations;
    
    /// <summary>
    /// Add a new weapon configuration to inventory
    /// </summary>
    public void AddWeaponConfiguration(WeaponConfiguration config)
    {
        if (config == null)
        {
            Debug.LogError("Cannot add null weapon configuration");
            return;
        }
        
        weaponConfigurations.Add(config);
        Debug.Log($"Added weapon configuration: {config.weaponData.Name}");
    }
    
    /// <summary>
    /// Create configuration from current weapon build
    /// </summary>
    public WeaponConfiguration CreateConfigurationFromWeapon(WeaponAttachmentSystem weapon)
    {
        if (weapon == null || weapon.weaponData == null)
        {
            Debug.LogError("Cannot create configuration from null weapon");
            return null;
        }
        
        WeaponConfiguration config = new WeaponConfiguration(weapon.weaponData);
        
        // Get equipped attachments
        foreach (var attachment in weapon.equippedAttachments)
        {
            if (attachment == null) continue;
            
            switch (attachment.type)
            {
                case AttachmentType.Sight:
                    config.sightAttachment = attachment;
                    break;
                case AttachmentType.Underbarrel:
                    config.underbarrelAttachment = attachment;
                    break;
                case AttachmentType.Barrel:
                    config.barrelAttachment = attachment;
                    break;
                case AttachmentType.Magazine:
                    config.magazineAttachment = attachment;
                    break;
                case AttachmentType.SideRail:
                    config.sideRailAttachment = attachment;
                    break;
            }
        }
        
        return config;
    }
    
    /// <summary>
    /// Find weapon configuration that exactly matches an order
    /// </summary>
    public WeaponConfiguration FindMatchingConfiguration(WeaponOrder order)
    {
        if (order == null)
        {
            Debug.LogError("Cannot find match for null order");
            return null;
        }
        
        Debug.Log($"Searching for weapon matching order:");
        Debug.Log($"  Weapon: {order.weaponRequested.Name}");
        Debug.Log($"  Sight: {order.sightAttachment?.Name ?? "None"}");
        Debug.Log($"  Underbarrel: {order.underbarrelAttachment?.Name ?? "None"}");
        Debug.Log($"  Barrel: {order.barrelAttachment?.Name ?? "None"}");
        Debug.Log($"  Magazine: {order.magazineAttachment?.Name ?? "None"}");
        Debug.Log($"  SideRail: {order.sideRailAttachment?.Name ?? "None"}");
        
        Debug.Log($"\nChecking {weaponConfigurations.Count} weapon configurations:");
        
        for (int i = 0; i < weaponConfigurations.Count; i++)
        {
            var config = weaponConfigurations[i];
            Debug.Log($"\n[{i}] Checking: {config.weaponData.Name}");
            Debug.Log(config.ToString());
            
            if (config.MatchesOrder(order))
            {
                Debug.Log($"✓ EXACT MATCH FOUND!");
                return config;
            }
            else
            {
                Debug.Log($"✗ No match");
            }
        }
        
        Debug.LogWarning("No matching weapon configuration found!");
        return null;
    }
    
    /// <summary>
    /// Remove a weapon configuration and its components from inventory
    /// </summary>
    public bool RemoveWeaponConfiguration(WeaponConfiguration config, InventorySystem inventory)
    {
        if (config == null || inventory == null)
        {
            Debug.LogError("Cannot remove: config or inventory is null");
            return false;
        }
        
        // Remove weapon from inventory
        if (!inventory.RemoveFromInventory(config.weaponData, 1))
        {
            Debug.LogError($"Failed to remove weapon {config.weaponData.Name} from inventory");
            return false;
        }
        
        // Remove all attachments from inventory
        foreach (var attachment in config.GetAllAttachments())
        {
            if (!inventory.RemoveFromInventory(attachment, 1))
            {
                Debug.LogWarning($"Failed to remove attachment {attachment.Name} from inventory");
            }
        }
        
        // Remove from configuration list
        weaponConfigurations.Remove(config);
        
        Debug.Log($"Removed weapon configuration: {config.weaponData.Name}");
        return true;
    }
    
    /// <summary>
    /// Get all configurations for a specific weapon type
    /// </summary>
    public List<WeaponConfiguration> GetConfigurationsForWeapon(WeaponData weaponData)
    {
        return weaponConfigurations
            .Where(c => c.weaponData == weaponData)
            .ToList();
    }
    
    /// <summary>
    /// Check if we have required materials to build a weapon configuration
    /// </summary>
    public bool CanBuildConfiguration(WeaponConfiguration config, InventorySystem inventory)
    {
        // Check weapon
        if (!inventory.ContainsItem(config.weaponData, 1))
            return false;
        
        // Check attachments
        foreach (var attachment in config.GetAllAttachments())
        {
            if (!inventory.ContainsItem(attachment, 1))
                return false;
        }
        
        return true;
    }
}