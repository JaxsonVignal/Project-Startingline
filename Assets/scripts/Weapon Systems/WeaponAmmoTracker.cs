using System.Collections.Generic;
using UnityEngine;

public static class WeaponAmmoTracker
{
    // Maps each weapon slot's unique ID (string) to its current ammo
    private static Dictionary<string, int> ammoDatabase = new Dictionary<string, int>();

    // Get ammo for a weapon slot, defaulting to full clip if not tracked yet
    public static int GetAmmo(string slotID, int defaultAmmo)
    {
        if (string.IsNullOrEmpty(slotID)) return defaultAmmo;

        if (ammoDatabase.TryGetValue(slotID, out int ammo))
            return ammo;

        // Initialize if not found
        ammoDatabase[slotID] = defaultAmmo;
        return defaultAmmo;
    }

    // Update ammo for a weapon slot
    public static void SetAmmo(string slotID, int ammo)
    {
        if (string.IsNullOrEmpty(slotID)) return;

        ammoDatabase[slotID] = ammo;
    }

    // Optional: Clear ammo data for a slot
    public static void ClearAmmo(string slotID)
    {
        if (string.IsNullOrEmpty(slotID)) return;

        if (ammoDatabase.ContainsKey(slotID))
            ammoDatabase.Remove(slotID);
    }

    // Optional: Debug method to see all tracked ammo
    public static void DebugPrintAmmo()
    {
        Debug.Log($"=== Ammo Tracker ({ammoDatabase.Count} weapons tracked) ===");
        foreach (var kvp in ammoDatabase)
        {
            Debug.Log($"Slot ID [{kvp.Key}]: {kvp.Value} ammo");
        }
    }
}
