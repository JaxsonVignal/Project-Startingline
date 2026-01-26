using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public abstract class InventoryDisplay : MonoBehaviour
{
    [SerializeField] protected MouseItemData mouseInventoryItem;

    protected InventorySystem inventorySystem;
    protected Dictionary<InventorySlot_UI, InventorySlot> slotDictionary;

    public InventorySystem InventorySystem => inventorySystem;
    public Dictionary<InventorySlot_UI, InventorySlot> SlotDictionary => slotDictionary;

    public abstract void AssignSlot(InventorySystem invToDisplay, int offset);

    // UI listens directly to InventorySlot events
    protected void UpdateSlots(InventorySlot updatedSlot) { }

    // --------------------------------------------------
    // SLOT CLICK LOGIC
    // --------------------------------------------------

    public void SlotClicked(InventorySlot_UI clickedUISlot)
    {
        bool isShiftPressed = Keyboard.current.leftShiftKey.isPressed;

        var clickedSlot = clickedUISlot.AssignedInventorySlot;
        var mouseSlot = mouseInventoryItem.AssignedInventorySlot;

        // PICK UP
        if (clickedSlot.ItemData != null && mouseSlot.ItemData == null)
        {
            if (isShiftPressed && clickedSlot.SplitStack(out InventorySlot halfStack))
            {
                mouseInventoryItem.UpdateMouseSlot(halfStack);
            }
            else
            {
                mouseInventoryItem.UpdateMouseSlot(clickedSlot);
                clickedSlot.ClearSlot();
            }
            return;
        }

        // PLACE
        if (clickedSlot.ItemData == null && mouseSlot.ItemData != null)
        {
            clickedSlot.AssignItem(mouseSlot);
            mouseInventoryItem.ClearSlot();
            return;
        }

        // BOTH HAVE ITEMS
        if (clickedSlot.ItemData != null && mouseSlot.ItemData != null)
        {
            bool sameItem = clickedSlot.ItemData == mouseSlot.ItemData;

            if (sameItem && clickedSlot.RoomLeftInStack(mouseSlot.StackSize))
            {
                clickedSlot.AssignItem(mouseSlot);
                mouseInventoryItem.ClearSlot();
            }
            else
            {
                SwapSlots(clickedSlot);
            }
        }
    }

    private void SwapSlots(InventorySlot targetSlot)
    {
        var temp = new InventorySlot(
            mouseInventoryItem.AssignedInventorySlot.ItemData,
            mouseInventoryItem.AssignedInventorySlot.StackSize
        );

        mouseInventoryItem.ClearSlot();
        mouseInventoryItem.UpdateMouseSlot(targetSlot);

        targetSlot.ClearSlot();
        targetSlot.AssignItem(temp);
    }
}
