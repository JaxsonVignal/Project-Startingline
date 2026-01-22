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

    [Header("Weapon Storage")]
    public Transform weaponHolder;

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
    // DELIVERY LOGIC
    // --------------------------------------------------

    private void DeliverWeapon()
    {
        WeaponAttachmentSystem weapon = FindMatchingWeapon();

        if (weapon == null)
        {
            promptText.text =
                "Delivery failed.\n\nYou don't have the required weapon.";
            Invoke(nameof(ClosePrompt), 2f);
            return;
        }

        RemoveWeaponFromInventory(weapon);
        AddMoney(currentOrder.agreedPrice);
        TextingManager.Instance.CompleteWeaponDelivery(nearbyNPC.npcName);

        ClosePrompt();
    }

    private WeaponAttachmentSystem FindMatchingWeapon()
    {
        WeaponAttachmentSystem[] weapons = GetAllWeaponsInInventory();

        foreach (var weapon in weapons)
        {
            if (weapon == null || weapon.weaponData == null)
                continue;

            // Match weapon by ID
            if (weapon.weaponData.weaponId != currentOrder.weaponRequested.weaponId)
                continue;

            // Match attachments
            if (HasRequiredAttachments(weapon))
                return weapon;
        }

        return null;
    }



    // --------------------------------------------------
    // ATTACHMENT VALIDATION (FIX 1)
    // --------------------------------------------------

    private bool HasRequiredAttachments(WeaponAttachmentSystem weapon)
    {
        return
            MatchAttachment(weapon, currentOrder.sightAttachment) &&
            MatchAttachment(weapon, currentOrder.underbarrelAttachment) &&
            MatchAttachment(weapon, currentOrder.barrelAttachment) &&
            MatchAttachment(weapon, currentOrder.magazineAttachment) &&
            MatchAttachment(weapon, currentOrder.sideRailAttachment);
    }

    private bool MatchAttachment(
        WeaponAttachmentSystem weapon,
        AttachmentData required
    )
    {
        // Slot not required → allow anything
        if (required == null)
            return true;

        // Match by ATTACHMENT ID
        return weapon.HasAttachment(required.id);
    }

    // --------------------------------------------------
    // INVENTORY
    // --------------------------------------------------

    private WeaponAttachmentSystem[] GetAllWeaponsInInventory()
    {
        if (weaponHolder != null)
            return weaponHolder.GetComponentsInChildren<WeaponAttachmentSystem>(true);

        return GetComponentsInChildren<WeaponAttachmentSystem>(true);
    }



    private void RemoveWeaponFromInventory(WeaponAttachmentSystem weapon)
    {
        PlayerInventoryHolder inventoryHolder = GetComponent<PlayerInventoryHolder>();
        if (inventoryHolder == null)
        {
            Debug.LogError("PlayerInventoryHolder not found!");
            return;
        }

        var inventory = inventoryHolder.PrimaryInventorySystem;

        var slot = inventory.InventorySlots.FirstOrDefault(s =>
            s.ItemData is WeaponData wd &&
            wd.weaponId == weapon.weaponData.weaponId);

        if (slot == null)
        {
            Debug.LogWarning("Weapon not found in inventory!");
        }
        else
        {
            if (slot.StackSize > 1)
                slot.RemoveFromStack(1);
            else
                slot.ClearSlot();

            inventory.OnInventorySlotChanged?.Invoke(slot);
        }

        Destroy(weapon.gameObject);
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
