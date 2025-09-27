using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.CodeDom.Compiler;

[System.Serializable]
[ExecuteInEditMode]
public class UniqueID : MonoBehaviour
{
    [ReadOnly,SerializeField] 
    private string _ID = Guid.NewGuid().ToString();

    [SerializeField]
    private static SerializableDictionary<string, GameObject> idDatabase = new SerializableDictionary<string, GameObject>();

    public string ID => _ID;

    private void OnValidate()
    {
        if (idDatabase.ContainsKey(ID)) Generate();

        else
        {
            idDatabase.Add(_ID, this.gameObject);
        }
    }

    private void Generate()
    {
        _ID = Guid.NewGuid().ToString();
        idDatabase.Add(_ID, this.gameObject);
    }

    private void OnDestroy()
    {
        if(idDatabase.ContainsKey(ID)) idDatabase.Remove(ID);
    }
}
