using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveData 
{
    public SerializableDictionary<string, ChestSaveData> ChestDictionary;

    public SaveData()
    {
        ChestDictionary = new SerializableDictionary<string, ChestSaveData> ();
    }
}
