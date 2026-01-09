using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

/// <summary>
/// Handles delivering weapons to NPCs at meeting locations.
/// Attach to Player.
/// </summary>
public class WeaponDeliveryInteraction : MonoBehaviour
{
    [Header("UI")]
    public GameObject deliveryPromptUI;
    public TMP_Text promptText;
    public Button deliverButton;
    public Button cancelButton;

    [Header("Interaction Hint UI (Optional)")]
    public GameObject interactionHintUI; // e.g., "Press E to deliver"
    public TMP_Text hintText;

    [Header("Interaction Settings")]
    public float interactionRange = 3f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private NPCManager nearbyNPC;
    private WeaponOrder currentOrder;
    private bool isMenuOpen = false;

    // References to player components (assign these in Inspector or find them)
    [Header("Player Control References")]
    public MonoBehaviour playerMovementScript; // Your FPS controller script
    public MonoBehaviour playerLookScript; // Your mouse look script

    [Header("Weapon System")]
    public Transform weaponHolder; // Parent object where weapons are spawned (optional, for optimization)

    private void Start()
    {
        if (deliveryPromptUI != null)
            deliveryPromptUI.SetActive(false);

        if (interactionHintUI != null)
            interactionHintUI.SetActive(false);

        if (deliverButton != null)
            deliverButton.onClick.AddListener(DeliverWeapon);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(ClosePrompt);
    }

    private void Update()
    {
        // Don't check for interactions if menu is open
        if (!isMenuOpen)
        {
            CheckForNearbyNPC();

            // Show hint when near NPC with order
            if (nearbyNPC != null && currentOrder != null)
            {
                ShowInteractionHint();

                if (Input.GetKeyDown(interactKey))
                {
                    ShowDeliveryPrompt();
                }
            }
            else
            {
                HideInteractionHint();
            }
        }
    }

    private void CheckForNearbyNPC()
    {
        nearbyNPC = null;
        currentOrder = null;

        Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRange);

        foreach (Collider col in colliders)
        {
            NPCManager npc = col.GetComponent<NPCManager>();
            if (npc != null)
            {
                if (showDebugLogs)
                    Debug.Log($"Found NPC: {npc.npcName}");

                // Check: Is NPC waiting at meeting?
                bool isReady = TextingManager.Instance.IsNPCReadyForDelivery(npc.npcName);

                if (showDebugLogs)
                    Debug.Log($"NPC {npc.npcName} ready for delivery: {isReady}");

                if (isReady)
                {
                    nearbyNPC = npc;
                    currentOrder = TextingManager.Instance.GetAcceptedOrderForNPC(npc.npcName);

                    if (showDebugLogs)
                    {
                        if (currentOrder != null)
                            Debug.Log($"Found order for {npc.npcName}: {currentOrder.weaponRequested.Name}");
                        else
                            Debug.LogWarning($"NPC {npc.npcName} is ready but no order found!");
                    }

                    break;
                }
            }
        }

