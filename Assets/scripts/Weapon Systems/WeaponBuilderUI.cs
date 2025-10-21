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

    [Header("Asset Data")]
    public List<AttachmentData> allAttachments; // Keep for lookup reference

    private WeaponData selectedBase;
    private InventorySlot selectedWeaponSlot;
    private WeaponInstance previewInstance;
    private WeaponRuntime previewRuntime;
    private Dictionary<string, AttachmentData> attachmentLookup = new Dictionary<string, AttachmentData>();
    private List<WeaponData> availableWeapons = new List<WeaponData>();
    private List<AttachmentData> availableAttachments = new List<AttachmentData>();

    void Start()
    {
        if (previewContainer != null)
            previewContainer.SetActive(false);

        // Build attachment lookup
        foreach (var att in allAttachments)
            if (att != null && !string.IsNullOrEmpty(att.id))
                attachmentLookup[att.id] = att;

        finalizeButton.onClick.AddListener(FinalizeWeapon);

        RefreshAvailableItems();
    }

    /// <summary>
    /// Scan player inventory for weapons and attachments
    /// </summary>
    public void RefreshAvailableItems()
    {
        availableWeapons.Clear();
        availableAttachments.Clear();

        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventory not assigned!");
            return;
        }

        // Get all inventory slots
        var slots = playerInventory.PrimaryInventorySystem.GetAllInventorySlots();

        foreach (var slot in slots)
        {
            if (slot.ItemData == null) continue;

            // Check if it's a weapon
            if (slot.ItemData is WeaponData weaponData)
            {
                availableWeapons.Add(weaponData);
            }
            // Check if it's an attachment
            else if (slot.ItemData is AttachmentData attachmentData)
            {
                availableAttachments.Add(attachmentData);
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
            return;
        }

        weaponDropdown.interactable = true;
        List<string> options = new List<string>();
        foreach (var w in availableWeapons)
        {
            options.Add(w.name);
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
        selectedWeaponSlot = FindInventorySlotForWeapon(selectedBase);

        StartPreview();
    }

    InventorySlot FindInventorySlotForWeapon(WeaponData weaponData)
    {
        var slots = playerInventory.PrimaryInventorySystem.GetAllInventorySlots();
        foreach (var slot in slots)
        {
            if (slot.ItemData == weaponData)
                return slot;
        }
        return null;
    }

    void StartPreview()
    {
        if (previewRuntime != null) Destroy(previewRuntime.gameObject);

        previewInstance = new WeaponInstance
        {
            weaponId = selectedBase.weaponId,
            displayName = selectedBase.name
        };

        GameObject go = Instantiate(selectedBase.weaponPrefab, previewContainer.transform);
        previewRuntime = go.AddComponent<WeaponRuntime>();
        var attachSys = go.AddComponent<WeaponAttachmentSystem>();
        attachSys.weaponData = selectedBase;
        previewRuntime.attachmentSystem = attachSys;
        previewRuntime.InitFromInstance(previewInstance, selectedBase, attachmentLookup);

        UpdateSelectedAttachmentsUI();
    }

    void PopulateAttachmentButtons()
    {
        foreach (Transform child in attachmentListPanel) Destroy(child.gameObject);

        if (availableAttachments.Count == 0)
        {
            // Optionally add a "No attachments available" text
            return;
        }

        foreach (var att in availableAttachments)
        {
            var btnGO = Instantiate(attachmentButtonPrefab, attachmentListPanel);
            var btn = btnGO.GetComponent<Button>();
            var img = btnGO.GetComponentInChildren<Image>();
            if (img != null) img.sprite = att.icon;

            btn.onClick.AddListener(() => AddAttachmentToPreview(att));
        }
    }

    public void AddAttachmentToPreview(AttachmentData att)
    {
        if (att == null || previewRuntime == null) return;

        // Check if attachment type is already equipped
        bool alreadyEquipped = previewInstance.attachments.Exists(e => e.type == att.type);

        if (alreadyEquipped)
        {
            Debug.Log($"Already have a {att.type} equipped. Remove it first.");
            return;
        }

        // Remove any existing attachment of the same type (safety check)
        previewInstance.attachments.RemoveAll(e => e.type == att.type);

        var entry = new WeaponAttachmentEntry(att.id, att.type, att.localPosition, att.localEuler, att.localScale);
        previewInstance.attachments.Add(entry);
        previewRuntime.attachmentSystem.EquipAttachment(att, entry);

        UpdateSelectedAttachmentsUI();
    }

    public void RemoveAttachmentFromPreview(AttachmentType type)
    {
        if (previewRuntime == null) return;

        previewInstance.attachments.RemoveAll(e => e.type == type);
        previewRuntime.attachmentSystem.UnequipType(type);

        UpdateSelectedAttachmentsUI();
    }

    void UpdateSelectedAttachmentsUI()
    {
        foreach (Transform t in selectedAttachmentsPanel) Destroy(t.gameObject);

        foreach (var entry in previewInstance.attachments)
        {
            if (!attachmentLookup.TryGetValue(entry.attachmentId, out var att)) continue;

            var btnGO = Instantiate(attachmentButtonPrefab, selectedAttachmentsPanel);
            var img = btnGO.GetComponentInChildren<Image>();
            if (img != null) img.sprite = att.icon;

            var btn = btnGO.GetComponent<Button>();
            btn.onClick.AddListener(() => RemoveAttachmentFromPreview(entry.type));
        }
    }

    public void FinalizeWeapon()
    {
        if (previewInstance == null || previewRuntime == null || selectedWeaponSlot == null)
        {
            Debug.LogError("Cannot finalize: missing preview or selected weapon slot");
            return;
        }

        // Remove base weapon from inventory
        playerInventory.PrimaryInventorySystem.RemoveFromInventory(selectedBase, 1);

        // Remove used attachments from inventory
        foreach (var entry in previewInstance.attachments)
        {
            if (attachmentLookup.TryGetValue(entry.attachmentId, out var att))
            {
                playerInventory.PrimaryInventorySystem.RemoveFromInventory(att, 1);
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

        // Apply attachments
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

        // Hide builder
        if (previewContainer != null)
            previewContainer.SetActive(false);
        gameObject.SetActive(false);
    }
}