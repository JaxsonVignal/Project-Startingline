using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        // --- Save Player Inventory ---
        var playerInventory = GameObject.FindObjectOfType<PlayerInventoryHolder>();
        if (playerInventory != null)
        {
            SaveInventoryInstances(playerInventory.PrimaryInventorySystem, data);
        }

        // --- Save All Chests ---
        var chests = GameObject.FindObjectsOfType<chestInventory>();
        foreach (var chest in chests)
        {
            SaveInventoryInstances(chest.PrimaryInventorySystem, data);
        }

        Debug.Log($"Saved weapon instances from player + {GameObject.FindObjectsOfType<chestInventory>().Length} chests");
    }

    // Helper method to reduce duplication
    private static void SaveInventoryInstances(InventorySystem invSystem, SaveData data)
    {
        foreach (var slot in invSystem.InventorySlots)
        {
            if (slot.ItemData is WeaponData)
            {
                var instance = WeaponInstanceStorage.GetInstance(slot.UniqueSlotID);
                if (instance != null)
                {
                    data.weaponInstances[slot.UniqueSlotID] = new WeaponInstanceSaveData(instance);
                    Debug.Log($"Saved weapon instance for slot {slot.UniqueSlotID} ({slot.ItemData.name}) with {instance.attachments.Count} attachments");
                }
            }
        }
    }

    private static void LoadWeaponInstances(SaveData data)
    {
        WeaponInstanceStorage.ClearAll();

        // --- STEP 1: Rebuild storage ---
        foreach (var kvp in data.weaponInstances)
        {
            var instance = kvp.Value.ToInstance();
            WeaponInstanceStorage.StoreInstance(kvp.Key, instance);
            Debug.Log($"Loaded weapon instance for slot {kvp.Key} with {instance.attachments.Count} attachments");
        }

        // --- STEP 2: Reapply to all inventories ---
        var allInventories = new List<InventorySystem>();

        var player = GameObject.FindObjectOfType<PlayerInventoryHolder>();
        if (player != null)
            allInventories.Add(player.PrimaryInventorySystem);

        allInventories.AddRange(GameObject.FindObjectsOfType<chestInventory>().Select(c => c.PrimaryInventorySystem));

        foreach (var inv in allInventories)
        {
            foreach (var slot in inv.InventorySlots)
            {
                if (slot.ItemData is WeaponData weaponData)
                {
                    var instance = WeaponInstanceStorage.GetInstance(slot.UniqueSlotID);
                    if (instance != null)
                    {
                        Debug.Log($"Restored attachments for {weaponData.name} in slot {slot.UniqueSlotID} ({instance.attachments.Count} attachments)");
                        // You don't need to visually re-attach yet; that will happen when equipped.
                        // But now, when the player picks or equips it, it will have the correct data.
                    }
                }
            }
        }

        Debug.Log("All weapon instances restored for player and chest inventories.");
    }

}