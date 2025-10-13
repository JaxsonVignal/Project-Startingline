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
        var ChestSaveData = new InventorySaveData(primaryInventorySystem, transform.position, transform.rotation);

        SaveGameManager.data.ChestDictionary.Add(GetComponent<UniqueID>().ID, ChestSaveData);
    }

    public void Interact(Interactor interactor, out bool interactSuccessful)
    {
        OnDynamicInventoryDisplayRequested?.Invoke(primaryInventorySystem, 0);
        interactSuccessful = true;
    }
    public void EndInteraction()
    {
       
    }

    protected override void LoadInventory(SaveData data)
    {
        //check save data for this chest 

        if(data.ChestDictionary.TryGetValue(GetComponent<UniqueID>().ID, out InventorySaveData chestData))
        {
            this.primaryInventorySystem = chestData.invSystem;
            this.transform.position = chestData.position;
            this.transform.rotation = chestData.rotation;
        }
    }



}

