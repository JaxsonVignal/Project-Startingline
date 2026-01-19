using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Automatically tracks all weapon configurations in the scene
/// Attach to Player - this will auto-detect and register all weapons
/// </summary>
public class AutoWeaponConfigTracker : MonoBehaviour
{
    [Header("Auto-Tracking Settings")]
    [Tooltip("Automatically scan for weapons on Start")]
    public bool scanOnStart = true;
    
    [Tooltip("Continuously monitor weapons every frame (performance impact)")]
    public bool continuousTracking = false;
    
    [Tooltip("Update interval for continuous tracking (seconds)")]
    public float updateInterval = 1f;
    
    [Header("References")]
    public Transform weaponHolder; // Where weapons are stored
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    private WeaponConfigurationInventory configInventory;
    private PlayerInventoryHolder inventoryHolder;
    private float lastUpdateTime;
    
    // Track which weapons we've already registered
    private Dictionary<string, WeaponConfiguration> trackedWeapons = new Dictionary<string, WeaponConfiguration>();
    
    private void Awake()
    {
        configInventory = GetComponent<WeaponConfigurationInventory>();
        inventoryHolder = GetComponent<PlayerInventoryHolder>();
        
        if (configInventory == null)
        {
            Debug.LogError("WeaponConfigurationInventory not found! Adding it now...");
            configInventory = gameObject.AddComponent<WeaponConfigurationInventory>();
        }
        
        if (inventoryHolder == null)
        {
            Debug.LogWarning("PlayerInventoryHolder not found on player!");
        }
    }
    
    private void Start()
    {
        if (scanOnStart)
        {
            ScanAndRegisterAllWeapons();
        }
    }
    
    private void Update()
    {
        if (continuousTracking && Time.time - lastUpdateTime >= updateInterval)
        {
            ScanAndRegisterAllWeapons();
            lastUpdateTime = Time.time;
        }
    }
    
    /// <summary>
    /// Scan for all weapons and register/update their configurations
    /// </summary>
    public void ScanAndRegisterAllWeapons()
    {
        if (showDebugLogs)
            Debug.Log("=== SCANNING FOR WEAPONS ===");
        
        WeaponAttachmentSystem[] allWeapons = GetAllWeaponsInScene();
        
        if (showDebugLogs)
            Debug.Log($"Found {allWeapons.Length} weapons in scene");
        
        foreach (var weapon in allWeapons)
        {
            if (weapon == null || weapon.weaponData == null)
                continue;
                
            RegisterOrUpdateWeapon(weapon);
        }
        
        // Clean up weapons that no longer exist
        CleanupMissingWeapons(allWeapons);
        
        if (showDebugLogs)
            Debug.Log($"Total tracked configurations: {configInventory.WeaponConfigurations.Count}");
    }
    
    /// <summary>
    /// Register or update a single weapon
    /// </summary>
    public void RegisterOrUpdateWeapon(WeaponAttachmentSystem weapon)
    {
        if (weapon == null || weapon.weaponData == null)
            return;
        
        // Create a unique key for this weapon instance
        string weaponKey = GetWeaponKey(weapon);
        
        // Create configuration from current state
        WeaponConfiguration newConfig = configInventory.CreateConfigurationFromWeapon(weapon);
        
        if (newConfig == null)
            return;
        
        // Check if we already have this weapon tracked
        if (trackedWeapons.ContainsKey(weaponKey))
        {
            WeaponConfiguration existingConfig = trackedWeapons[weaponKey];
            
            // Check if attachments changed
            if (!ConfigurationsMatch(existingConfig, newConfig))
            {
                if (showDebugLogs)
                    Debug.Log($"↻ Weapon modified: {weapon.weaponData.Name} - updating configuration");
                
                // Remove old configuration
                configInventory.WeaponConfigurations.Remove(existingConfig);
                
                // Add updated configuration
                configInventory.AddWeaponConfiguration(newConfig);
                trackedWeapons[weaponKey] = newConfig;
            }
        }
        else
        {
            // New weapon - register it
            if (showDebugLogs)
                Debug.Log($"+ New weapon detected: {weapon.weaponData.Name}");
            
            configInventory.AddWeaponConfiguration(newConfig);
            trackedWeapons[weaponKey] = newConfig;
        }
    }
    
    /// <summary>
    /// Get unique key for weapon instance
    /// </summary>
    private string GetWeaponKey(WeaponAttachmentSystem weapon)
    {
        // Try to use UniqueID if available
        UniqueID uniqueId = weapon.GetComponent<UniqueID>();
        if (uniqueId != null)
            return uniqueId.ID;
        
        // Fallback: use instance ID
        return weapon.GetInstanceID().ToString();
    }
    
