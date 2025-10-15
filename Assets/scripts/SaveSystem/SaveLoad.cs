using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

public static class SaveLoad
{
    public static UnityAction OnSaveGame;
    public static UnityAction<SaveData> OnLoadGame;
    private static string directory = "/SaveData/";
    private static string fileName = "SaveGame.sav";

    public static bool Save(SaveData data)
    {
        OnSaveGame?.Invoke();

        // NEW: Save all weapon instances with attachments before saving data
        SaveWeaponInstances(data);

        string dir = Application.persistentDataPath + directory;
        GUIUtility.systemCopyBuffer = dir;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(path: dir + fileName, contents: json);
        Debug.Log(message: "Saving game with weapon instances");
        return true;
    }

    public static SaveData Load()
    {
        string fullPath = Application.persistentDataPath + directory + fileName;
        SaveData data = new SaveData();
        if (File.Exists(fullPath))
        {
            string json = File.ReadAllText(fullPath);
            data = JsonUtility.FromJson<SaveData>(json);

            // NEW: Load all weapon instances with attachments
            LoadWeaponInstances(data);

            OnLoadGame?.Invoke(data);
            Debug.Log("Game loaded with weapon instances restored");
        }
        else
        {
            Debug.Log("Savefile does not exist");
        }
        return data;
    }

    public static void DeleteSaveData()
    {
        string fullPath = Application.persistentDataPath + directory + fileName;
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            WeaponInstanceStorage.ClearAll();
            Debug.Log("Save data and weapon instances deleted");
        }
    }

    // NEW: Save all weapon instances to SaveData
    private static void SaveWeaponInstances(SaveData data)
    {
        data.weaponInstances.Clear();

        // Get all inventory slots and save their weapon instances
        var playerInventory = GameObject.FindObjectOfType<PlayerInventoryHolder>();
        if (playerInventory != null)
        {
            foreach (var slot in playerInventory.PrimaryInventorySystem.InventorySlots)
            {
                if (slot.ItemData is WeaponData)
                {
                    var instance = WeaponInstanceStorage.GetInstance(slot.UniqueSlotID);
                    if (instance != null)
                    {
                        data.weaponInstances[slot.UniqueSlotID] = new WeaponInstanceSaveData(instance);
                        Debug.Log($"Saved weapon instance for slot {slot.UniqueSlotID} with {instance.attachments.Count} attachments");
                    }
                }
            }
        }
    }

    // NEW: Load all weapon instances from SaveData
    private static void LoadWeaponInstances(SaveData data)
    {
        WeaponInstanceStorage.ClearAll();

        foreach (var kvp in data.weaponInstances)
        {
            var instance = kvp.Value.ToInstance();
            WeaponInstanceStorage.StoreInstance(kvp.Key, instance);
            Debug.Log($"Loaded weapon instance for slot {kvp.Key} with {instance.attachments.Count} attachments");
        }
    }
}