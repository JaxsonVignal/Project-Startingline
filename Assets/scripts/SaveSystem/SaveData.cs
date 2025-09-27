using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveData 
{
    public List<string> collectedItems;
    public SerializableDictionary<string, ChestSaveData> ChestDictionary;
    public SerializableDictionary<string, ItemPickUpSaveData> activeItems;
    public SaveData()
    {
        collectedItems = new List<string>();
        ChestDictionary = new SerializableDictionary<string, ChestSaveData> ();
        activeItems = new SerializableDictionary<string, ItemPickUpSaveData> (); 
}
}
