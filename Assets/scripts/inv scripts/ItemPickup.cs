using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(UniqueID))]
public class ItemPickup : MonoBehaviour
{
    public float pickUpRadius = 1;
    public InventoryItemData ItemData;
    private SphereCollider myCollider;
    [SerializeField] private ItemPickUpSaveData itemSaveData;
    private string id;

    private void Awake()
    {
        SaveLoad.OnLoadGame += LoadGame;
        myCollider = GetComponent<SphereCollider>();
        myCollider.isTrigger = true;
        myCollider.radius = pickUpRadius;
        id = GetComponent<UniqueID>().ID;
        itemSaveData = new ItemPickUpSaveData(ItemData, transform.position, transform.rotation);
    }

    private void Start()
    {
        SaveGameManager.data.activeItems.Add(id, itemSaveData);
    }

    private void LoadGame(SaveData data)
    {
        if (data.collectedItems.Contains(id))
        {
            Destroy(this.gameObject);
        }
    }

    private void OnDestroy()
    {
        if (SaveGameManager.data.activeItems.ContainsKey(id))
        {
            SaveGameManager.data.activeItems.Remove(id);
            SaveLoad.OnLoadGame -= LoadGame;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var inventory = other.GetComponent<PlayerInventoryHolder>();
        if (!inventory) return;

        Debug.Log("Player detected, attempting to add item: " + ItemData.name);

        // Store weapon instance BEFORE pickup (in case we need to find the slot after)
        var instanceHolder = GetComponent<WeaponInstanceHolder>();
        WeaponInstance weaponInstance = instanceHolder?.weaponInstance;

        if (inventory.AddToInventory(ItemData, 1))
        {
            // NEW: Transfer weapon attachment data if this is a weapon with attachments
            if (weaponInstance != null)
            {
                // Find the slot that contains this weapon
                InventorySlot targetSlot = FindSlotWithWeapon(inventory, ItemData);
                if (targetSlot != null)
                {
                    WeaponInstanceStorage.StoreInstance(targetSlot.UniqueSlotID, weaponInstance);
                    Debug.Log($"Transferred weapon instance with {weaponInstance.attachments.Count} attachments to slot {targetSlot.UniqueSlotID}");
                }
            }

            SaveGameManager.data.collectedItems.Add(id);
            Destroy(this.gameObject);
        }
    }

    private InventorySlot FindSlotWithWeapon(PlayerInventoryHolder inventory, InventoryItemData weaponData)
    {
        // Search through all slots in the primary inventory system
        foreach (var slot in inventory.PrimaryInventorySystem.InventorySlots)
        {
            if (slot.ItemData == weaponData)
                return slot;
        }

        return null;
    }
}

[System.Serializable]
public struct ItemPickUpSaveData
{
    public InventoryItemData ItemData;
    public Vector3 position;
    public Quaternion rotation;

    public ItemPickUpSaveData(InventoryItemData _itemData, Vector3 _position, Quaternion _rotation)
    {
        ItemData = _itemData;
        position = _position;
        rotation = _rotation;
    }
}