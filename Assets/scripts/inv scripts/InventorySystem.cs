using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

[System.Serializable]
public class InventorySystem
{
    [SerializeField] private List<InventorySlot> inventorySlots;
    public List<InventorySlot> InventorySlots => inventorySlots;
    public int InventorySize => inventorySlots.Count;
    public UnityAction<InventorySlot> OnInventorySlotChanged;

    public InventorySystem(int size)
    {
        inventorySlots = new List<InventorySlot>(size);
        for (int i = 0; i < size; i++)
        {
            inventorySlots.Add(new InventorySlot());
        }
    }

    public bool AddToInventory(InventoryItemData itemToAdd, int amountToAdd)
    {
        if (containsItem(itemToAdd, out List<InventorySlot> invslot))
        {
            foreach (var slot in invslot)
            {
                if (slot.RoomLeftInStack(amountToAdd))
                {
                    slot.AddToStack(amountToAdd);
                    OnInventorySlotChanged?.Invoke(slot);
                    return true;
                }
            }
        }

        if (hasFreeSlot(out InventorySlot freeSlot))
        {
            freeSlot.UpdateInventorySlot(itemToAdd, amountToAdd);
            OnInventorySlotChanged?.Invoke(freeSlot);
            return true;
        }
        return false;
    }

    public bool containsItem(InventoryItemData itemToAdd, out List<InventorySlot> slot)
    {
        slot = InventorySlots.Where(i => i.ItemData == itemToAdd).ToList();
        return slot == null ? false : true;
    }

    public bool hasFreeSlot(out InventorySlot freeSlot)
    {
        freeSlot = InventorySlots.FirstOrDefault(i => i.ItemData == null);
        return freeSlot == null ? false : true;
    }

    // NEW METHODS FOR AMMO SYSTEM

    /// <summary>
    /// Gets the total count of ammo of a specific type in inventory
    /// </summary>
    public int GetAmmoCount(AmmoType ammoType)
    {
        return InventorySlots
            .Where(slot => slot.ItemData is AmmoData ammo && ammo.ammoType == ammoType)
            .Sum(slot => slot.StackSize);
    }

    /// <summary>
    /// Tries to consume ammo from inventory. Returns true if successful.
    /// </summary>
    public bool ConsumeAmmo(AmmoType ammoType, int amount)
    {
        // Check if we have enough ammo first
        if (GetAmmoCount(ammoType) < amount)
            return false;

        // Get all slots with this ammo type
        var ammoSlots = InventorySlots
            .Where(slot => slot.ItemData is AmmoData ammo && ammo.ammoType == ammoType && slot.StackSize > 0)
            .OrderBy(slot => slot.StackSize) // Start with smallest stacks
            .ToList();

        int remaining = amount;

        foreach (var slot in ammoSlots)
        {
            if (remaining <= 0) break;

            int toRemove = Mathf.Min(remaining, slot.StackSize);
            slot.RemoveFromStack(toRemove);
            remaining -= toRemove;

            // Clear slot if empty
            if (slot.StackSize <= 0)
                slot.ClearSlot();

            OnInventorySlotChanged?.Invoke(slot);
        }

        return remaining == 0;
    }

    /// <summary>
    /// Checks if inventory has at least the specified amount of ammo
    /// </summary>
    public bool HasAmmo(AmmoType ammoType, int amount)
    {
        return GetAmmoCount(ammoType) >= amount;
    }
}