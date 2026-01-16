using UnityEngine;

/// <summary>
/// Add this to your weapon builder UI/system to register completed weapon builds
/// Call RegisterCurrentWeaponBuild() when player "saves" or "finishes" a weapon build
/// </summary>
public class WeaponBuilderIntegration : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The current weapon being built/customized")]
    public WeaponAttachmentSystem currentWeapon;
    
    [Tooltip("Reference to player's weapon configuration inventory")]
    public WeaponConfigurationInventory configInventory;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    /// <summary>
    /// Call this when player finishes building a weapon
    /// This registers the weapon configuration for delivery tracking
    /// </summary>
    public void RegisterCurrentWeaponBuild()
    {
        if (currentWeapon == null)
        {
            Debug.LogError("Cannot register weapon: currentWeapon is null!");
            return;
        }
        
        if (configInventory == null)
        {
            Debug.LogError("Cannot register weapon: configInventory is null!");
            return;
        }
        
        // Create configuration from current weapon
        WeaponConfiguration config = configInventory.CreateConfigurationFromWeapon(currentWeapon);
        
        if (config == null)
        {
            Debug.LogError("Failed to create weapon configuration!");
            return;
        }
        
        // Add to inventory
        configInventory.AddWeaponConfiguration(config);
        
        if (showDebugLogs)
        {
            Debug.Log($"✓ Registered weapon build:");
            Debug.Log(config.ToString());
        }
    }
    
    /// <summary>
    /// Call this when player picks up or equips a weapon
    /// Automatically registers it if not already tracked
    /// </summary>
    public void RegisterWeaponIfNeeded(WeaponAttachmentSystem weapon)
    {
        if (weapon == null || configInventory == null)
            return;
            
        currentWeapon = weapon;
        RegisterCurrentWeaponBuild();
    }
    
    /// <summary>
    /// Manual registration - call from UI button or when exiting weapon builder
    /// </summary>
    public void OnWeaponBuildComplete()
    {
        RegisterCurrentWeaponBuild();
        
        // Optional: Show confirmation message
        Debug.Log("Weapon build saved and ready for delivery!");
    }
}

// ==============================================================
// INTEGRATION EXAMPLES:
// ==============================================================

/// <summary>
/// Example: Add this to your weapon equip/spawn system
/// </summary>
public class WeaponEquipExample : MonoBehaviour
{
    public WeaponBuilderIntegration builderIntegration;
    
    public void OnWeaponEquipped(WeaponAttachmentSystem weapon)
    {
        // When player equips/spawns a weapon, register it
        if (builderIntegration != null)
        {
            builderIntegration.RegisterWeaponIfNeeded(weapon);
        }
    }
}

/// <summary>
/// Example: Add this to your attachment equip system
/// </summary>
public class AttachmentEquipExample : MonoBehaviour
{
    public WeaponBuilderIntegration builderIntegration;
    public WeaponAttachmentSystem currentWeapon;
    
    public void OnAttachmentEquipped(AttachmentData attachment)
    {
        // After equipping an attachment, re-register the weapon
        if (builderIntegration != null && currentWeapon != null)
        {
            builderIntegration.currentWeapon = currentWeapon;
            builderIntegration.RegisterCurrentWeaponBuild();
        }
    }
}

/// <summary>
/// Example: Add button listener in weapon builder UI
/// </summary>
public class WeaponBuilderUIExample : MonoBehaviour
{
    public WeaponBuilderIntegration builderIntegration;
    
    void Start()
    {
        // Hook up to "Save Build" or "Finish" button
        // GetComponent<Button>().onClick.AddListener(OnSaveBuildClicked);
    }
    
    void OnSaveBuildClicked()
    {
        if (builderIntegration != null)
        {
            builderIntegration.OnWeaponBuildComplete();
        }
    }
}