using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static storage for WeaponInstances mapped to inventory slot IDs
/// This allows weapon attachment data to persist when weapons are picked up and equipped
/// </summary>
public static class WeaponInstanceStorage
{
    private static Dictionary<string, WeaponInstance> instances = new Dictionary<string, WeaponInstance>();

    /// <summary>
    /// Store a WeaponInstance for a specific slot ID
    /// </summary>
    public static void StoreInstance(string slotID, WeaponInstance instance)
    {
        if (string.IsNullOrEmpty(slotID) || instance == null)
        {
            Debug.LogWarning("Cannot store null instance or empty slotID");
            return;
        }

        instances[slotID] = instance;
        Debug.Log($"Stored weapon instance for slot {slotID} with {instance.attachments.Count} attachments");
    }

    /// <summary>
    /// Retrieve a WeaponInstance for a specific slot ID
    /// </summary>
    public static WeaponInstance GetInstance(string slotID)
    {
        if (string.IsNullOrEmpty(slotID))
            return null;

        instances.TryGetValue(slotID, out var instance);
        return instance;
    }

    /// <summary>
    /// Remove a WeaponInstance from storage
    /// </summary>
    public static void RemoveInstance(string slotID)
    {
        if (string.IsNullOrEmpty(slotID))
            return;

        instances.Remove(slotID);
    }

    /// <summary>
    /// Clear all stored instances
    /// </summary>
    public static void ClearAll()
    {
        instances.Clear();
    }
}