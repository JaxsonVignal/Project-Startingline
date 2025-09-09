using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class chestInventory : InventoryHolder, IInteractable
{
   public UnityAction<IInteractable> OnInteractionComplete {  get; set; }

    public void EndInteraction()
    {
       
    }

    public void Interact(Interactor interactor, out bool interactSuccessful)
    {
        OnDynamicInventoryDisplayRequested?.Invoke(InventorySystem);
        interactSuccessful = true;
    }
}
