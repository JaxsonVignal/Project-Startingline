using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof (UniqueID))]
public class chestInventory : InventoryHolder, IInteractable
{
   public UnityAction<IInteractable> OnInteractionComplete {  get; set; }

    protected override void Awake()
    {
        base.Awake();
        SaveLoad.OnLoadGame += LoadInventory;
    }

    private void Start()
    {
        var ChestSaveData = new ChestSaveData(inventorySystem, transform.position, transform.rotation);

        SaveGameManager.data.ChestDictionary.Add(GetComponent<UniqueID>().ID, ChestSaveData);
    }

    public void Interact(Interactor interactor, out bool interactSuccessful)
    {
        OnDynamicInventoryDisplayRequested?.Invoke(inventorySystem);
        interactSuccessful = true;
    }
    public void EndInteraction()
    {
       
    }

    private void LoadInventory(SaveData data)
    {
        //check save data for this chest 

        if(data.ChestDictionary.TryGetValue(GetComponent<UniqueID>().ID, out ChestSaveData chestData))
        {
            this.inventorySystem = chestData.invSystem;
            this.transform.position = chestData.position;
            this.transform.rotation = chestData.rotation;
        }
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