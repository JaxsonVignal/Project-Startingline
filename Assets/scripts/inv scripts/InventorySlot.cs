using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public class InventorySlot : ISerializationCallbackReceiver
{
    // --------------------------------------------------
    // EVENTS
    // --------------------------------------------------

    public event Action OnSlotChanged;

    // --------------------------------------------------
    // DATA
    // --------------------------------------------------

    [SerializeField] private InventoryItemData itemData;
    [SerializeField] private int _itemID = -1;
    [SerializeField] private int stackSize;
    [SerializeField] private string _uniqueSlotID;

    public InventoryItemData ItemData => itemData;
    public int StackSize => stackSize;
    public string UniqueSlotID => _uniqueSlotID;

    // --------------------------------------------------
    // CONSTRUCTORS
    // --------------------------------------------------

    public InventorySlot(InventoryItemData source, int amount)
    {
        itemData = source;
        _itemID = source != null ? source.ID : -1;
        stackSize = amount;
        _uniqueSlotID = Guid.NewGuid().ToString();
    }

    public InventorySlot()
    {
        _uniqueSlotID = Guid.NewGuid().ToString();
        ClearSlot();
    }

    // --------------------------------------------------
    // CORE
    // --------------------------------------------------

    public void ClearSlot()
    {
        if (itemData is WeaponData)
        {
            WeaponInstanceStorage.RemoveInstance(_uniqueSlotID);
            Debug.Log($"[InventorySlot] Removed weapon instance for slot {_uniqueSlotID}");
        }

        itemData = null;
        _itemID = -1;
        stackSize = -1;

        OnSlotChanged?.Invoke();
    }

    public void AssignItem(InventorySlot invSlot)
    {
        bool wasWeapon = itemData is WeaponData;
        bool isWeapon = invSlot.itemData is WeaponData;

        if (wasWeapon && !isWeapon)
            WeaponInstanceStorage.RemoveInstance(_uniqueSlotID);

        if (itemData == invSlot.itemData)
        {
            AddToStack(invSlot.stackSize);
        }
        else
        {
            itemData = invSlot.itemData;
            _itemID = itemData != null ? itemData.ID : -1;
            stackSize = 0;
            AddToStack(invSlot.stackSize);
        }

        if (isWeapon)
        {
            var instance = WeaponInstanceStorage.GetInstance(invSlot.UniqueSlotID);
            if (instance != null)
            {
                WeaponInstanceStorage.RemoveInstance(invSlot.UniqueSlotID);
                WeaponInstanceStorage.StoreInstance(_uniqueSlotID, instance);
            }
        }

        OnSlotChanged?.Invoke();
    }

    // --------------------------------------------------
    // STACKING
    // --------------------------------------------------

    public bool RoomLeftInStack(int amountToAdd, out int amountRemaining)
    {
        amountRemaining = itemData.maxStackSize - stackSize;
        return RoomLeftInStack(amountToAdd);
    }

    public bool RoomLeftInStack(int amountToAdd)
    {
        return itemData != null && stackSize + amountToAdd <= itemData.maxStackSize;
    }

    public void AddToStack(int amount)
    {
        stackSize += amount;
        OnSlotChanged?.Invoke();
    }

    public void RemoveFromStack(int amount)
    {
        stackSize -= amount;

        if (stackSize <= 0)
            ClearSlot();
        else
            OnSlotChanged?.Invoke();
    }

    // --------------------------------------------------
    // UTIL
    // --------------------------------------------------

    public void UpdateInventorySlot(InventoryItemData data, int amount)
    {
        itemData = data;
        _itemID = data != null ? data.ID : -1;
        stackSize = amount;

        OnSlotChanged?.Invoke();
    }

    public bool SplitStack(out InventorySlot splitStack)
    {
        if (stackSize <= 1)
        {
            splitStack = null;
            return false;
        }

        int halfStack = Mathf.RoundToInt(stackSize / 2f);
        RemoveFromStack(halfStack);

        splitStack = new InventorySlot(itemData, halfStack);

        if (itemData is WeaponData)
        {
            var instance = WeaponInstanceStorage.GetInstance(_uniqueSlotID);
            if (instance != null)
            {
                WeaponInstanceStorage.StoreInstance(splitStack.UniqueSlotID, instance);
            }
        }

        return true;
    }

    // --------------------------------------------------
    // SERIALIZATION
    // --------------------------------------------------

    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize()
    {
        if (_itemID == -1) return;

        var db = Resources.Load<Database>("Database");
        itemData = db.GetItem(_itemID);

        if (string.IsNullOrEmpty(_uniqueSlotID))
            _uniqueSlotID = Guid.NewGuid().ToString();
    }
}
