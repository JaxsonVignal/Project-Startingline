using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles delivering weapons to NPCs.
/// Attach this to the Player.
/// </summary>
public class WeaponDeliveryInteraction : MonoBehaviour
{
    [Header("UI")]
    public GameObject deliveryPromptUI;
    public TMP_Text promptText;
    public Button deliverButton;
    public Button cancelButton;

    [Header("Interaction")]
    public float interactionRange = 3f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Player Controls")]
    public MonoBehaviour playerMovementScript;
    public MonoBehaviour playerLookScript;

    private NPCManager nearbyNPC;
    private WeaponOrder currentOrder;
    private bool menuOpen;

    private void Start()
    {
        deliveryPromptUI.SetActive(false);
        deliverButton.onClick.AddListener(DeliverWeapon);
        cancelButton.onClick.AddListener(ClosePrompt);
    }

    private void Update()
    {
        if (menuOpen) return;

        FindNearbyNPC();

        if (nearbyNPC != null && currentOrder != null)
        {
            if (Input.GetKeyDown(interactKey))
                OpenPrompt();
        }
    }

    // --------------------------------------------------
    // NPC & ORDER DETECTION
    // --------------------------------------------------

    private void FindNearbyNPC()
    {
        nearbyNPC = null;
        currentOrder = null;

        foreach (var col in Physics.OverlapSphere(transform.position, interactionRange))
        {
            var npc = col.GetComponent<NPCManager>();
            if (!npc) continue;

            if (!TextingManager.Instance.IsNPCReadyForDelivery(npc.npcName))
                continue;

            currentOrder = TextingManager.Instance.GetAcceptedOrderForNPC(npc.npcName);
            if (currentOrder == null) continue;

            nearbyNPC = npc;
            break;
        }
    }

    // --------------------------------------------------
    // UI
    // --------------------------------------------------

    private void OpenPrompt()
    {
        menuOpen = true;
        deliveryPromptUI.SetActive(true);

        DisableControls();
        UnlockCursor();

        promptText.text =
            $"Deliver to {nearbyNPC.npcName}\n\n" +
            $"Weapon: {currentOrder.weaponRequested.Name}\n" +
            $"Payment: ${currentOrder.agreedPrice:F0}";
    }

    private void ClosePrompt()
    {
        menuOpen = false;
        deliveryPromptUI.SetActive(false);

        EnableControls();
        LockCursor();
    }

    // --------------------------------------------------
    // DELIVERY LOGIC (DATA-DRIVEN)
    // --------------------------------------------------

    private void DeliverWeapon()
    {
        var slot = FindMatchingWeaponSlot();

        if (slot == null)
        {
            promptText.text =
                "Delivery failed.\n\nYou don't have the required weapon.";
            Invoke(nameof(ClosePrompt), 2f);
            return;
        }

        // Remove weapon instance + inventory slot
        WeaponInstanceStorage.RemoveInstance(slot.UniqueSlotID);
        slot.ClearSlot();

        AddMoney(currentOrder.agreedPrice);
        TextingManager.Instance.CompleteWeaponDelivery(nearbyNPC.npcName);

        ClosePrompt();
    }

    private InventorySlot FindMatchingWeaponSlot()
    {
        var inventoryHolder = GetComponent<PlayerInventoryHolder>();
        if (inventoryHolder == null)
            return null;

        var inventory = inventoryHolder.PrimaryInventorySystem;

        foreach (var slot in inventory.InventorySlots)
        {
            if (slot.ItemData is not WeaponData weaponData)
                continue;

            // Match weapon ID
            if (weaponData.weaponId != currentOrder.weaponRequested.weaponId)
                continue;

            // Get weapon instance (attachment data)
            var instance = WeaponInstanceStorage.GetInstance(slot.UniqueSlotID);
            if (instance == null)
                continue;

            // Validate attachments
            if (HasRequiredAttachments(instance))
                return slot;
        }

        return null;
    }

    // --------------------------------------------------
    // ATTACHMENT VALIDATION (DATA-ONLY)
    // --------------------------------------------------

    private bool HasRequiredAttachments(WeaponInstance instance)
    {
        return
            MatchAttachment(instance, currentOrder.sightAttachment) &&
            MatchAttachment(instance, currentOrder.underbarrelAttachment) &&
            MatchAttachment(instance, currentOrder.barrelAttachment) &&
            MatchAttachment(instance, currentOrder.magazineAttachment) &&
            MatchAttachment(instance, currentOrder.sideRailAttachment);
    }

    private bool MatchAttachment(WeaponInstance instance, AttachmentData required)
    {
        if (required == null)
            return true;

        if (instance.attachments == null)
            return false;

        return instance.attachments.Any(e => e != null && e.attachmentId == required.id);
    }




    // --------------------------------------------------
    // PLAYER
    // --------------------------------------------------

    private void AddMoney(float amount)
    {
        GetComponent<PlayerMoneyManager>()?.AddMoney(amount, "Weapon delivery");
    }

    private void DisableControls()
    {
        if (playerMovementScript) playerMovementScript.enabled = false;
        if (playerLookScript) playerLookScript.enabled = false;
    }

    private void EnableControls()
    {
        if (playerMovementScript) playerMovementScript.enabled = true;
        if (playerLookScript) playerLookScript.enabled = true;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // --------------------------------------------------
    // DEBUG
    // --------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
