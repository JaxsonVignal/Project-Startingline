using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(menuName = "Inventory System/Item Database")]
public class Database : ScriptableObject
{
    [SerializeField] private List<InventoryItemData> _itemDatabase;

    // NEW: Separate lookup for attachments by their string ID
    private Dictionary<string, AttachmentData> _attachmentLookup;

    [ContextMenu(itemName: "Set IDs")]
    public void SetItemIDs()
    {
        _itemDatabase = new List<InventoryItemData>();

        // Load all InventoryItemData (including WeaponData)
        var foundItems = Resources.LoadAll<InventoryItemData>(path: "ItemData").OrderBy(i => i.ID).ToList();

        // NEW: Also load all AttachmentData
        var foundAttachments = Resources.LoadAll<AttachmentData>(path: "ItemData").ToList();

        // Combine both lists (attachments are also InventoryItemData)
        var allItems = foundItems.Union(foundAttachments).Distinct().OrderBy(i => i.ID).ToList();

        var hasIDinRange = allItems.Where(i => i.ID != -1 && i.ID < allItems.Count).OrderBy(i => i.ID).ToList();
        var hasIDNotInRange = allItems.Where(i => i.ID != -1 && i.ID >= allItems.Count).OrderBy(i => i.ID).ToList();
        var noID = allItems.Where(i => i.ID <= -1).ToList();

        var index = 0;
        for (int i = 0; i < allItems.Count; i++)
        {
            InventoryItemData itemToAdd;
            itemToAdd = hasIDinRange.Find(d => d.ID == i);

            if (itemToAdd != null)
            {
                _itemDatabase.Add(itemToAdd);
            }
            else if (index < noID.Count)
            {
                noID[index].ID = i;
                itemToAdd = noID[index];
                index++;
                _itemDatabase.Add(itemToAdd);
            }
        }

        foreach (var item in hasIDNotInRange)
        {
            _itemDatabase.Add(item);
        }

        // NEW: Build attachment lookup after IDs are set
        BuildAttachmentLookup();

        Debug.Log($"Database: Set IDs for {_itemDatabase.Count} items ({_attachmentLookup?.Count ?? 0} attachments)");
    }

    // NEW: Build the attachment lookup dictionary
    private void BuildAttachmentLookup()
    {
        _attachmentLookup = new Dictionary<string, AttachmentData>();

        foreach (var item in _itemDatabase)
        {
            if (item is AttachmentData attachment)
            {
                if (!string.IsNullOrEmpty(attachment.id))
                {
                    if (!_attachmentLookup.ContainsKey(attachment.id))
                    {
                        _attachmentLookup.Add(attachment.id, attachment);
                    }
                    else
                    {
                        Debug.LogWarning($"Duplicate attachment ID found: {attachment.id}");
                    }
                }
                else
                {
                    Debug.LogWarning($"AttachmentData '{attachment.name}' has no ID set!");
                }
            }
        }
    }

    // Existing method
    public InventoryItemData GetItem(int id)
    {
        return _itemDatabase.Find(i => i.ID == id);
    }

    // NEW: Get attachment by string ID
    public AttachmentData GetAttachment(string attachmentId)
    {
        // Build lookup if not already built (for runtime use)
        if (_attachmentLookup == null || _attachmentLookup.Count == 0)
        {
            BuildAttachmentLookup();
        }

        if (_attachmentLookup.TryGetValue(attachmentId, out var attachment))
        {
            return attachment;
        }

        Debug.LogWarning($"Attachment with ID '{attachmentId}' not found in database");
        return null;
    }

    // NEW: Get the full attachment lookup dictionary
    public Dictionary<string, AttachmentData> GetAttachmentLookup()
    {
        // Build lookup if not already built
        if (_attachmentLookup == null || _attachmentLookup.Count == 0)
        {
            BuildAttachmentLookup();
        }

        return _attachmentLookup;
    }

    // NEW: Get all attachments of a specific type
    public List<AttachmentData> GetAttachmentsByType(AttachmentType type)
    {
        var attachments = new List<AttachmentData>();

        foreach (var item in _itemDatabase)
        {
            if (item is AttachmentData attachment && attachment.type == type)
            {
                attachments.Add(attachment);
            }
        }

        return attachments;
    }

}