using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
[ExecuteInEditMode]
public class UniqueID : MonoBehaviour
{
    [ReadOnly,SerializeField] 
    private string _ID = Guid.NewGuid().ToString();
}
