using System.Collections.Generic;
using UnityEngine;

public class StaticInventoryDisplay : InventoryDisplay
{
    [SerializeField] private InventoryHolder inventoryHolder;
    [SerializeField] protected InventorySlot_UI[] slots;

    private void OnEnable()
    {
        RefreshStaticDisplay();
    }

    private void Start()
    {
        RefreshStaticDisplay();
    }

    public override void AssignSlot(InventorySystem invToDisplay, int offset)
    {
        slotDictionary = new Dictionary<InventorySlot_UI, InventorySlot>();

        if (invToDisplay == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            var slot = invToDisplay.InventorySlots[i];
            slotDictionary.Add(slots[i], slot);

            slots[i].Init(slot); 
        }
    }
    protected virtual void AfterSlotsAssigned() { }


    private void RefreshStaticDisplay()
    {
        if (inventoryHolder == null)
        {
            Debug.LogWarning($"[StaticInventoryDisplay] No InventoryHolder assigned on {name}");
            return;
        }

        inventorySystem = inventoryHolder.PrimaryInventorySystem;
        AssignSlot(inventorySystem, 0);

        AfterSlotsAssigned(); // ✅ SAFE HOOK
    }



}