    /// <summary>
    /// Check if two configurations are identical
    /// </summary>
    private bool ConfigurationsMatch(WeaponConfiguration a, WeaponConfiguration b)
    {
        if (a.weaponData != b.weaponData)
            return false;
        
        if (a.sightAttachment != b.sightAttachment)
            return false;
        if (a.underbarrelAttachment != b.underbarrelAttachment)
            return false;
        if (a.barrelAttachment != b.barrelAttachment)
            return false;
        if (a.magazineAttachment != b.magazineAttachment)
            return false;
        if (a.sideRailAttachment != b.sideRailAttachment)
            return false;
        
        return true;
    }
    
    /// <summary>
    /// Remove configurations for weapons that no longer exist
    /// </summary>
    private void CleanupMissingWeapons(WeaponAttachmentSystem[] currentWeapons)
    {
        List<string> keysToRemove = new List<string>();
        
        foreach (var kvp in trackedWeapons)
        {
            string weaponKey = kvp.Key;
            bool stillExists = false;
            
            foreach (var weapon in currentWeapons)
            {
                if (weapon != null && GetWeaponKey(weapon) == weaponKey)
                {
                    stillExists = true;
                    break;
                }
            }
            
            if (!stillExists)
            {
                keysToRemove.Add(weaponKey);
                configInventory.WeaponConfigurations.Remove(kvp.Value);
                
                if (showDebugLogs)
                    Debug.Log($"- Weapon removed from scene: {kvp.Value.weaponData.Name}");
            }
        }
        
        foreach (string key in keysToRemove)
        {
            trackedWeapons.Remove(key);
        }
    }
    
    /// <summary>
    /// Get all weapons in scene (including inactive)
    /// </summary>
    private WeaponAttachmentSystem[] GetAllWeaponsInScene()
    {
        if (weaponHolder != null)
        {
            // Search in specified holder
            return weaponHolder.GetComponentsInChildren<WeaponAttachmentSystem>(true);
        }
        
        // Search in player children
        return GetComponentsInChildren<WeaponAttachmentSystem>(true);
    }
    
    /// <summary>
    /// Force refresh all weapon configurations
    /// </summary>
    public void ForceRefresh()
    {
        trackedWeapons.Clear();
        configInventory.WeaponConfigurations.Clear();
        ScanAndRegisterAllWeapons();
        
        if (showDebugLogs)
            Debug.Log("Force refreshed all weapon configurations");
    }
    
    /// <summary>
    /// Call this when a weapon is equipped/spawned
    /// </summary>
    public void OnWeaponAdded(WeaponAttachmentSystem weapon)
    {
        if (showDebugLogs)
            Debug.Log($"OnWeaponAdded called for: {weapon?.weaponData?.Name ?? "null"}");
        
        RegisterOrUpdateWeapon(weapon);
    }
    
    /// <summary>
    /// Call this when attachments are modified
    /// </summary>
    public void OnWeaponModified(WeaponAttachmentSystem weapon)
    {
        if (showDebugLogs)
            Debug.Log($"OnWeaponModified called for: {weapon?.weaponData?.Name ?? "null"}");
        
        RegisterOrUpdateWeapon(weapon);
    }
    
    /// <summary>
    /// Call this when a weapon is destroyed/removed
    /// </summary>
    public void OnWeaponRemoved(WeaponAttachmentSystem weapon)
    {
        if (weapon == null)
            return;
        
        string weaponKey = GetWeaponKey(weapon);
        
        if (trackedWeapons.ContainsKey(weaponKey))
        {
            WeaponConfiguration config = trackedWeapons[weaponKey];
            configInventory.WeaponConfigurations.Remove(config);
            trackedWeapons.Remove(weaponKey);
            
            if (showDebugLogs)
                Debug.Log($"- Weapon removed: {weapon.weaponData.Name}");
        }
    }
    
    // Optional: Debug display in inspector
    private void OnGUI()
    {
        if (!showDebugLogs)
            return;
        
        GUILayout.BeginArea(new Rect(10, 10, 400, 500));
        GUILayout.Label($"<b>Tracked Weapon Configurations: {configInventory?.WeaponConfigurations.Count ?? 0}</b>");
        
        if (configInventory != null)
        {
            foreach (var config in configInventory.WeaponConfigurations)
            {
                GUILayout.Label($"• {config.weaponData.Name}");
                
                var attachments = config.GetAllAttachments();
                foreach (var att in attachments)
                {
                    GUILayout.Label($"  - {att.type}: {att.Name}");
                }
            }
        }
        
        if (GUILayout.Button("Force Refresh"))
        {
            ForceRefresh();
        }
        
        GUILayout.EndArea();
    }
}