        // Debug: Show what we found
        if (showDebugLogs && colliders.Length > 0 && nearbyNPC == null)
        {
            Debug.Log($"Found {colliders.Length} colliders in range, but no valid NPC with order");
        }
    }

    private void ShowInteractionHint()
    {
        if (interactionHintUI != null)
        {
            interactionHintUI.SetActive(true);
            if (hintText != null)
                hintText.text = $"Press [{interactKey}] to deliver to {nearbyNPC.npcName}";
        }
    }

    private void HideInteractionHint()
    {
        if (interactionHintUI != null)
            interactionHintUI.SetActive(false);
    }

    private void ShowDeliveryPrompt()
    {
        if (deliveryPromptUI == null || currentOrder == null)
        {
            Debug.LogError("Cannot show delivery prompt: UI or order is null!");
            return;
        }

        deliveryPromptUI.SetActive(true);
        isMenuOpen = true;

        // Lock cursor and disable player controls
        LockCursor(false);
        DisablePlayerControls();

        if (showDebugLogs)
            Debug.Log($"Showing delivery prompt for {nearbyNPC.npcName}");

        string weaponInfo = $"{currentOrder.weaponRequested.Name}";
        string attachmentInfo = "";

        if (currentOrder.sightAttachment != null)
            attachmentInfo += $"\n• {currentOrder.sightAttachment.Name}";
        if (currentOrder.underbarrelAttachment != null)
            attachmentInfo += $"\n• {currentOrder.underbarrelAttachment.Name}";
        if (currentOrder.barrelAttachment != null)
            attachmentInfo += $"\n• {currentOrder.barrelAttachment.Name}";
        if (currentOrder.magazineAttachment != null)
            attachmentInfo += $"\n• {currentOrder.magazineAttachment.Name}";
        if (currentOrder.sideRailAttachment != null)
            attachmentInfo += $"\n• {currentOrder.sideRailAttachment.Name}";

        if (promptText != null)
        {
            promptText.text =
                $"Deliver to {nearbyNPC.npcName}?\n\n" +
                $"Weapon: {weaponInfo}" +
                attachmentInfo +
                $"\n\nPayment: ${currentOrder.agreedPrice:F0}";
        }

        // Hide the hint when showing full prompt
        HideInteractionHint();
    }

    private void DeliverWeapon()
    {
        if (currentOrder == null || nearbyNPC == null)
        {
            Debug.LogError("Cannot deliver: order or NPC is null!");
            return;
        }

        // Check if player has the weapon with correct attachments equipped
        if (CheckWeaponAndAttachments())
        {
            // Player has correct weapon with attachments - complete the delivery successfully
            RemoveWeaponFromInventory();
            AddMoneyToPlayer(currentOrder.agreedPrice);

            // COMPLETE DELIVERY
            TextingManager.Instance.CompleteWeaponDelivery(nearbyNPC.npcName);

            Debug.Log($"✓ Delivered weapon to {nearbyNPC.npcName} for ${currentOrder.agreedPrice}");

            ClosePrompt();
        }
        else
        {
            // Player doesn't have correct weapon/attachments - fail the order, no payment
            Debug.LogWarning($"✗ Order FAILED: Player doesn't have the weapon with required attachments for {nearbyNPC.npcName}!");

            // Close/fail the order without payment
            TextingManager.Instance.CompleteWeaponDelivery(nearbyNPC.npcName);

            // Show error message to player
            if (promptText != null)
            {
                promptText.text = "Delivery FAILED!\n\nYou don't have the correct weapon with the required attachments equipped.\n\nOrder cancelled - no payment received.";
            }

            // Close prompt after a brief delay so player can see the message
            Invoke(nameof(ClosePrompt), 2.5f);
        }
    }

    private bool CheckWeaponAndAttachments()
    {
        // Get player inventory
        PlayerInventoryHolder playerInventory = GetComponent<PlayerInventoryHolder>();
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventoryHolder not found!");
            return false;
        }

        var inventory = playerInventory.PrimaryInventorySystem;

        // Check if player has the weapon in inventory
        if (!inventory.ContainsItem(currentOrder.weaponRequested, 1))
        {
            Debug.LogWarning($"Player doesn't have {currentOrder.weaponRequested.Name} in inventory");
            return false;
        }

        if (showDebugLogs)
        {
            Debug.Log($"=== CHECKING INVENTORY FOR WEAPON: {currentOrder.weaponRequested.Name} ===");
            Debug.Log($"=== REQUIRED ATTACHMENTS ===");
            if (currentOrder.sightAttachment != null)
                Debug.Log($"  - Sight: {currentOrder.sightAttachment.Name} (ID: {currentOrder.sightAttachment.id})");
            if (currentOrder.underbarrelAttachment != null)
                Debug.Log($"  - Underbarrel: {currentOrder.underbarrelAttachment.Name} (ID: {currentOrder.underbarrelAttachment.id})");
            if (currentOrder.barrelAttachment != null)
                Debug.Log($"  - Barrel: {currentOrder.barrelAttachment.Name} (ID: {currentOrder.barrelAttachment.id})");
            if (currentOrder.magazineAttachment != null)
                Debug.Log($"  - Magazine: {currentOrder.magazineAttachment.Name} (ID: {currentOrder.magazineAttachment.id})");
            if (currentOrder.sideRailAttachment != null)
                Debug.Log($"  - SideRail: {currentOrder.sideRailAttachment.Name} (ID: {currentOrder.sideRailAttachment.id})");
        }

        // Get all weapons in inventory (both active and inactive)
        WeaponAttachmentSystem[] allWeapons = GetAllWeaponsInInventory();

        if (showDebugLogs)
        {
            Debug.Log($"Found {allWeapons.Length} total weapons in inventory");

            // Log all weapons found
            foreach (var w in allWeapons)
            {
                if (w.weaponData != null)
                {
                    Debug.Log($"  - Weapon: {w.weaponData.Name}, Attachments: {w.equippedAttachments.Count}");
                    foreach (var att in w.equippedAttachments)
                    {
                        Debug.Log($"    * {att.type}: {att.name} (ID: {att.id})");
                    }
                }
            }
        }

        // Find matching weapon with correct attachments
        foreach (var weapon in allWeapons)
        {
            // Skip if weapon data is null
            if (weapon.weaponData == null)
                continue;

            // Check if this is the correct weapon type
            if (weapon.weaponData.id == currentOrder.weaponRequested.id)
            {
                if (showDebugLogs)
                {
                    Debug.Log($">>> Found matching weapon: {weapon.weaponData.Name}");
                    Debug.Log($">>> Weapon has {weapon.equippedAttachments.Count} attachments equipped");
                }

                if (CheckAllAttachments(weapon))
                {
                    if (showDebugLogs)
                        Debug.Log($"✓✓✓ WEAPON HAS ALL REQUIRED ATTACHMENTS!");
                    return true;
                }
                else
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"✗ Weapon found but missing/wrong required attachments");
                }
            }
        }

        if (showDebugLogs)
            Debug.LogWarning("✗✗✗ NO WEAPON FOUND WITH ALL REQUIRED ATTACHMENTS");
        return false;
    }

    private bool CheckAllAttachments(WeaponAttachmentSystem weapon)
    {
        if (showDebugLogs)
            Debug.Log("--- Checking Required Attachments ---");

        // Check each required attachment
        bool sightOk = CheckAttachment(weapon, AttachmentType.Sight, currentOrder.sightAttachment);
        bool underbarrelOk = CheckAttachment(weapon, AttachmentType.Underbarrel, currentOrder.underbarrelAttachment);
        bool barrelOk = CheckAttachment(weapon, AttachmentType.Barrel, currentOrder.barrelAttachment);
        bool magazineOk = CheckAttachment(weapon, AttachmentType.Magazine, currentOrder.magazineAttachment);
        bool sideRailOk = CheckAttachment(weapon, AttachmentType.SideRail, currentOrder.sideRailAttachment);

        bool allOk = sightOk && underbarrelOk && barrelOk && magazineOk && sideRailOk;

        if (showDebugLogs)
            Debug.Log($"--- Attachment Check Results: {(allOk ? "PASS ✓" : "FAIL ✗")} ---");

        return allOk;
    }

    private WeaponAttachmentSystem[] GetAllWeaponsInInventory()
    {
        if (showDebugLogs)
            Debug.Log(">>> Searching for weapons in inventory...");

        // Search from weaponHolder if assigned (includes inactive weapons)
        if (weaponHolder != null)
        {
            WeaponAttachmentSystem[] weapons = weaponHolder.GetComponentsInChildren<WeaponAttachmentSystem>(true);
            if (showDebugLogs)
                Debug.Log($">>> Found {weapons.Length} weapons in weaponHolder");
            return weapons;
        }

        // Search all child objects of player (includes inactive weapons)
        WeaponAttachmentSystem[] allWeapons = GetComponentsInChildren<WeaponAttachmentSystem>(true);
        if (showDebugLogs)
            Debug.Log($">>> Found {allWeapons.Length} weapons in player children");
        return allWeapons;
    }

    private bool CheckAttachment(WeaponAttachmentSystem weapon, AttachmentType type, AttachmentData requiredAttachment)
    {
        // If no attachment is required for this slot, that's fine
        if (requiredAttachment == null)
        {
            if (showDebugLogs)
                Debug.Log($"  [{type}] Not required - OK ✓");
            return true;
        }

        if (showDebugLogs)
            Debug.Log($"  [{type}] Required: {requiredAttachment.name} (ID: {requiredAttachment.id})");

        // Find the equipped attachment of this type on this weapon
        AttachmentData equippedAttachment = weapon.equippedAttachments
            .FirstOrDefault(att => att.type == type);

        if (equippedAttachment == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"  [{type}] ✗ MISSING - Required '{requiredAttachment.name}' but nothing equipped");
            return false;
        }

        if (showDebugLogs)
            Debug.Log($"  [{type}] Found equipped: {equippedAttachment.name} (ID: {equippedAttachment.id})");

        // Check if it matches the required attachment (compare by ID for exact match)
        if (equippedAttachment.id != requiredAttachment.id)
        {
            if (showDebugLogs)
                Debug.LogWarning($"  [{type}] ✗ WRONG - Expected '{requiredAttachment.name}' but found '{equippedAttachment.name}'");
            return false;
        }

        if (showDebugLogs)
            Debug.Log($"  [{type}] ✓ CORRECT - {requiredAttachment.name}");
        return true;
    }

    private void RemoveWeaponFromInventory()
    {
        PlayerInventoryHolder playerInventory = GetComponent<PlayerInventoryHolder>();
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventoryHolder not found!");
            return;
        }

        var inventory = playerInventory.PrimaryInventorySystem;

        // Find the specific weapon instance that matches the order
        WeaponAttachmentSystem[] allWeapons = GetAllWeaponsInInventory();
        WeaponAttachmentSystem weaponToRemove = null;

        foreach (var weapon in allWeapons)
        {
            if (weapon.weaponData != null && weapon.weaponData.id == currentOrder.weaponRequested.id)
            {
                if (CheckAllAttachments(weapon))
                {
                    weaponToRemove = weapon;
                    break;
                }
            }
        }

        if (weaponToRemove != null)
        {
            // Destroy the weapon GameObject (which includes all attachments)
            Destroy(weaponToRemove.gameObject);

            // Remove from inventory system
            inventory.RemoveFromInventory(currentOrder.weaponRequested, 1);

            if (showDebugLogs)
                Debug.Log($"Removed {currentOrder.weaponRequested.Name} with all attachments from inventory");
        }
        else
        {
            Debug.LogError("Could not find the weapon to remove!");
        }
    }

    private void AddMoneyToPlayer(float amount)
    {
        PlayerMoneyManager money = GetComponent<PlayerMoneyManager>();

        if (money != null)
        {
            money.AddMoney(amount);
            if (showDebugLogs)
                Debug.Log($"Added ${amount} to player");
        }
        else
        {
            Debug.LogError("PlayerMoneyManager not found!");
        }
    }

    private void ClosePrompt()
    {
        if (deliveryPromptUI != null)
            deliveryPromptUI.SetActive(false);

        isMenuOpen = false;

        // Unlock cursor and re-enable player controls
        LockCursor(true);
        EnablePlayerControls();
    }

    // Cursor control methods
    private void LockCursor(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // Player control methods
    private void DisablePlayerControls()
    {
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (playerLookScript != null)
            playerLookScript.enabled = false;
    }

    private void EnablePlayerControls()
    {
        if (playerMovementScript != null)
            playerMovementScript.enabled = true;

        if (playerLookScript != null)
            playerLookScript.enabled = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);

        // Draw line to nearby NPC if found
        if (nearbyNPC != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, nearbyNPC.transform.position);
        }
    }
}