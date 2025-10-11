using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SaveGameManager : MonoBehaviour
{
    public static SaveData data;

    private void Awake()
    {
        data = new SaveData();
        SaveLoad.OnLoadGame += LoadData;
        SaveLoad.OnSaveGame += PrepareAmmoDataForSave; // NEW: Hook into save event
    }

    private void OnDestroy()
    {
        SaveLoad.OnLoadGame -= LoadData;
        SaveLoad.OnSaveGame -= PrepareAmmoDataForSave; // NEW: Clean up
    }

    public void DeleteData()
    {
        SaveLoad.DeleteSaveData();
        WeaponAmmoTracker.ClearAllAmmo(); // NEW: Clear ammo when deleting save
    }

    public static void SaveData()
    {
        var SaveData = data;
        SaveLoad.Save(SaveData);
    }

    public static void LoadData(SaveData _data)
    {
        data = _data;

        // NEW: Load weapon ammo data
        if (_data.weaponAmmoData != null)
        {
            WeaponAmmoTracker.LoadAmmoData(_data.weaponAmmoData);
            Debug.Log($"Loaded ammo data for {_data.weaponAmmoData.Count} weapons");
        }
    }

    // NEW: Prepare ammo data before saving
    private void PrepareAmmoDataForSave()
    {
        data.weaponAmmoData.Clear();
        var ammoData = WeaponAmmoTracker.GetAllAmmoData();

        foreach (var kvp in ammoData)
        {
            data.weaponAmmoData[kvp.Key] = kvp.Value;
        }

        Debug.Log($"Preparing to save ammo data for {data.weaponAmmoData.Count} weapons");
    }

    public static void TryLoadData()
    {
        SaveLoad.Load();
    }
}