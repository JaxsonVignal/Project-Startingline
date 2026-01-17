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
        SaveLoad.OnSaveGame += PrepareAmmoDataForSave;
        SaveLoad.OnSaveGame += PrepareDayNightDataForSave; // NEW
    }

    private void OnDestroy()
    {
        SaveLoad.OnLoadGame -= LoadData;
        SaveLoad.OnSaveGame -= PrepareAmmoDataForSave;
        SaveLoad.OnSaveGame -= PrepareDayNightDataForSave; // NEW
    }

    public void DeleteData()
    {
        SaveLoad.DeleteSaveData();
        WeaponAmmoTracker.ClearAllAmmo();
    }

    public static void SaveData()
    {
        var SaveData = data;
        SaveLoad.Save(SaveData);
    }

    public static void LoadData(SaveData _data)
    {
        data = _data;

        // Load weapon ammo data
        if (_data.weaponAmmoData != null)
        {
            WeaponAmmoTracker.LoadAmmoData(_data.weaponAmmoData);
            Debug.Log($"Loaded ammo data for {_data.weaponAmmoData.Count} weapons");
        }

        // NEW: Load day/night cycle data
        LoadDayNightData(_data);
    }

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

    // NEW: Prepare day/night data before saving
    private void PrepareDayNightDataForSave()
    {
        if (DayNightCycleManager.Instance != null)
        {
            data.currentDayOfWeek = (int)DayNightCycleManager.Instance.CurrentDayOfWeek;
            data.currentTimeOfDay = DayNightCycleManager.Instance.currentTimeOfDay;
            data.totalDaysPassed = DayNightCycleManager.Instance.GetCurrentDay();

            Debug.Log($"Saving day/night data: {(DayNightCycleManager.DayOfWeek)data.currentDayOfWeek}, Time: {data.currentTimeOfDay:F2}, Total Days: {data.totalDaysPassed}");
        }
        else
        {
            Debug.LogWarning("DayNightCycleManager not found when saving!");
        }
    }

    // NEW: Load day/night data after loading save
    private static void LoadDayNightData(SaveData _data)
    {
        if (DayNightCycleManager.Instance != null)
        {
            // Restore day of week
            DayNightCycleManager.DayOfWeek loadedDay = (DayNightCycleManager.DayOfWeek)_data.currentDayOfWeek;
            DayNightCycleManager.Instance.SetDayOfWeek(loadedDay);

            // Restore time of day
            DayNightCycleManager.Instance.SetTime(_data.currentTimeOfDay);

            Debug.Log($"Loaded day/night data: {loadedDay}, Time: {_data.currentTimeOfDay:F2}, Total Days: {_data.totalDaysPassed}");
        }
        else
        {
            Debug.LogWarning("DayNightCycleManager not found when loading!");
        }
    }

    public static void TryLoadData()
    {
        SaveLoad.Load();
    }
}