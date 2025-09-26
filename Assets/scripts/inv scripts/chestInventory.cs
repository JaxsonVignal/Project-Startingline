using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
public class chestInventory : InventoryHolder, IInteractable
{
   public UnityAction<IInteractable> OnInteractionComplete {  get; set; }

    protected override void Awake()
    {
        base.Awake();
        SaveLoad.OnLoadGame += LoadInventory;
    }

    public void Interact(Interactor interactor, out bool interactSuccessful)
    {
        OnDynamicInventoryDisplayRequested?.Invoke(InventorySystem);
        interactSuccessful = true;
    }
    public void EndInteraction()
    {
       
    }

    private void LoadInventory(SaveData data)
    {
        //check save data for this chest 


    }



}

[System.Serializable]

public struct ChestSaveData
{
    public InventorySystem invSystem;
    public Vector3 position;
    public Quaternion rotation;

    public ChestSaveData(InventorySystem _invSystem, Vector3 _position, Quaternion _rotation)
    {
        invSystem = _invSystem;
        position = _position;
        rotation = _rotation;
    }
}