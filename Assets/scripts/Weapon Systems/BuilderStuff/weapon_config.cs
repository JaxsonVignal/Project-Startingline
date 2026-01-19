using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a specific weapon build with attachments
/// </summary>
[System.Serializable]
public class WeaponConfiguration
{
    public string configId; // Unique ID for this specific build
    public WeaponData weaponData;
    public AttachmentData sightAttachment;
    public AttachmentData underbarrelAttachment;
    public AttachmentData barrelAttachment;
    public AttachmentData magazineAttachment;
    public AttachmentData sideRailAttachment;
    
    public WeaponConfiguration(WeaponData weapon)
    {
        configId = Guid.NewGuid().ToString();
        weaponData = weapon;
    }
    
    /// <summary>
    /// Check if this configuration exactly matches an order
    /// </summary>
    public bool MatchesOrder(WeaponOrder order)
    {
        // Check weapon type
        if (weaponData != order.weaponRequested)
            return false;
            
        // Check each attachment slot
        if (!AttachmentMatches(sightAttachment, order.sightAttachment))
            return false;
        if (!AttachmentMatches(underbarrelAttachment, order.underbarrelAttachment))
            return false;
        if (!AttachmentMatches(barrelAttachment, order.barrelAttachment))
            return false;
        if (!AttachmentMatches(magazineAttachment, order.magazineAttachment))
            return false;
        if (!AttachmentMatches(sideRailAttachment, order.sideRailAttachment))
            return false;
            
        return true;
    }
    
    private bool AttachmentMatches(AttachmentData equipped, AttachmentData required)
    {
        // Both null = match
        if (equipped == null && required == null)
            return true;
            
        // One null, one not = no match
        if (equipped == null || required == null)
            return false;
            
        // Compare by reference or name
        return equipped == required || equipped.name == required.name;
    }
    
    /// <summary>
    /// Get list of all attachments in this configuration
    /// </summary>
    public List<AttachmentData> GetAllAttachments()
    {
        List<AttachmentData> attachments = new List<AttachmentData>();
        
        if (sightAttachment != null) attachments.Add(sightAttachment);
        if (underbarrelAttachment != null) attachments.Add(underbarrelAttachment);
        if (barrelAttachment != null) attachments.Add(barrelAttachment);
        if (magazineAttachment != null) attachments.Add(magazineAttachment);
        if (sideRailAttachment != null) attachments.Add(sideRailAttachment);
        
        return attachments;
    }
    
    public override string ToString()
    {
        string result = $"Weapon: {weaponData?.Name ?? "None"}";
        
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