using System.Collections.Generic;
using UnityEngine;

public static class WeaponAmmoTracker
{
    // Maps each weapon's unique ID to its current ammo
    private static Dictionary<string, int> ammoDatabase = new Dictionary<string, int>();

    // Get ammo for a weapon, defaulting to full clip if not tracked yet
    public static int GetAmmo(UniqueID weaponID, int defaultAmmo)
    {
        if (weaponID == null) return defaultAmmo;

        if (ammoDatabase.TryGetValue(weaponID.ID, out int ammo))
            return ammo;

        // Initialize if not found
        ammoDatabase[weaponID.ID] = defaultAmmo;
        return defaultAmmo;
    }

    // Update ammo for a weapon
    public static void SetAmmo(UniqueID weaponID, int ammo)
    {
        if (weaponID == null) return;

        ammoDatabase[weaponID.ID] = ammo;
    }
}
