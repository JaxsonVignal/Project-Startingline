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

    [Header("Asset Data")]
    public List<WeaponData> allWeaponTemplates;
    public List<AttachmentData> allAttachments;

    private WeaponData selectedBase;
    private WeaponInstance previewInstance;
    private WeaponRuntime previewRuntime;
    private Dictionary<string, AttachmentData> attachmentLookup = new Dictionary<string, AttachmentData>();

    void Start()
    {
        // Ensure preview container is OFF by default
        if (previewContainer != null)
            previewContainer.SetActive(false);

        // Build attachment lookup
        foreach (var att in allAttachments)
            if (att != null && !string.IsNullOrEmpty(att.id))
                attachmentLookup[att.id] = att;

        // Populate TMP Dropdown
        weaponDropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (var w in allWeaponTemplates) options.Add(w.name);
        weaponDropdown.AddOptions(options);
        weaponDropdown.onValueChanged.AddListener(OnWeaponSelected);

        finalizeButton.onClick.AddListener(FinalizeWeapon);

        PopulateAttachmentButtons();

        if (allWeaponTemplates.Count > 0)
            OnWeaponSelected(0);
    }

    void OnWeaponSelected(int index)
    {
        if (index < 0 || index >= allWeaponTemplates.Count) return;
        selectedBase = allWeaponTemplates[index];
        StartPreview();
    }

    void StartPreview()
    {
        // Destroy old preview
        if (previewRuntime != null) Destroy(previewRuntime.gameObject);

        // Create new instance for runtime
        previewInstance = new WeaponInstance { weaponId = selectedBase.weaponId, displayName = selectedBase.name };

        GameObject go = Instantiate(selectedBase.weaponPrefab, previewContainer.transform);
        previewRuntime = go.AddComponent<WeaponRuntime>();
        var attachSys = go.AddComponent<WeaponAttachmentSystem>();
        attachSys.weaponData = selectedBase;
        previewRuntime.attachmentSystem = attachSys;
        previewRuntime.InitFromInstance(previewInstance, selectedBase, attachmentLookup);

        // DO NOT enable previewContainer here! Let WeaponBuilderController handle visibility

        UpdateSelectedAttachmentsUI();
    }

    void PopulateAttachmentButtons()
    {
        foreach (Transform child in attachmentListPanel) Destroy(child.gameObject);

        foreach (var att in allAttachments)
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

        // Remove any existing attachment of the same type
        previewInstance.attachments.RemoveAll(e => e.type == att.type);

        // Create a new WeaponAttachmentEntry
        var entry = new WeaponAttachmentEntry(att.id, att.type, att.localPosition, att.localEuler, att.localScale);

        // Add to preview instance
        previewInstance.attachments.Add(entry);

        // Equip attachment visually using both arguments
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
        if (previewInstance == null) return;

        // Spawn in front of player
        Vector3 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 2f;
        Quaternion spawnRot = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);

        GameObject pickupWeapon = Instantiate(selectedBase.weaponPrefab, spawnPos, spawnRot);

        // Ensure SphereCollider exists
        SphereCollider col = pickupWeapon.GetComponent<SphereCollider>();
        if (col == null) col = pickupWeapon.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.5f;

        // Ensure UniqueID exists
        if (!pickupWeapon.TryGetComponent<UniqueID>(out var id))
            pickupWeapon.AddComponent<UniqueID>();

        // Add ItemPickup component
        var pickup = pickupWeapon.GetComponent<ItemPickup>();
        if (pickup == null) pickup = pickupWeapon.AddComponent<ItemPickup>();
        pickup.ItemData = selectedBase;
        pickup.pickUpRadius = col.radius;

        // Ensure WeaponRuntime and AttachmentSystem exist
        var runtime = pickupWeapon.GetComponent<WeaponRuntime>();
        if (runtime == null) runtime = pickupWeapon.AddComponent<WeaponRuntime>();

        var attachSys = pickupWeapon.GetComponent<WeaponAttachmentSystem>();
        if (attachSys == null) attachSys = pickupWeapon.AddComponent<WeaponAttachmentSystem>();
        attachSys.weaponData = selectedBase;
        runtime.attachmentSystem = attachSys;

        // Apply attachments from previewInstance
        foreach (var entry in previewInstance.attachments)
        {
            if (!attachmentLookup.TryGetValue(entry.attachmentId, out var att)) continue;
            attachSys.EquipAttachment(att, entry);
        }

        runtime.InitFromInstance(previewInstance, selectedBase, attachmentLookup);

        // NEW: Store the weapon instance data on the pickup object
        var instanceHolder = pickupWeapon.AddComponent<WeaponInstanceHolder>();
        instanceHolder.weaponInstance = previewInstance;

        // Hide builder and preview
        if (previewContainer != null)
            previewContainer.SetActive(false);
        gameObject.SetActive(false);
    }
}