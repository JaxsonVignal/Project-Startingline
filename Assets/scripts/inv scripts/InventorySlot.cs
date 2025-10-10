using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class InventorySlot : ISerializationCallbackReceiver
{
    [SerializeField] private InventoryItemData itemData;
    [SerializeField] private int _itemID = -1;
    [SerializeField] private int stackSize;
    [SerializeField] private string _uniqueSlotID; // Unique ID for this specific slot instance

    public InventoryItemData ItemData => itemData;
    public int StackSize => stackSize;
    public string UniqueSlotID => _uniqueSlotID;

    public InventorySlot(InventoryItemData source, int amount)
    {
        itemData = source;
        _itemID = itemData.ID;
        stackSize = amount;
        _uniqueSlotID = Guid.NewGuid().ToString(); // Generate unique ID for this slot
    }

    public InventorySlot()
    {
        _uniqueSlotID = Guid.NewGuid().ToString(); // Generate unique ID even for empty slots
        ClearSlot();
    }

    public void ClearSlot()
    {
        itemData = null;
        _itemID = -1;
        stackSize = -1;
        // Keep the uniqueSlotID - don't regenerate it
    }

    public void AssignItem(InventorySlot invSlot)
    {
        if (itemData == invSlot.itemData)
        {
            AddToStack(invSlot.stackSize);
        }
        else
        {
            itemData = invSlot.itemData;
            _itemID = itemData.ID;
            stackSize = 0;
            AddToStack(invSlot.StackSize);
        }
    }

    public bool RoomLeftInStack(int amountToAdd, out int amountRemaining)
    {
        amountRemaining = ItemData.maxStackSize - stackSize;
        return RoomLeftInStack(amountToAdd);
    }

    public bool RoomLeftInStack(int amountToAdd)
    {
        if (stackSize + amountToAdd <= itemData.maxStackSize)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void AddToStack(int amount)
    {
        stackSize += amount;
    }

    public void RemoveFromStack(int amount)
    {
        stackSize -= amount;
    }

    public void UpdateInventorySlot(InventoryItemData data, int amount)
    {
        itemData = data;
        stackSize = amount;
    }

    public bool SplitStack(out InventorySlot splitStack)
    {
        if (stackSize <= 1)
        {
            splitStack = null;
            return false;
        }
        int halfStack = Mathf.RoundToInt(stackSize / 2);
        RemoveFromStack(halfStack);
        splitStack = new InventorySlot(itemData, halfStack);
        return true;
    }

    public void OnBeforeSerialize()
    {
    }

    public void OnAfterDeserialize()
    {
        if (_itemID == -1) return;
        var db = Resources.Load<Database>(path: "Database");
        itemData = db.GetItem(_itemID);

        // Ensure we have a unique ID after deserialization
        if (string.IsNullOrEmpty(_uniqueSlotID))
        {
            _uniqueSlotID = Guid.NewGuid().ToString();
        }
    }
}