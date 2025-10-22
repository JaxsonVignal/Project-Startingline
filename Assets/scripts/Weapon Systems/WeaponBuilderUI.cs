using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponBuilderUI : MonoBehaviour
{
    [Header("References")]
    public TMP_Dropdown weaponDropdown;
    public Transform attachmentListPanel;
    public Transform selectedAttachmentsPanel;
    public GameObject attachmentButtonPrefab;
    public Button finalizeButton;
    public GameObject previewContainer;
    public PlayerInventoryHolder playerInventory;
    public AttachmentMinigameManager minigameManager;

    [Header("Asset Data")]
    public List<AttachmentData> allAttachments; // Keep for lookup reference

    private WeaponData selectedBase;
    private WeaponInstance previewInstance;
    private WeaponRuntime previewRuntime;
    private Dictionary<string, AttachmentData> attachmentLookup = new Dictionary<string, AttachmentData>();
    private List<WeaponData> availableWeapons = new List<WeaponData>();
    private List<AttachmentData> availableAttachments = new List<AttachmentData>();
    private Dictionary<AttachmentData, int> attachmentCounts = new Dictionary<AttachmentData, int>();

    // Track which attachments were on the weapon BEFORE we started editing
    private List<string> originalAttachmentIds = new List<string>();

    void Start()
    {
        if (previewContainer != null)
            previewContainer.SetActive(false);

        // Build attachment lookup
        foreach (var att in allAttachments)
            if (att != null && !string.IsNullOrEmpty(att.id))
                attachmentLookup[att.id] = att;

        finalizeButton.onClick.AddListener(FinalizeWeapon);
    }

    void OnEnable()
    {
        // Refresh items whenever the builder UI is opened
        RefreshAvailableItems();
    }

    /// <summary>
    /// Scan player inventory for weapons and attachments
    /// </summary>
    public void RefreshAvailableItems()
    {
        availableWeapons.Clear();
        availableAttachments.Clear();
        attachmentCounts.Clear();

        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventory not assigned!");
            return;
        }

        // Get unique weapons from inventory
        availableWeapons = playerInventory.PrimaryInventorySystem.GetAllWeapons();

        // Get unique attachments and their counts
        var attachmentSlots = playerInventory.PrimaryInventorySystem.InventorySlots
            .FindAll(slot => slot.ItemData is AttachmentData);

        foreach (var slot in attachmentSlots)
        {
            var att = slot.ItemData as AttachmentData;
            if (att != null)
            {
                if (!availableAttachments.Contains(att))
                {
                    availableAttachments.Add(att);
                }

                // Track how many of each attachment we have
                if (attachmentCounts.ContainsKey(att))
                    attachmentCounts[att] += slot.StackSize;
                else
                    attachmentCounts[att] = slot.StackSize;
            }
        }

        PopulateWeaponDropdown();
        PopulateAttachmentButtons();
    }

    void PopulateWeaponDropdown()
    {
        weaponDropdown.ClearOptions();

        if (availableWeapons.Count == 0)
        {
            weaponDropdown.AddOptions(new List<string> { "No weapons available" });
            weaponDropdown.interactable = false;
            finalizeButton.interactable = false;
            return;
        }

        weaponDropdown.interactable = true;
        finalizeButton.interactable = true; // Re-enable finalize button when weapons are available

        List<string> options = new List<string>();
        foreach (var w in availableWeapons)
        {
            int count = playerInventory.PrimaryInventorySystem.GetItemCount(w);
            options.Add($"{w.name} ({count})");
        }
        weaponDropdown.AddOptions(options);
        weaponDropdown.onValueChanged.RemoveAllListeners();
        weaponDropdown.onValueChanged.AddListener(OnWeaponSelected);

        if (availableWeapons.Count > 0)
            OnWeaponSelected(0);
    }

    void OnWeaponSelected(int index)
    {
        if (index < 0 || index >= availableWeapons.Count) return;

        selectedBase = availableWeapons[index];
        StartPreview();
    }

    void StartPreview()
    {
        if (previewRuntime != null) Destroy(previewRuntime.gameObject);

        // Clear the original attachments list
        originalAttachmentIds.Clear();

        // Check if this weapon has an existing instance with attachments
        WeaponInstance existingInstance = FindExistingWeaponInstance(selectedBase);

        if (existingInstance != null)
        {
            // Use existing instance (preserves attachments)
            previewInstance = existingInstance;

            // Store which attachments were originally on the weapon
            foreach (var att in existingInstance.attachments)
            {
                originalAttachmentIds.Add(att.attachmentId);
            }

            Debug.Log($"Loaded existing weapon instance with {existingInstance.attachments.Count} attachments");
        }
        else
        {
            // Create new instance
            previewInstance = new WeaponInstance
            {
                weaponId = selectedBase.weaponId,
                displayName = selectedBase.name
            };
        }

        GameObject go = Instantiate(selectedBase.weaponPrefab, previewContainer.transform);
        previewRuntime = go.AddComponent<WeaponRuntime>();
        var attachSys = go.AddComponent<WeaponAttachmentSystem>();
        attachSys.weaponData = selectedBase;
        previewRuntime.attachmentSystem = attachSys;
        previewRuntime.InitFromInstance(previewInstance, selectedBase, attachmentLookup);

        UpdateSelectedAttachmentsUI();
    }

    /// <summary>
    /// Find if this weapon already has a WeaponInstance stored in inventory
    /// </summary>
    WeaponInstance FindExistingWeaponInstance(WeaponData weaponData)
    {
        if (playerInventory == null) return null;

        var slots = playerInventory.PrimaryInventorySystem.InventorySlots;

        foreach (var slot in slots)
        {
            if (slot.ItemData == weaponData)
            {
                // Check if this slot has a stored weapon instance
                var instance = WeaponInstanceStorage.GetInstance(slot.UniqueSlotID);
                if (instance != null)
                {
                    return instance;
                }
            }
        }

        return null;
    }

    void PopulateAttachmentButtons()
    {
        foreach (Transform child in attachmentListPanel) Destroy(child.gameObject);

        if (availableAttachments.Count == 0)
        {
            // Create a "No attachments" text object
            GameObject textGO = new GameObject("NoAttachmentsText");
            textGO.transform.SetParent(attachmentListPanel, false);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "No attachments in inventory";
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.gray;
            return;
        }

        foreach (var att in availableAttachments)
        {
            var btnGO = Instantiate(attachmentButtonPrefab, attachmentListPanel);
            var btn = btnGO.GetComponent<Button>();
            var img = btnGO.GetComponentInChildren<Image>();
            if (img != null) img.sprite = att.Icon;

            // Show attachment count
            var texts = btnGO.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0)
            {
                int available = GetAvailableAttachmentCount(att);
                texts[0].text = $"x{available}";
            }

            // Disable button if no more available (all used in preview)
            int availableCount = GetAvailableAttachmentCount(att);
            btn.interactable = availableCount > 0;

            btn.onClick.AddListener(() => AddAttachmentToPreview(att));
        }
    }

    /// <summary>
    /// Get how many of this attachment are available (total - used in preview)
    /// </summary>
    int GetAvailableAttachmentCount(AttachmentData att)
    {
        int total = attachmentCounts.ContainsKey(att) ? attachmentCounts[att] : 0;
        int usedInPreview = previewInstance != null ?
            previewInstance.attachments.FindAll(e => e.attachmentId == att.id).Count : 0;
        return total - usedInPreview;
    }

    public void AddAttachmentToPreview(AttachmentData att)
    {
        if (att == null || previewRuntime == null) return;

        // Check if we have this attachment available
        if (GetAvailableAttachmentCount(att) <= 0)
        {
            Debug.Log($"No {att.name} available in inventory");
            return;
        }

        // Check if attachment type is already equipped
        bool alreadyEquipped = previewInstance.attachments.Exists(e => e.type == att.type);

        if (alreadyEquipped)
        {
            Debug.Log($"Already have a {att.type} equipped. Remove it first.");
            return;
        }

        // Check if this attachment type requires a minigame
        if (RequiresMinigame(att))
        {
            StartAttachmentMinigame(att);
        }
        else
        {
            // No minigame required, add directly
            AddAttachmentDirectly(att);
        }
    }

    /// <summary>
    /// Check if this attachment type requires a minigame
    /// </summary>
    bool RequiresMinigame(AttachmentData att)
    {
        switch (att.type)
        {
            case AttachmentType.Barrel: // Silencers
                return true;
            // Add more types that require minigames
            default:
                return false;
        }
    }

    /// <summary>
    /// Start the minigame for this attachment
    /// </summary>
    void StartAttachmentMinigame(AttachmentData att)
    {
        if (minigameManager == null)
        {
            Debug.LogError("MinigameManager not assigned! Adding attachment directly.");
            AddAttachmentDirectly(att);
            return;
        }

        // Get the appropriate socket for this attachment type
        Transform socket = GetSocketForAttachmentType(att.type);

        if (socket == null)
        {
            Debug.LogError($"No socket found for {att.type}! Adding directly.");
            AddAttachmentDirectly(att);
            return;
        }

        // Disable finalize button while minigame is active
        if (finalizeButton != null)
            finalizeButton.interactable = false;

        // Start the minigame
        minigameManager.StartMinigame(att, selectedBase, socket, (completedAttachment) =>
        {
            // Called when minigame is completed
            AddAttachmentDirectly(completedAttachment);

            // Re-enable finalize button
            if (finalizeButton != null)
                finalizeButton.interactable = true;
        });
    }

    /// <summary>
    /// Get the socket transform for an attachment type
    /// </summary>
    Transform GetSocketForAttachmentType(AttachmentType type)
    {
        if (previewRuntime == null) return null;

        // Get the AttachmentSlotMap from the preview weapon
        AttachmentSlotMap slotMap = previewRuntime.GetComponent<AttachmentSlotMap>();
        if (slotMap == null)
        {
            Debug.LogError("AttachmentSlotMap not found on preview weapon!");
            return null;
        }

        return slotMap.GetSocket(type);
    }

    /// <summary>
    /// Add attachment directly without minigame
    /// </summary>
    void AddAttachmentDirectly(AttachmentData att)
    {
        var entry = new WeaponAttachmentEntry(att.id, att.type, att.localPosition, att.localEuler, att.localScale);
        previewInstance.attachments.Add(entry);
        previewRuntime.attachmentSystem.EquipAttachment(att, entry);

        UpdateSelectedAttachmentsUI();
        PopulateAttachmentButtons(); // Refresh to update counts
    }

    public void RemoveAttachmentFromPreview(AttachmentType type)
    {
        if (previewRuntime == null) return;

        previewInstance.attachments.RemoveAll(e => e.type == type);
        previewRuntime.attachmentSystem.UnequipType(type);

        UpdateSelectedAttachmentsUI();
        PopulateAttachmentButtons(); // Refresh to update counts
    }

    void UpdateSelectedAttachmentsUI()
    {
        foreach (Transform t in selectedAttachmentsPanel) Destroy(t.gameObject);

        foreach (var entry in previewInstance.attachments)
        {
            if (!attachmentLookup.TryGetValue(entry.attachmentId, out var att)) continue;

            var btnGO = Instantiate(attachmentButtonPrefab, selectedAttachmentsPanel);
            var img = btnGO.GetComponentInChildren<Image>();
            if (img != null) img.sprite = att.Icon;

            var btn = btnGO.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => RemoveAttachmentFromPreview(entry.type));

            // Optional: Change button color or add X icon to indicate it's removable
        }
    }

    public void FinalizeWeapon()
    {
        // Check if a minigame is currently active
        if (minigameManager != null && minigameManager.IsMinigameActive())
        {
            Debug.LogWarning("Cannot finalize while minigame is active! Complete or cancel the minigame first.");
            return;
        }

        if (previewInstance == null || previewRuntime == null || selectedBase == null)
        {
            Debug.LogError("Cannot finalize: missing preview or selected weapon");
            return;
        }

        // Find the inventory slot containing this weapon
        InventorySlot weaponSlot = FindInventorySlotForWeapon(selectedBase);

        if (weaponSlot == null)
        {
            Debug.LogError("Cannot find weapon in inventory!");
            return;
        }

        // Verify we still have the weapon in inventory
        if (!playerInventory.PrimaryInventorySystem.ContainsItem(selectedBase, 1))
        {
            Debug.LogError("Selected weapon no longer in inventory!");
            RefreshAvailableItems();
            return;
        }

        // Verify we have NEW attachments that were added (don't check pre-existing ones)
        foreach (var entry in previewInstance.attachments)
        {
            // Skip if this attachment was already on the weapon when we started editing
            if (originalAttachmentIds.Contains(entry.attachmentId))
                continue;

            if (attachmentLookup.TryGetValue(entry.attachmentId, out var att))
            {
                if (!playerInventory.PrimaryInventorySystem.ContainsItem(att, 1))
                {
                    Debug.LogError($"Attachment {att.name} no longer in inventory!");
                    RefreshAvailableItems();
                    return;
                }
            }
        }

        // Remove base weapon from inventory
        if (!playerInventory.PrimaryInventorySystem.RemoveFromInventory(selectedBase, 1))
        {
            Debug.LogError("Failed to remove weapon from inventory!");
            return;
        }

        // Remove the weapon instance from storage (we'll create a new one)
        WeaponInstanceStorage.RemoveInstance(weaponSlot.UniqueSlotID);

        // Remove only NEWLY ADDED attachments from inventory (not pre-existing ones)
        foreach (var entry in previewInstance.attachments)
        {
            // Skip if this attachment was already on the weapon when we started editing
            if (originalAttachmentIds.Contains(entry.attachmentId))
            {
                Debug.Log($"Keeping pre-existing attachment: {entry.attachmentId}");
                continue;
            }

            if (attachmentLookup.TryGetValue(entry.attachmentId, out var att))
            {
                if (!playerInventory.PrimaryInventorySystem.RemoveFromInventory(att, 1))
                {
                    Debug.LogWarning($"Failed to remove attachment {att.name} from inventory!");
                }
                else
                {
                    Debug.Log($"Removed new attachment from inventory: {att.name}");
                }
            }
        }

        // Create the finished weapon pickup
        Vector3 spawnPos = previewRuntime.transform.position;
        Quaternion spawnRot = previewRuntime.transform.rotation;

        GameObject pickupWeapon = Instantiate(selectedBase.weaponPrefab, spawnPos, spawnRot);

        // Setup collider
        SphereCollider col = pickupWeapon.GetComponent<SphereCollider>();
        if (col == null) col = pickupWeapon.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.5f;

        // Ensure UniqueID
        if (!pickupWeapon.TryGetComponent<UniqueID>(out var id))
            pickupWeapon.AddComponent<UniqueID>();

        // Add ItemPickup
        var pickup = pickupWeapon.GetComponent<ItemPickup>();
        if (pickup == null) pickup = pickupWeapon.AddComponent<ItemPickup>();
        pickup.ItemData = selectedBase;
        pickup.pickUpRadius = col.radius;

        // Setup runtime and attachment system
        var runtime = pickupWeapon.GetComponent<WeaponRuntime>();
        if (runtime == null) runtime = pickupWeapon.AddComponent<WeaponRuntime>();

        var attachSys = pickupWeapon.GetComponent<WeaponAttachmentSystem>();
        if (attachSys == null) attachSys = pickupWeapon.AddComponent<WeaponAttachmentSystem>();
        attachSys.weaponData = selectedBase;
        runtime.attachmentSystem = attachSys;

        // Apply ALL attachments (including pre-existing ones)
        foreach (var entry in previewInstance.attachments)
        {
            if (!attachmentLookup.TryGetValue(entry.attachmentId, out var att)) continue;
            attachSys.EquipAttachment(att, entry);
        }

        runtime.InitFromInstance(previewInstance, selectedBase, attachmentLookup);

        // Store weapon instance
        var instanceHolder = pickupWeapon.AddComponent<WeaponInstanceHolder>();
        instanceHolder.weaponInstance = previewInstance;

        // Notify inventory system
        PlayerInventoryHolder.OnPlayerInventoryChanged?.Invoke();

        Debug.Log($"Finalized weapon: {selectedBase.name} with {previewInstance.attachments.Count} attachments");

        // Call WeaponBuilderController to close FIRST (before disabling this GameObject)
        WeaponBuilderController controller = FindObjectOfType<WeaponBuilderController>();
        if (controller != null)
        {
            controller.CloseBuilder();
        }
        else
        {
            // Fallback if no controller exists
            if (previewContainer != null)
                previewContainer.SetActive(false);

            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Find the inventory slot containing the selected weapon
    /// </summary>
    InventorySlot FindInventorySlotForWeapon(WeaponData weaponData)
    {
        if (playerInventory == null) return null;

        var slots = playerInventory.PrimaryInventorySystem.InventorySlots;

        foreach (var slot in slots)
        {
            if (slot.ItemData == weaponData)
            {
                return slot;
            }
        }

        return null;
    }
}