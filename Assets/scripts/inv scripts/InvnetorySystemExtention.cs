using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Extension methods for InventorySystem to make weapon builder integration easier
/// </summary>
public static class InventorySystemExtensions
{
    /// <summary>
    /// Get all inventory slots (including empty ones)
    /// </summary>
    public static List<InventorySlot> GetAllInventorySlots(this InventorySystem inventory)
    {
        return inventory.InventorySlots;
    }

    /// <summary>
    /// Remove a specific item from inventory by reference
    /// </summary>
    public static bool RemoveFromInventory(this InventorySystem inventory, InventoryItemData itemData, int amount)
    {
        // Find all slots containing this item
        var matchingSlots = inventory.InventorySlots
            .Where(slot => slot.ItemData == itemData && slot.StackSize > 0)
            .OrderBy(slot => slot.StackSize) // Remove from smallest stacks first
            .ToList();

        if (matchingSlots.Count == 0)
        {
            Debug.LogWarning($"Item {itemData.Name} not found in inventory");
            return false;
        }

        int remaining = amount;

        foreach (var slot in matchingSlots)
        {
            if (remaining <= 0) break;

            int toRemove = Mathf.Min(remaining, slot.StackSize);
            slot.RemoveFromStack(toRemove);
            remaining -= toRemove;

            // Clear slot if empty
            if (slot.StackSize <= 0)
            {
                slot.ClearSlot();
            }

            inventory.OnInventorySlotChanged?.Invoke(slot);
        }

        return remaining == 0;
    }

    /// <summary>
    /// Get all items of a specific type from inventory
    /// </summary>
    public static List<T> GetItemsOfType<T>(this InventorySystem inventory) where T : InventoryItemData
    {
        return inventory.InventorySlots
            .Where(slot => slot.ItemData is T)
            .Select(slot => slot.ItemData as T)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Check if inventory contains a specific item
    /// </summary>
    public static bool ContainsItem(this InventorySystem inventory, InventoryItemData itemData, int amount = 1)
    {
        int totalCount = inventory.InventorySlots
            .Where(slot => slot.ItemData == itemData)
            .Sum(slot => slot.StackSize);

        return totalCount >= amount;
    }

    /// <summary>
    /// Get total count of a specific item in inventory
    /// </summary>
    public static int GetItemCount(this InventorySystem inventory, InventoryItemData itemData)
    {
        return inventory.InventorySlots
            .Where(slot => slot.ItemData == itemData)
            .Sum(slot => slot.StackSize);
    }

    /// <summary>
    /// Get all unique weapons in inventory
    /// </summary>
    public static List<WeaponData> GetAllWeapons(this InventorySystem inventory)
    {
        return inventory.GetItemsOfType<WeaponData>();
    }

    /// <summary>
    /// Get all unique attachments in inventory
    /// </summary>
    public static List<AttachmentData> GetAllAttachments(this InventorySystem inventory)
    {
        return inventory.GetItemsOfType<AttachmentData>();
    }
}