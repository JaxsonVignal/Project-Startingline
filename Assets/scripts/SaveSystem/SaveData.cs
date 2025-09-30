using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveData 
{
    public List<string> collectedItems;
    public SerializableDictionary<string, InventorySaveData> ChestDictionary;
    public SerializableDictionary<string, ItemPickUpSaveData> activeItems;

    public InventorySaveData playerInventory;
    public SaveData()
    {
        collectedItems = new List<string>();
        ChestDictionary = new SerializableDictionary<string, InventorySaveData> ();
        activeItems = new SerializableDictionary<string, ItemPickUpSaveData> (); 
        playerInventory = new InventorySaveData ();
    }
}
