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

        // Disable ADS script on preview weapon
        ADS adsScript = go.GetComponent<ADS>();
        if (adsScript != null)
        {
            adsScript.enabled = false;
            Debug.Log("Disabled ADS script on weapon preview");
        }

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

        // Filter attachments to only show those compatible with the selected weapon
        List<AttachmentData> compatibleAttachments = new List<AttachmentData>();

        if (selectedBase != null)
        {
            foreach (var att in availableAttachments)
            {
                if (selectedBase.IsAttachmentAllowed(att))
                {
                    compatibleAttachments.Add(att);
                }
            }
        }
        else
        {
            // No weapon selected, show all attachments
            compatibleAttachments = availableAttachments;
        }

        if (compatibleAttachments.Count == 0)
        {
            // Create a "No compatible attachments" text object
            GameObject textGO = new GameObject("NoCompatibleAttachmentsText");
            textGO.transform.SetParent(attachmentListPanel, false);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "No compatible attachments for this weapon";
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.gray;
            return;
        }

        foreach (var att in compatibleAttachments)
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

        Debug.Log($"AddAttachmentToPreview called for: {att.name} (Type: {att.type})");

        // Check if this attachment is compatible with the selected weapon
        if (selectedBase != null && !selectedBase.IsAttachmentAllowed(att))
        {
            Debug.LogWarning($"Attachment {att.name} is not compatible with {selectedBase.name}!");
            return;
        }

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
        bool requiresMinigame = RequiresMinigame(att);
        Debug.Log($"RequiresMinigame({att.type}): {requiresMinigame}");

        if (requiresMinigame)
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
            case AttachmentType.Barrel:      // Silencers
            case AttachmentType.Sight:       // Scopes
            case AttachmentType.Underbarrel: // Underbarrel attachments
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
        Debug.Log($"StartAttachmentMinigame called for {att.name} (Type: {att.type})");

        if (minigameManager == null)
        {
            Debug.LogError("MinigameManager not assigned! Adding attachment directly.");
            AddAttachmentDirectly(att);
            return;
        }

        Debug.Log("MinigameManager is assigned");

        // Get the appropriate socket for this attachment type
        Transform socket = GetSocketForAttachmentType(att.type);

        if (socket == null)
        {
            Debug.LogError($"No socket found for {att.type}! Adding directly.");
            AddAttachmentDirectly(att);
            return;
        }

        Debug.Log($"Socket found: {socket.name}");

        // Disable finalize button and attachment buttons while minigame is active
        if (finalizeButton != null)
            finalizeButton.interactable = false;

        DisableAllAttachmentButtons();

        Debug.Log("About to call minigameManager.StartMinigame...");

        // Start the minigame
        minigameManager.StartMinigame(att, selectedBase, socket, (completedAttachment) =>
        {
            Debug.Log("Minigame completion callback triggered!");
            // Called when minigame is completed
            AddAttachmentDirectly(completedAttachment);

            // Re-enable finalize button and attachment buttons
            if (finalizeButton != null)
                finalizeButton.interactable = true;

            PopulateAttachmentButtons(); // This will re-enable available buttons
        });
    }

    /// <summary>
    /// Disable all attachment buttons (used during minigame)
    /// </summary>
    void DisableAllAttachmentButtons()
    {
        foreach (Transform child in attachmentListPanel)
        {
            var button = child.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
            }
        }
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

        // Find the attachment being removed
        var attachmentEntry = previewInstance.attachments.Find(e => e.type == type);

        if (attachmentEntry != null)
        {
            // Check if this was an ORIGINAL attachment (already on the weapon)
            bool wasOriginal = originalAttachmentIds.Contains(attachmentEntry.attachmentId);

            if (wasOriginal)
            {
                // This was a pre-existing attachment - add it back to inventory
                if (attachmentLookup.TryGetValue(attachmentEntry.attachmentId, out var att))
                {
                    Debug.Log($"Removing PRE-EXISTING attachment: {att.name} - adding back to inventory");

                    if (playerInventory.PrimaryInventorySystem.AddToInventory(att, 1))
                    {
                        Debug.Log($"Successfully added {att.name} back to inventory");
                        // Remove from original list so it won't be kept on finalize
                        originalAttachmentIds.Remove(attachmentEntry.attachmentId);
                    }
                    else
                    {
                        Debug.LogError($"Failed to add {att.name} back to inventory - inventory full?");
                    }
                }
            }
            else
            {
                // This was a newly added attachment - it's already in inventory, just being removed from preview
                Debug.Log($"Removing NEWLY ADDED attachment: {attachmentEntry.attachmentId} - already in inventory");
            }
        }

        // Remove from preview
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

        Debug.Log($"=== FINALIZE WEAPON DEBUG ===");
        Debug.Log($"Total attachments in preview: {previewInstance.attachments.Count}");
        Debug.Log($"Original attachments count: {originalAttachmentIds.Count}");

        // Verify we have NEW attachments that were added (don't check pre-existing ones)
        foreach (var entry in previewInstance.attachments)
        {
            Debug.Log($"Checking attachment: {entry.attachmentId} (Type: {entry.type})");

            // Skip if this attachment was already on the weapon when we started editing
            if (originalAttachmentIds.Contains(entry.attachmentId))
            {
                Debug.Log($"  -> This is a PRE-EXISTING attachment, skipping inventory check");
                continue;
            }

            Debug.Log($"  -> This is a NEW attachment, checking inventory...");

            if (attachmentLookup.TryGetValue(entry.attachmentId, out var att))
            {
                Debug.Log($"  -> Found in lookup: {att.name}");

                if (!playerInventory.PrimaryInventorySystem.ContainsItem(att, 1))
                {
                    Debug.LogError($"Attachment {att.name} no longer in inventory!");
                    RefreshAvailableItems();
                    return;
                }
                else
                {
                    Debug.Log($"  -> Verified in inventory");
                }
            }
            else
            {
                Debug.LogError($"  -> NOT FOUND in attachmentLookup! ID: {entry.attachmentId}");
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

        Debug.Log($"=== REMOVING ATTACHMENTS FROM INVENTORY ===");

        // Remove only NEWLY ADDED attachments from inventory (not pre-existing ones)
        foreach (var entry in previewInstance.attachments)
        {
            Debug.Log($"Processing attachment: {entry.attachmentId} (Type: {entry.type})");

            // Skip if this attachment was already on the weapon when we started editing
            if (originalAttachmentIds.Contains(entry.attachmentId))
            {
                Debug.Log($"  -> PRE-EXISTING: Keeping {entry.attachmentId}");
                continue;
            }

            Debug.Log($"  -> NEW ATTACHMENT: Attempting to remove from inventory...");

            if (attachmentLookup.TryGetValue(entry.attachmentId, out var att))
            {
                Debug.Log($"  -> Found in lookup: {att.name} (Type: {att.type})");
                Debug.Log($"  -> Calling RemoveFromInventory...");

                if (!playerInventory.PrimaryInventorySystem.RemoveFromInventory(att, 1))
                {
                    Debug.LogWarning($"  -> FAILED to remove attachment {att.name} from inventory!");
                }
                else
                {
                    Debug.Log($"  -> SUCCESS: Removed {att.name} from inventory");
                }
            }
            else
            {
                Debug.LogError($"  -> ERROR: Could not find attachment {entry.attachmentId} in attachmentLookup!");
                Debug.LogError($"  -> Available IDs in lookup:");
                foreach (var kvp in attachmentLookup)
                {
                    Debug.LogError($"     - {kvp.Key} = {kvp.Value.name}");
                }
            }
        }

        Debug.Log($"=== CREATING FINALIZED WEAPON ===");

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

        // Debug log all attachments before applying
        Debug.Log($"Applying {previewInstance.attachments.Count} attachments to finalized weapon:");
        foreach (var entry in previewInstance.attachments)
        {
            if (attachmentLookup.TryGetValue(entry.attachmentId, out var att))
            {
                Debug.Log($"  - {att.name} (Type: {att.type}, ID: {entry.attachmentId})");
            }
        }

        // Apply ALL attachments (including pre-existing ones) BEFORE initializing runtime
        foreach (var entry in previewInstance.attachments)
        {
            if (!attachmentLookup.TryGetValue(entry.attachmentId, out var att))
            {
                Debug.LogWarning($"Could not find attachment with ID: {entry.attachmentId}");
                continue;
            }

            Debug.Log($"Equipping attachment: {att.name} at socket type: {att.type}");
            attachSys.EquipAttachment(att, entry);
        }

        // Initialize runtime AFTER attachments are equipped
        runtime.InitFromInstance(previewInstance, selectedBase, attachmentLookup);

        // Handle iron sight visibility based on attachments
        HandleIronSightVisibility(pickupWeapon, selectedBase, previewInstance, attachmentLookup);

        // Store weapon instance
        var instanceHolder = pickupWeapon.AddComponent<WeaponInstanceHolder>();
        instanceHolder.weaponInstance = previewInstance;

        // Notify inventory system
        PlayerInventoryHolder.OnPlayerInventoryChanged?.Invoke();

        Debug.Log($"=== FINALIZED: {selectedBase.name} with {previewInstance.attachments.Count} attachments ===");

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

    /// <summary>
    /// Disables the iron sight part if a sight attachment is equipped
    /// Call this after equipping attachments on any weapon
    /// </summary>
    public static void HandleIronSightVisibility(GameObject weaponObject, WeaponData weaponData, WeaponInstance weaponInstance, Dictionary<string, AttachmentData> attachmentLookup)
    {
        if (weaponObject == null || weaponData == null || weaponInstance == null) return;

        // Check if weapon has a sight attachment equipped
        bool hasSightAttachment = false;
        foreach (var entry in weaponInstance.attachments)
        {
            if (attachmentLookup.TryGetValue(entry.attachmentId, out var att))
            {
                if (att.type == AttachmentType.Sight)
                {
                    hasSightAttachment = true;
                    break;
                }
            }
        }

        // Get all parts that should be disabled/enabled
        var partsToToggle = weaponData.GetPartsToDisableWithSight();

        if (partsToToggle == null || partsToToggle.Count == 0)
            return;

        // Process each part
        foreach (var partPath in partsToToggle)
        {
            if (string.IsNullOrEmpty(partPath))
                continue;

            Transform partTransform = weaponObject.transform.Find(partPath);

            if (partTransform != null)
            {
                if (hasSightAttachment)
                {
                    // Disable the part when sight is equipped
                    partTransform.gameObject.SetActive(false);
                    Debug.Log($"[HandleIronSightVisibility] Disabled part: {partPath}");
                }
                else
                {
                    // Re-enable the part when no sight is equipped
                    partTransform.gameObject.SetActive(true);
                    Debug.Log($"[HandleIronSightVisibility] Re-enabled part: {partPath}");
                }
            }
            else
            {
                Debug.LogWarning($"[HandleIronSightVisibility] Could not find part at path: {partPath}");
            }
        }
    }
}