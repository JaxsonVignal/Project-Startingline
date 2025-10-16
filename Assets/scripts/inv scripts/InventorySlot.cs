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
        // If this slot had a weapon instance, remove it from storage
        if (itemData is WeaponData)
        {
            WeaponInstanceStorage.RemoveInstance(_uniqueSlotID);
            Debug.Log($"Removed weapon instance for cleared slot {_uniqueSlotID}");
        }

        itemData = null;
        _itemID = -1;
        stackSize = -1;
        // Keep the uniqueSlotID - don't regenerate it
    }

    public void AssignItem(InventorySlot invSlot)
    {
        bool wasWeapon = itemData is WeaponData;
        bool isWeapon = invSlot.itemData is WeaponData;

        // If we are replacing a weapon with something else, clean up
        if (wasWeapon && !isWeapon)
        {
            WeaponInstanceStorage.RemoveInstance(_uniqueSlotID);
        }

        if (itemData == invSlot.itemData)
        {
            AddToStack(invSlot.stackSize);
        }
        else
        {
            itemData = invSlot.itemData;
            _itemID = itemData != null ? itemData.ID : -1;
            stackSize = 0;
            AddToStack(invSlot.StackSize);
        }

        // Move weapon instance if needed
        if (isWeapon)
        {
            var instance = WeaponInstanceStorage.GetInstance(invSlot.UniqueSlotID);
            if (instance != null)
            {
                WeaponInstanceStorage.RemoveInstance(invSlot.UniqueSlotID);
                WeaponInstanceStorage.StoreInstance(_uniqueSlotID, instance);
                Debug.Log($"Moved weapon instance from {invSlot.UniqueSlotID} -> {_uniqueSlotID}");
            }
        }
    }

    public bool RoomLeftInStack(int amountToAdd, out int amountRemaining)
    {
        amountRemaining = ItemData.maxStackSize - stackSize;
        return RoomLeftInStack(amountToAdd);
    }

    public bool RoomLeftInStack(int amountToAdd)
    {
        return (stackSize + amountToAdd <= itemData.maxStackSize);
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

        // If weapon, also copy its instance (since both halves represent the same weapon)
        if (itemData is WeaponData)
        {
            var instance = WeaponInstanceStorage.GetInstance(_uniqueSlotID);
            if (instance != null)
            {
                WeaponInstanceStorage.StoreInstance(splitStack.UniqueSlotID, instance);
                Debug.Log($"Copied weapon instance to split slot {splitStack.UniqueSlotID}");
            }
        }

        return true;
    }

    public void OnBeforeSerialize() { }

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
