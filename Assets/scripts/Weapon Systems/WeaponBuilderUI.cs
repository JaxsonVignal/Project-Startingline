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
    public Camera previewCamera; // ADDED: Reference to preview camera so we don't destroy it

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
    /// FIXED: Clean up preview container while it's still active, but DON'T destroy cameras
    /// Called by WeaponBuilderController BEFORE disabling the container
    /// </summary>
    public void CleanupPreviewContainer()
    {
        Debug.Log("=== CLEANUP PREVIEW CONTAINER ===");

        if (previewContainer != null && previewContainer.activeSelf)
        {
            Debug.Log($"Preview container is active - cleaning up {previewContainer.transform.childCount} children");

            // Destroy ALL children EXCEPT cameras (check by component type)
            var childrenToDestroy = new List<GameObject>();
            foreach (Transform child in previewContainer.transform)
            {
                // Skip any GameObject with a Camera component
                if (child.GetComponent<Camera>() != null)
                {
                    Debug.Log($"Skipping camera object: {child.gameObject.name}");
                    continue;
                }

                childrenToDestroy.Add(child.gameObject);
                Debug.Log($"Queuing for immediate destruction: {child.gameObject.name}");
            }

            // Use DestroyImmediate for instant cleanup while still active
            foreach (var child in childrenToDestroy)
            {
                Debug.Log($"Immediately destroying: {child.name}");
                DestroyImmediate(child);
            }

            Debug.Log($"Cleanup complete - preview container now has {previewContainer.transform.childCount} children");
        }
        else
        {
            Debug.LogWarning("Preview container is null or inactive - cannot clean up");
        }

        // Clear references
        previewRuntime = null;
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
        finalizeButton.interactable = true;

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
            compatibleAttachments = availableAttachments;
        }

        if (compatibleAttachments.Count == 0)
        {
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

            var texts = btnGO.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0)
            {
                int available = GetAvailableAttachmentCount(att);
                texts[0].text = $"x{available}";
            }

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
            // Special handling for barrel attachments - they need to go through minigame to be replaced
            if (att.type == AttachmentType.Barrel && RequiresMinigame(att))
            {
                Debug.Log($"Barrel attachment already equipped. Starting replacement minigame...");
                StartBarrelReplacementMinigame(att);
                return;
            }

            // Special handling for underbarrel attachments - they need to go through minigame to be replaced
            if (att.type == AttachmentType.Underbarrel && RequiresMinigame(att))
            {
                Debug.Log($"Underbarrel attachment already equipped. Starting replacement minigame...");
                StartUnderbarrelReplacementMinigame(att);
                return;
            }

            // Special handling for scope/sight attachments - they need to go through minigame to be replaced
            if (att.type == AttachmentType.Sight && RequiresMinigame(att))
            {
                Debug.Log($"Sight attachment already equipped. Starting replacement minigame...");
                StartScopeReplacementMinigame(att);
                return;
            }

            if (att.type == AttachmentType.SideRail && RequiresMinigame(att))
            {
                Debug.Log($"Siderail attachment already equipped. Starting replacement minigame...");
                StartSiderailReplacementMinigame(att);
                return;
            }

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
            case AttachmentType.Barrel:
            case AttachmentType.Sight:
            case AttachmentType.Underbarrel:
            case AttachmentType.Magazine:
            case AttachmentType.SideRail:  // ADD THIS LINE
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Start a siderail replacement minigame (slide old off, slide new on)
    /// </summary>
    void StartSiderailReplacementMinigame(AttachmentData newSiderailAttachment)
    {
        Debug.Log($"StartSiderailReplacementMinigame called for {newSiderailAttachment.name}");

        if (minigameManager == null)
        {
            Debug.LogError("MinigameManager not assigned!");
            return;
        }

        // Get the currently equipped siderail attachment
        var currentSiderailEntry = previewInstance.attachments.Find(e => e.type == AttachmentType.SideRail);

        if (currentSiderailEntry == null)
        {
            Debug.LogError("No current siderail attachment found!");
            return;
        }

        AttachmentData currentSiderailAttachment = null;
        if (attachmentLookup.TryGetValue(currentSiderailEntry.attachmentId, out var att))
        {
            currentSiderailAttachment = att;
        }

        if (currentSiderailAttachment == null)
        {
            Debug.LogError($"Could not find current siderail attachment in lookup: {currentSiderailEntry.attachmentId}");
            return;
        }

        Debug.Log($"Current siderail: {currentSiderailAttachment.name}, New siderail: {newSiderailAttachment.name}");

        // Get the siderail socket
        Transform socket = GetSocketForAttachmentType(AttachmentType.SideRail);

        if (socket == null)
        {
            Debug.LogError($"No socket found for Siderail type!");
            return;
        }

        // Disable finalize button and attachment buttons while minigame is active
        if (finalizeButton != null)
            finalizeButton.interactable = false;

        DisableAllAttachmentButtons();

        // Start the siderail replacement minigame
        minigameManager.StartSiderailReplacementMinigame(
            currentSiderailAttachment,
            newSiderailAttachment,
            selectedBase,
            socket,
            (replacedAttachment) =>
            {
                Debug.Log("Siderail replacement minigame complete!");

                // Remove the old siderail attachment from preview
                previewInstance.attachments.RemoveAll(e => e.type == AttachmentType.SideRail);
                previewRuntime.attachmentSystem.UnequipType(AttachmentType.SideRail);

                // CRITICAL: Check if the old siderail was an ORIGINAL attachment
                bool wasOriginal = originalAttachmentIds.Contains(currentSiderailEntry.attachmentId);

                if (wasOriginal)
                {
                    // This was on the weapon when we opened the builder - return it to inventory
                    Debug.Log($"Old siderail {currentSiderailAttachment.name} was ORIGINAL - returning to inventory");

                    if (playerInventory.PrimaryInventorySystem.AddToInventory(currentSiderailAttachment, 1))
                    {
                        Debug.Log($"Successfully returned {currentSiderailAttachment.name} to inventory");
                        originalAttachmentIds.Remove(currentSiderailEntry.attachmentId);
                    }
                    else
                    {
                        Debug.LogError($"Failed to return {currentSiderailAttachment.name} to inventory - inventory full?");
                    }
                }
                else
                {
                    Debug.Log($"Old siderail {currentSiderailAttachment.name} was newly added - already in inventory");
                }

                // Add the new siderail attachment
                AddAttachmentDirectly(replacedAttachment);

                // Re-enable finalize button and attachment buttons
                if (finalizeButton != null)
                    finalizeButton.interactable = true;

                // Note: AddAttachmentDirectly already calls PopulateAttachmentButtons(), so we don't need to call it again
            });
    }


    /// <summary>
    /// Start a barrel replacement minigame (remove old, attach new)
    /// </summary>
    void StartBarrelReplacementMinigame(AttachmentData newBarrelAttachment)
    {
        Debug.Log($"StartBarrelReplacementMinigame called for {newBarrelAttachment.name}");

        if (minigameManager == null)
        {
            Debug.LogError("MinigameManager not assigned!");
            return;
        }

        // Get the currently equipped barrel attachment
        var currentBarrelEntry = previewInstance.attachments.Find(e => e.type == AttachmentType.Barrel);

        if (currentBarrelEntry == null)
        {
            Debug.LogError("No current barrel attachment found!");
            return;
        }

        AttachmentData currentBarrelAttachment = null;
        if (attachmentLookup.TryGetValue(currentBarrelEntry.attachmentId, out var att))
        {
            currentBarrelAttachment = att;
        }

        if (currentBarrelAttachment == null)
        {
            Debug.LogError($"Could not find current barrel attachment in lookup: {currentBarrelEntry.attachmentId}");
            return;
        }

        Debug.Log($"Current barrel: {currentBarrelAttachment.name}, New barrel: {newBarrelAttachment.name}");

        // Get the barrel socket
        Transform socket = GetSocketForAttachmentType(AttachmentType.Barrel);

        if (socket == null)
        {
            Debug.LogError($"No socket found for Barrel type!");
            return;
        }

        // Disable finalize button and attachment buttons while minigame is active
        if (finalizeButton != null)
            finalizeButton.interactable = false;

        DisableAllAttachmentButtons();

        // Start the barrel replacement minigame
        minigameManager.StartBarrelReplacementMinigame(
            currentBarrelAttachment,
            newBarrelAttachment,
            selectedBase,
            socket,
            (replacedAttachment) =>
            {
                Debug.Log("Barrel replacement minigame complete!");

                // Remove the old barrel attachment from preview (WITHOUT re-enabling default parts)
                previewInstance.attachments.RemoveAll(e => e.type == AttachmentType.Barrel);
                previewRuntime.attachmentSystem.UnequipType(AttachmentType.Barrel);

                // NOTE: We DON'T call ReEnableDefaultBarrelParts() here because we're replacing with another barrel
                // The default parts should stay disabled

                // CRITICAL: Check if the old barrel was an ORIGINAL attachment
                bool wasOriginal = originalAttachmentIds.Contains(currentBarrelEntry.attachmentId);

                if (wasOriginal)
                {
                    // This was on the weapon when we opened the builder - return it to inventory
                    Debug.Log($"Old barrel {currentBarrelAttachment.name} was ORIGINAL - returning to inventory");

                    if (playerInventory.PrimaryInventorySystem.AddToInventory(currentBarrelAttachment, 1))
                    {
                        Debug.Log($"Successfully returned {currentBarrelAttachment.name} to inventory");
                        originalAttachmentIds.Remove(currentBarrelEntry.attachmentId);
                    }
                    else
                    {
                        Debug.LogError($"Failed to return {currentBarrelAttachment.name} to inventory - inventory full?");
                    }
                }
                else
                {
                    Debug.Log($"Old barrel {currentBarrelAttachment.name} was newly added - already in inventory");
                }


                // Add the new barrel attachment
                AddAttachmentDirectly(replacedAttachment);

                // Re-enable finalize button and attachment buttons
                if (finalizeButton != null)
                    finalizeButton.interactable = true;

                // Note: AddAttachmentDirectly already calls PopulateAttachmentButtons(), so we don't need to call it again
            });
    }

    /// <summary>
    /// Start an underbarrel replacement minigame (remove old, attach new)
    /// </summary>
    void StartUnderbarrelReplacementMinigame(AttachmentData newUnderbarrelAttachment)
    {
        Debug.Log($"StartUnderbarrelReplacementMinigame called for {newUnderbarrelAttachment.name}");

        if (minigameManager == null)
        {
            Debug.LogError("MinigameManager not assigned!");
            return;
        }

        // Get the currently equipped underbarrel attachment
        var currentUnderbarrelEntry = previewInstance.attachments.Find(e => e.type == AttachmentType.Underbarrel);

        if (currentUnderbarrelEntry == null)
        {
            Debug.LogError("No current underbarrel attachment found!");
            return;
        }

        AttachmentData currentUnderbarrelAttachment = null;
        if (attachmentLookup.TryGetValue(currentUnderbarrelEntry.attachmentId, out var att))
        {
            currentUnderbarrelAttachment = att;
        }

        if (currentUnderbarrelAttachment == null)
        {
            Debug.LogError($"Could not find current underbarrel attachment in lookup: {currentUnderbarrelEntry.attachmentId}");
            return;
        }

        Debug.Log($"Current underbarrel: {currentUnderbarrelAttachment.name}, New underbarrel: {newUnderbarrelAttachment.name}");

        // Get the underbarrel socket
        Transform socket = GetSocketForAttachmentType(AttachmentType.Underbarrel);

        if (socket == null)
        {
            Debug.LogError($"No socket found for Underbarrel type!");
            return;
        }

        // Disable finalize button and attachment buttons while minigame is active
        if (finalizeButton != null)
            finalizeButton.interactable = false;

        DisableAllAttachmentButtons();

        // Start the underbarrel replacement minigame
        minigameManager.StartUnderbarrelReplacementMinigame(
            currentUnderbarrelAttachment,
            newUnderbarrelAttachment,
            selectedBase,
            socket,
            (replacedAttachment) =>
            {
                Debug.Log("Underbarrel replacement minigame complete!");

                // Remove the old underbarrel attachment from preview
                previewInstance.attachments.RemoveAll(e => e.type == AttachmentType.Underbarrel);
                previewRuntime.attachmentSystem.UnequipType(AttachmentType.Underbarrel);

                // CRITICAL: Check if the old underbarrel was an ORIGINAL attachment
                bool wasOriginal = originalAttachmentIds.Contains(currentUnderbarrelEntry.attachmentId);

                if (wasOriginal)
                {
                    // This was on the weapon when we opened the builder - return it to inventory
                    Debug.Log($"Old underbarrel {currentUnderbarrelAttachment.name} was ORIGINAL - returning to inventory");

                    if (playerInventory.PrimaryInventorySystem.AddToInventory(currentUnderbarrelAttachment, 1))
                    {
                        Debug.Log($"Successfully returned {currentUnderbarrelAttachment.name} to inventory");
                        originalAttachmentIds.Remove(currentUnderbarrelEntry.attachmentId);
                    }
                    else
                    {
                        Debug.LogError($"Failed to return {currentUnderbarrelAttachment.name} to inventory - inventory full?");
                    }
                }
                else
                {
                    Debug.Log($"Old underbarrel {currentUnderbarrelAttachment.name} was newly added - already in inventory");
                }


                // Add the new underbarrel attachment
                AddAttachmentDirectly(replacedAttachment);

                // Re-enable finalize button and attachment buttons
                if (finalizeButton != null)
                    finalizeButton.interactable = true;

                // Note: AddAttachmentDirectly already calls PopulateAttachmentButtons(), so we don't need to call it again
            });
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

        Transform socket = GetSocketForAttachmentType(att.type);

        if (socket == null)
        {
            Debug.LogError($"No socket found for {att.type}! Adding directly.");
            AddAttachmentDirectly(att);
            return;
        }

        if (finalizeButton != null)
            finalizeButton.interactable = false;

        DisableAllAttachmentButtons();

        minigameManager.StartMinigame(att, selectedBase, socket, (completedAttachment) =>
        {
            Debug.Log("Minigame completion callback triggered!");
            AddAttachmentDirectly(completedAttachment);

            if (finalizeButton != null)
                finalizeButton.interactable = true;

            PopulateAttachmentButtons();
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
        PopulateAttachmentButtons();
    }

    public void RemoveAttachmentFromPreview(AttachmentType type)
    {
        if (previewRuntime == null) return;

        var attachmentEntry = previewInstance.attachments.Find(e => e.type == type);

        if (attachmentEntry == null)
        {
            Debug.LogWarning($"No attachment of type {type} found to remove!");
            return;
        }

        // Get the attachment data
        if (!attachmentLookup.TryGetValue(attachmentEntry.attachmentId, out var attachmentData))
        {
            Debug.LogError($"Could not find attachment data for ID: {attachmentEntry.attachmentId}");
            return;
        }

        // Check if this attachment type requires a removal minigame
        bool requiresRemovalMinigame = RequiresMinigame(attachmentData);

        if (requiresRemovalMinigame)
        {
            Debug.Log($"Starting removal minigame for {attachmentData.Name}");
            StartRemovalMinigame(attachmentData, attachmentEntry);
        }
        else
        {
            Debug.Log($"Removing {attachmentData.Name} directly (no minigame)");
            RemoveAttachmentDirectly(type, attachmentEntry);
        }
    }

    /// <summary>
    /// Start the removal minigame for an attachment
    /// </summary>
    void StartRemovalMinigame(AttachmentData att, WeaponAttachmentEntry entry)
    {
        Debug.Log($"StartRemovalMinigame called for {att.Name} (Type: {att.type})");

        if (minigameManager == null)
        {
            Debug.LogError("MinigameManager not assigned! Removing attachment directly.");
            RemoveAttachmentDirectly(att.type, entry);
            return;
        }

        Transform socket = GetSocketForAttachmentType(att.type);

        if (socket == null)
        {
            Debug.LogError($"No socket found for {att.type}! Removing directly.");
            RemoveAttachmentDirectly(att.type, entry);
            return;
        }

        // Disable finalize button and attachment buttons while minigame is active
        if (finalizeButton != null)
            finalizeButton.interactable = false;

        DisableAllAttachmentButtons();

        // Call the appropriate removal minigame based on attachment type
        switch (att.type)
        {
            case AttachmentType.Barrel:
                Debug.Log("Starting barrel removal minigame");
                minigameManager.StartBarrelRemovalMinigame(
                    att,
                    selectedBase,
                    socket,
                    (removedAttachment) => OnRemovalMinigameComplete(att.type, entry)
                );
                break;

            case AttachmentType.Sight:
                Debug.Log("Starting scope removal minigame");
                minigameManager.StartScopeRemovalMinigame(
                    att,
                    selectedBase,
                    socket,
                    (removedAttachment) => OnRemovalMinigameComplete(att.type, entry)
                );
                break;

            case AttachmentType.Underbarrel:
                Debug.Log("Starting underbarrel removal minigame");
                minigameManager.StartUnderbarrelRemovalMinigame(
                    att,
                    selectedBase,
                    socket,
                    (removedAttachment) => OnRemovalMinigameComplete(att.type, entry)
                );
                break;

            case AttachmentType.SideRail:
                Debug.Log("Starting siderail removal minigame");
                minigameManager.StartSiderailRemovalMinigame(
                    att,
                    selectedBase,
                    socket,
                    (removedAttachment) => OnRemovalMinigameComplete(att.type, entry)
                );
                break;

            case AttachmentType.Magazine:
                Debug.Log("Starting magazine removal minigame");
                minigameManager.StartMagazineRemovalMinigame(
                    att,
                    selectedBase,
                    socket,
                    (removedAttachment) => OnRemovalMinigameComplete(att.type, entry)
                );
                break;

            default:
                Debug.LogWarning($"No removal minigame for attachment type: {att.type}");
                RemoveAttachmentDirectly(att.type, entry);
                break;
        }
    }

    /// <summary>
    /// Called when removal minigame completes successfully
    /// </summary>
    void OnRemovalMinigameComplete(AttachmentType type, WeaponAttachmentEntry entry)
    {
        Debug.Log($"Removal minigame complete for {type}!");

        // The minigame has already destroyed the attachment GameObject
        // and re-enabled default weapon parts (if applicable)

        // Now we just need to update our data structures and UI
        RemoveAttachmentDirectly(type, entry);

        // Re-enable finalize button and attachment buttons
        if (finalizeButton != null)
            finalizeButton.interactable = true;

        PopulateAttachmentButtons();
    }

    /// <summary>
    /// Remove attachment directly without minigame (or after minigame completes)
    /// </summary>
    void RemoveAttachmentDirectly(AttachmentType type, WeaponAttachmentEntry entry)
    {
        bool wasOriginal = originalAttachmentIds.Contains(entry.attachmentId);

        if (wasOriginal)
        {
            if (attachmentLookup.TryGetValue(entry.attachmentId, out var att))
            {
                Debug.Log($"Removing PRE-EXISTING attachment: {att.name} - adding back to inventory");

                if (playerInventory.PrimaryInventorySystem.AddToInventory(att, 1))
                {
                    Debug.Log($"Successfully added {att.name} back to inventory");
                    originalAttachmentIds.Remove(entry.attachmentId);
                }
                else
                {
                    Debug.LogError($"Failed to add {att.name} back to inventory - inventory full?");
                }
            }
        }
        else
        {
            Debug.Log($"Removing NEWLY ADDED attachment: {entry.attachmentId} - already in inventory");
        }

        // Remove from preview instance
        previewInstance.attachments.RemoveAll(e => e.type == type);

        // Unequip from runtime system
        previewRuntime.attachmentSystem.UnequipType(type);

        // Update UI
        UpdateSelectedAttachmentsUI();
        PopulateAttachmentButtons();
    }


    /// <summary>
    /// Re-enable the default barrel parts when a barrel attachment is removed
    /// </summary>
    private void ReEnableDefaultBarrelParts()
    {
        if (previewRuntime == null || selectedBase == null) return;

        var partsToReEnable = selectedBase.GetPartsToDisableWithBarrel();

        if (partsToReEnable == null || partsToReEnable.Count == 0)
        {
            Debug.Log("No barrel parts to re-enable");
            return;
        }

        foreach (var partPath in partsToReEnable)
        {
            if (string.IsNullOrEmpty(partPath))
                continue;

            Transform partTransform = previewRuntime.transform.Find(partPath);

            if (partTransform == null)
            {
                partTransform = FindChildByNameRecursive(previewRuntime.transform, partPath);
            }

            if (partTransform != null)
            {
                partTransform.gameObject.SetActive(true);
                Debug.Log($"Re-enabled default barrel part: {partPath}");
            }
            else
            {
                Debug.LogWarning($"Could not find barrel part to re-enable: {partPath}");
            }
        }
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
        }
    }

    public void FinalizeWeapon()
    {
        if (minigameManager != null && minigameManager.IsMinigameActive())
        {
            Debug.LogWarning("Cannot finalize while minigame is active!");
            return;
        }

        if (previewInstance == null || previewRuntime == null || selectedBase == null)
        {
            Debug.LogError("Cannot finalize: missing preview or selected weapon");
            return;
        }

        InventorySlot weaponSlot = FindInventorySlotForWeapon(selectedBase);

        if (weaponSlot == null)
        {
            Debug.LogError("Cannot find weapon in inventory!");
            return;
        }

        if (!playerInventory.PrimaryInventorySystem.ContainsItem(selectedBase, 1))
        {
            Debug.LogError("Selected weapon no longer in inventory!");
            RefreshAvailableItems();
            return;
        }

        Debug.Log("=== FINALIZE WEAPON - ATTACHMENT VERIFICATION ===");

        // Verify attachments - but we need to be smart about this
        // For newly added attachments (not original), they should be in inventory
        foreach (var entry in previewInstance.attachments)
        {
            if (attachmentLookup.TryGetValue(entry.attachmentId, out var att))
            {
                bool isOriginal = originalAttachmentIds.Contains(entry.attachmentId);
                Debug.Log($"Checking attachment: {att.name} - IsOriginal: {isOriginal}");

                if (!isOriginal)
                {
                    // This is a newly added attachment - verify it's in inventory
                    if (!playerInventory.PrimaryInventorySystem.ContainsItem(att, 1))
                    {
                        Debug.LogError($"Attachment {att.name} no longer in inventory!");
                        RefreshAvailableItems();
                        return;
                    }
                }
            }
        }

        // Remove weapon from inventory
        if (!playerInventory.PrimaryInventorySystem.RemoveFromInventory(selectedBase, 1))
        {
            Debug.LogError("Failed to remove weapon from inventory!");
            return;
        }

        WeaponInstanceStorage.RemoveInstance(weaponSlot.UniqueSlotID);

        Debug.Log("=== FINALIZE WEAPON - REMOVING ATTACHMENTS FROM INVENTORY ===");

        // Remove newly added attachments from inventory
        foreach (var entry in previewInstance.attachments)
        {
            bool isOriginal = originalAttachmentIds.Contains(entry.attachmentId);

            if (!isOriginal)
            {
                if (attachmentLookup.TryGetValue(entry.attachmentId, out var att))
                {
                    Debug.Log($"Removing newly added attachment from inventory: {att.name}");
                    if (!playerInventory.PrimaryInventorySystem.RemoveFromInventory(att, 1))
                    {
                        Debug.LogWarning($"Failed to remove attachment {att.name} from inventory!");
                    }
                    else
                    {
                        Debug.Log($"Successfully removed {att.name} from inventory");
                    }
                }
            }
            else
            {
                Debug.Log($"Skipping original attachment: {entry.attachmentId}");
            }
        }

        // Create finalized weapon
        Vector3 spawnPos = previewRuntime.transform.position;
        Quaternion spawnRot = previewRuntime.transform.rotation;

        GameObject pickupWeapon = Instantiate(selectedBase.weaponPrefab, spawnPos, spawnRot);

        SphereCollider col = pickupWeapon.GetComponent<SphereCollider>();
        if (col == null) col = pickupWeapon.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.5f;

        if (!pickupWeapon.TryGetComponent<UniqueID>(out var id))
            pickupWeapon.AddComponent<UniqueID>();

        var pickup = pickupWeapon.GetComponent<ItemPickup>();
        if (pickup == null) pickup = pickupWeapon.AddComponent<ItemPickup>();
        pickup.ItemData = selectedBase;
        pickup.pickUpRadius = col.radius;

        var runtime = pickupWeapon.GetComponent<WeaponRuntime>();
        if (runtime == null) runtime = pickupWeapon.AddComponent<WeaponRuntime>();

        var attachSys = pickupWeapon.GetComponent<WeaponAttachmentSystem>();
        if (attachSys == null) attachSys = pickupWeapon.AddComponent<WeaponAttachmentSystem>();
        attachSys.weaponData = selectedBase;
        runtime.attachmentSystem = attachSys;

        // Apply attachments
        foreach (var entry in previewInstance.attachments)
        {
            if (!attachmentLookup.TryGetValue(entry.attachmentId, out var att))
                continue;

            attachSys.EquipAttachment(att, entry);
        }

        runtime.InitFromInstance(previewInstance, selectedBase, attachmentLookup);

        HandleIronSightVisibility(pickupWeapon, selectedBase, previewInstance, attachmentLookup);
        HandleMagazineVisibility(pickupWeapon, selectedBase, previewInstance, attachmentLookup);

        var instanceHolder = pickupWeapon.AddComponent<WeaponInstanceHolder>();
        instanceHolder.weaponInstance = previewInstance;

        PlayerInventoryHolder.OnPlayerInventoryChanged?.Invoke();

        Debug.Log($"FINALIZED: {selectedBase.name} with {previewInstance.attachments.Count} attachments");

        // Find and properly close the builder
        WeaponBuilderController controller = FindObjectOfType<WeaponBuilderController>();
        if (controller != null)
        {
            Debug.Log("Closing builder via WeaponBuilderController (this will handle gameplay mode)");
            controller.CloseBuilder();
        }
        else
        {
            Debug.LogWarning("WeaponBuilderController not found! Closing builder manually and restoring gameplay mode");

            // Fallback: manually close and restore gameplay mode
            if (previewContainer != null)
                previewContainer.SetActive(false);

            gameObject.SetActive(false);

            // NEW: Manually restore gameplay mode as fallback
            PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.EnableGameplayMode();
                Debug.Log("Manually restored gameplay mode");
            }
            else
            {
                Debug.LogError("Could not find PlayerMovement to restore gameplay mode!");
                // Ultimate fallback
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    void StartScopeReplacementMinigame(AttachmentData newScopeAttachment)
    {
        Debug.Log($"StartScopeReplacementMinigame called for {newScopeAttachment.name}");

        if (minigameManager == null)
        {
            Debug.LogError("MinigameManager not assigned!");
            return;
        }

        var currentScopeEntry = previewInstance.attachments.Find(e => e.type == AttachmentType.Sight);

        if (currentScopeEntry == null)
        {
            Debug.LogError("No current scope attachment found!");
            return;
        }

        AttachmentData currentScopeAttachment = null;
        if (attachmentLookup.TryGetValue(currentScopeEntry.attachmentId, out var att))
        {
            currentScopeAttachment = att;
        }

        if (currentScopeAttachment == null)
        {
            Debug.LogError($"Could not find current scope attachment in lookup: {currentScopeEntry.attachmentId}");
            return;
        }

        Debug.Log($"Current scope: {currentScopeAttachment.name}, New scope: {newScopeAttachment.name}");

        Transform socket = GetSocketForAttachmentType(AttachmentType.Sight);

        if (socket == null)
        {
            Debug.LogError($"No socket found for Sight type!");
            return;
        }

        if (finalizeButton != null)
            finalizeButton.interactable = false;

        DisableAllAttachmentButtons();

        minigameManager.StartScopeReplacementMinigame(
            currentScopeAttachment,
            newScopeAttachment,
            selectedBase,
            socket,
            (replacedAttachment) =>
            {
                Debug.Log("Scope replacement minigame complete!");

                previewInstance.attachments.RemoveAll(e => e.type == AttachmentType.Sight);
                previewRuntime.attachmentSystem.UnequipType(AttachmentType.Sight);

                // CRITICAL: Check if the old scope was an ORIGINAL attachment
                bool wasOriginal = originalAttachmentIds.Contains(currentScopeEntry.attachmentId);

                if (wasOriginal)
                {
                    // This was on the weapon when we opened the builder - return it to inventory
                    Debug.Log($"Old scope {currentScopeAttachment.name} was ORIGINAL - returning to inventory");

                    if (playerInventory.PrimaryInventorySystem.AddToInventory(currentScopeAttachment, 1))
                    {
                        Debug.Log($"Successfully returned {currentScopeAttachment.name} to inventory");
                        originalAttachmentIds.Remove(currentScopeEntry.attachmentId);
                    }
                    else
                    {
                        Debug.LogError($"Failed to return {currentScopeAttachment.name} to inventory - inventory full?");
                    }
                }
                else
                {
                    Debug.Log($"Old scope {currentScopeAttachment.name} was newly added - already in inventory");
                }


                AddAttachmentDirectly(replacedAttachment);

                if (finalizeButton != null)
                    finalizeButton.interactable = true;
            });
    }

    public static void HandleMagazineVisibility(GameObject weaponObject, WeaponData weaponData, WeaponInstance weaponInstance, Dictionary<string, AttachmentData> attachmentLookup)
    {
        if (weaponObject == null || weaponData == null || weaponInstance == null) return;

        bool hasMagazineAttachment = false;
        foreach (var entry in weaponInstance.attachments)
        {
            if (attachmentLookup.TryGetValue(entry.attachmentId, out var att))
            {
                if (att.type == AttachmentType.Magazine)
                {
                    hasMagazineAttachment = true;
                    break;
                }
            }
        }

        var partsToToggle = weaponData.GetPartsToDisableWithMagazine();

        if (partsToToggle == null || partsToToggle.Count == 0)
            return;

        foreach (var partPath in partsToToggle)
        {
            if (string.IsNullOrEmpty(partPath))
                continue;

            Transform partTransform = weaponObject.transform.Find(partPath);

            if (partTransform == null)
            {
                partTransform = FindChildByNameRecursive(weaponObject.transform, partPath);
            }

            if (partTransform != null)
            {
                partTransform.gameObject.SetActive(!hasMagazineAttachment);
            }
        }
    }

    private static Transform FindChildByNameRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }

            Transform result = FindChildByNameRecursive(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

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

    public static void HandleIronSightVisibility(GameObject weaponObject, WeaponData weaponData, WeaponInstance weaponInstance, Dictionary<string, AttachmentData> attachmentLookup)
    {
        if (weaponObject == null || weaponData == null || weaponInstance == null) return;

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

        var partsToToggle = weaponData.GetPartsToDisableWithSight();

        if (partsToToggle == null || partsToToggle.Count == 0)
            return;

        foreach (var partPath in partsToToggle)
        {
            if (string.IsNullOrEmpty(partPath))
                continue;

            Transform partTransform = weaponObject.transform.Find(partPath);

            if (partTransform != null)
            {
                partTransform.gameObject.SetActive(!hasSightAttachment);
            }
        }
    }

    void OnDisable()
    {
        Debug.Log("=== BUILDER CLOSING ===");
        Debug.Log($"Attachments in preview: {previewInstance?.attachments.Count ?? 0}");
        if (previewInstance != null)
        {
            foreach (var att in previewInstance.attachments)
            {
                if (attachmentLookup.TryGetValue(att.attachmentId, out var attData))
                {
                    Debug.Log($"  - {attData.name} (ID: {att.attachmentId}, Type: {att.type})");
                }
            }
        }

        Debug.Log($"Original attachments: {originalAttachmentIds.Count}");
        foreach (var id in originalAttachmentIds)
        {
            if (attachmentLookup.TryGetValue(id, out var attData))
            {
                Debug.Log($"  - Original: {attData.name} (ID: {id})");
            }
        }

        // Cancel any active minigame when UI is disabled
        if (minigameManager != null && minigameManager.IsMinigameActive())
        {
            Debug.Log("WeaponBuilderUI disabled - cancelling active minigame");
            minigameManager.CancelCurrentMinigame();
        }
    }
}