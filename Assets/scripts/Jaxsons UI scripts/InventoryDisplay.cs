using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine;

public abstract class InventoryDisplay : MonoBehaviour
{
    [SerializeField] MouseItemData mouseInventoryItem;
    protected InventorySystem inventorySystem;

    protected Dictionary<InventorySlot_UI, InventorySlot> slotDictionary;
    public InventorySystem InventorySystem => inventorySystem;

    public Dictionary<InventorySlot_UI, InventorySlot> SlotDictionary => slotDictionary;

    public abstract void AssignSlot(InventorySystem invToDisplay);

    public virtual void Start()
    {

    }

    protected virtual void UpdateSlots(InventorySlot updatedSlot)
    {
        foreach ( var slot in SlotDictionary)
        {
            if(slot.Value == updatedSlot)
            {
                slot.Key.UpdateUISlot(updatedSlot);
            }
        }
    }

    public void SlotClicked(InventorySlot_UI clickedUISlot)
    {
        //clicked slot has item mouse does not have item - pick up item 

        if(clickedUISlot.AssignedInventorySlot.ItemData != null && mouseInventoryItem.AssignedInventorySlot.ItemData == null)
        {
            mouseInventoryItem.UpdateMouseSlot(clickedUISlot.AssignedInventorySlot);
            clickedUISlot.clearSlot();
            return;
        }

        //clicked slot doesnt have item but mouse does - place mouse item in slot 

        //both slots have item - decide what to do 

        //if items are same combine stack if mouse stack + stack is less then stack max 

        //if different items then swap items 
    }
}
