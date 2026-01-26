using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DynamicInventoryDisplay : InventoryDisplay
{
    [SerializeField] protected InventorySlot_UI slotPrefab;

    private void Start()
    {
        // optional
    }

    public void RefreshDynamicInventory(InventorySystem invToDisplay, int offset)
    {
        ClearSlots();
        inventorySystem = invToDisplay;
        AssignSlot(invToDisplay, offset);
    }

    public override void AssignSlot(InventorySystem invToDisplay, int offset)
    {
        slotDictionary = new Dictionary<InventorySlot_UI, InventorySlot>();

        if (invToDisplay == null)
            return;

        for (int i = offset; i < invToDisplay.InventorySize; i++)
        {
            var uiSlot = Instantiate(slotPrefab, transform);
            var slot = invToDisplay.InventorySlots[i];

            slotDictionary.Add(uiSlot, slot);
            uiSlot.Init(slot);
        }
    }

    private void ClearSlots()
    {
        foreach (var item in transform.Cast<Transform>())
            Destroy(item.gameObject);

        slotDictionary?.Clear();
    }
}
