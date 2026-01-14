using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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
    public GameObject interactionHintUI;
    public TMP_Text hintText;

    [Header("Interaction Settings")]
    public float interactionRange = 3f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private NPCManager nearbyNPC;
    private WeaponOrder currentOrder;
    private bool isMenuOpen = false;

    [Header("Player Control References")]
    public MonoBehaviour playerMovementScript;
    public MonoBehaviour playerLookScript;

    [Header("Weapon System")]
    public Transform weaponHolder; // Where all weapons are stored

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
        if (!isMenuOpen)
        {
            CheckForNearbyNPC();

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

        HideInteractionHint();
    }

    private void DeliverWeapon()
    {
        if (currentOrder == null || nearbyNPC == null)
        {
            Debug.LogError("Cannot deliver: order or NPC is null!");
            return;
        }

        // Find the EXACT weapon with correct attachments from ALL weapons in inventory
        WeaponAttachmentSystem matchingWeapon = FindExactMatchingWeapon();

        if (matchingWeapon != null)
        {
            RemoveWeaponFromInventory(matchingWeapon);
            AddMoneyToPlayer(currentOrder.agreedPrice);
            TextingManager.Instance.CompleteWeaponDelivery(nearbyNPC.npcName);

            Debug.Log($"✓ Delivered weapon to {nearbyNPC.npcName} for ${currentOrder.agreedPrice}");

            ClosePrompt();
        }
        else
        {
            Debug.LogWarning($"✗ Order FAILED: No weapon found with exact attachments!");

            TextingManager.Instance.CompleteWeaponDelivery(nearbyNPC.npcName);

            if (promptText != null)
            {
                promptText.text = "Delivery FAILED!\n\nYou don't have a weapon with the exact attachments required.\n\nOrder cancelled - no payment received.";
            }

            Invoke(nameof(ClosePrompt), 2.5f);
        }
    }

    /// <summary>
    /// Finds the EXACT weapon that matches both weapon type AND all attachments
    /// Searches ALL weapons in inventory, not just the currently held one
    /// </summary>
    private WeaponAttachmentSystem FindExactMatchingWeapon()
    {
        if (showDebugLogs)
        {
            Debug.Log($"\n╔════════════════════════════════════════╗");
            Debug.Log($"║  SEARCHING FOR EXACT WEAPON MATCH     ║");
            Debug.Log($"╚════════════════════════════════════════╝");
            Debug.Log($"Required Weapon: {currentOrder.weaponRequested.Name}");
            Debug.Log($"\n--- REQUIRED ATTACHMENTS ---");
            LogRequiredAttachments();
        }

        // Get ALL weapon GameObjects from inventory (active AND inactive)
        WeaponAttachmentSystem[] allWeapons = GetAllWeaponsInInventory();

        if (showDebugLogs)
        {
            Debug.Log($"\n╔════════════════════════════════════════╗");
            Debug.Log($"║  FOUND {allWeapons.Length} WEAPONS IN INVENTORY      ║");
            Debug.Log($"╚════════════════════════════════════════╝");
        }

        // Track all weapons of the correct type
        List<WeaponAttachmentSystem> correctTypeWeapons = new List<WeaponAttachmentSystem>();

        // Log all weapons found and filter by type
        for (int i = 0; i < allWeapons.Length; i++)
        {
            var weapon = allWeapons[i];

            if (weapon == null || weapon.weaponData == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[{i}] Weapon is null or has no data - SKIPPED");
                continue;
            }

            bool isCorrectType = IsCorrectWeaponType(weapon);

            if (showDebugLogs)
            {
                Debug.Log($"\n[{i}] ═══════════════════════════════════");
                Debug.Log($"  Weapon: {weapon.weaponData.Name}");
                Debug.Log($"  GameObject: {weapon.gameObject.name}");
                Debug.Log($"  Active: {weapon.gameObject.activeInHierarchy}");
                Debug.Log($"  Correct Type: {(isCorrectType ? "✓ YES" : "✗ NO")}");

                if (weapon.equippedAttachments.Count > 0)
                {
                    Debug.Log($"  Equipped Attachments ({weapon.equippedAttachments.Count}):");
                    foreach (var att in weapon.equippedAttachments)
                    {
                        if (att != null)
                            Debug.Log($"    • {att.type}: {att.name}");
                    }
                }
                else
                {
                    Debug.Log($"  Equipped Attachments: NONE");
                }
            }

            if (isCorrectType)
            {
                correctTypeWeapons.Add(weapon);
            }
        }

        if (correctTypeWeapons.Count == 0)
        {
            if (showDebugLogs)
                Debug.LogWarning($"✗✗✗ NO WEAPONS OF TYPE '{currentOrder.weaponRequested.Name}' FOUND!");
            return null;
        }

        if (showDebugLogs)
        {
            Debug.Log($"\n╔════════════════════════════════════════╗");
            Debug.Log($"║  {correctTypeWeapons.Count} WEAPON(S) OF CORRECT TYPE     ║");
            Debug.Log($"╚════════════════════════════════════════╝");
        }

        // Now check each weapon of correct type for exact attachment match
        for (int i = 0; i < correctTypeWeapons.Count; i++)
        {
            var weapon = correctTypeWeapons[i];

            if (showDebugLogs)
            {
                Debug.Log($"\n┌─────────────────────────────────────┐");
                Debug.Log($"│ CHECKING WEAPON [{i}]: {weapon.weaponData.Name}");
                Debug.Log($"└─────────────────────────────────────┘");
            }

            if (HasExactAttachments(weapon))
            {
                if (showDebugLogs)
                {
                    Debug.Log($"\n╔════════════════════════════════════════╗");
                    Debug.Log($"║  ✓✓✓ EXACT MATCH FOUND ✓✓✓           ║");
                    Debug.Log($"╚════════════════════════════════════════╝");
                    Debug.Log($"Weapon: {weapon.weaponData.Name}");
                    Debug.Log($"GameObject: {weapon.gameObject.name}\n");
                }
                return weapon;
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogWarning($"✗ Attachments don't match for weapon [{i}]");
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"\n╔════════════════════════════════════════╗");
            Debug.Log($"║  ✗✗✗ NO EXACT MATCH FOUND ✗✗✗        ║");
            Debug.Log($"╚════════════════════════════════════════╝");
        }
        return null;
    }

    private bool IsCorrectWeaponType(WeaponAttachmentSystem weapon)
    {
        // Check by reference first (most reliable)
        if (weapon.weaponData == currentOrder.weaponRequested)
            return true;

        // Fallback: check by name
        if (weapon.weaponData.Name == currentOrder.weaponRequested.Name)
            return true;

        return false;
    }

    /// <summary>
    /// Checks if weapon has EXACTLY the required attachments (no more, no less)
    /// </summary>
    private bool HasExactAttachments(WeaponAttachmentSystem weapon)
    {
        if (showDebugLogs)
            Debug.Log("  ┌─ Checking Attachments ─┐");

        bool sightOk = CheckAttachment(weapon, AttachmentType.Sight, currentOrder.sightAttachment);
        bool underbarrelOk = CheckAttachment(weapon, AttachmentType.Underbarrel, currentOrder.underbarrelAttachment);
        bool barrelOk = CheckAttachment(weapon, AttachmentType.Barrel, currentOrder.barrelAttachment);
        bool magazineOk = CheckAttachment(weapon, AttachmentType.Magazine, currentOrder.magazineAttachment);
        bool sideRailOk = CheckAttachment(weapon, AttachmentType.SideRail, currentOrder.sideRailAttachment);

        bool allMatch = sightOk && underbarrelOk && barrelOk && magazineOk && sideRailOk;

        if (showDebugLogs)
            Debug.Log($"  └─ Result: {(allMatch ? "✓ ALL MATCH" : "✗ MISMATCH")} ─┘");

        return allMatch;
    }

    private void LogRequiredAttachments()
    {
        if (currentOrder.sightAttachment != null)
            Debug.Log($"  • Sight: {currentOrder.sightAttachment.name}");
        if (currentOrder.underbarrelAttachment != null)
            Debug.Log($"  • Underbarrel: {currentOrder.underbarrelAttachment.name}");
        if (currentOrder.barrelAttachment != null)
            Debug.Log($"  • Barrel: {currentOrder.barrelAttachment.name}");
        if (currentOrder.magazineAttachment != null)
            Debug.Log($"  • Magazine: {currentOrder.magazineAttachment.name}");
        if (currentOrder.sideRailAttachment != null)
            Debug.Log($"  • SideRail: {currentOrder.sideRailAttachment.name}");

        // Count required attachments
        int requiredCount = 0;
        if (currentOrder.sightAttachment != null) requiredCount++;
        if (currentOrder.underbarrelAttachment != null) requiredCount++;
        if (currentOrder.barrelAttachment != null) requiredCount++;
        if (currentOrder.magazineAttachment != null) requiredCount++;
        if (currentOrder.sideRailAttachment != null) requiredCount++;

        if (requiredCount == 0)
            Debug.Log("  • No attachments required");
    }

    private WeaponAttachmentSystem[] GetAllWeaponsInInventory()
    {
        if (showDebugLogs)
            Debug.Log(">>> Searching for weapon GameObjects in inventory...");

        // Search in weaponHolder if assigned
        if (weaponHolder != null)
        {
            // true = include inactive GameObjects
            WeaponAttachmentSystem[] weapons = weaponHolder.GetComponentsInChildren<WeaponAttachmentSystem>(true);
            if (showDebugLogs)
                Debug.Log($">>> Found {weapons.Length} weapons in weaponHolder (including inactive)");
            return weapons;
        }

        // Fallback: search in all player children
        WeaponAttachmentSystem[] allWeapons = GetComponentsInChildren<WeaponAttachmentSystem>(true);
        if (showDebugLogs)
            Debug.Log($">>> Found {allWeapons.Length} weapons in player children (including inactive)");
        return allWeapons;
    }

    private bool CheckAttachment(WeaponAttachmentSystem weapon, AttachmentType type, AttachmentData requiredAttachment)
    {
        // If no attachment is required for this slot
        if (requiredAttachment == null)
        {
            // Check if weapon has something equipped in this slot
            bool hasAttachment = weapon.equippedAttachments.Any(att => att != null && att.type == type);

            if (hasAttachment)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"  [{type}] ✗ EXTRA - Not required but weapon has one equipped!");
                return false; // Weapon has extra attachment that wasn't ordered
            }

            if (showDebugLogs)
                Debug.Log($"  [{type}] ✓ Not required, none equipped");
            return true;
        }

        // Required attachment specified
        if (showDebugLogs)
            Debug.Log($"  [{type}] Required: {requiredAttachment.name}");

        AttachmentData equippedAttachment = weapon.equippedAttachments
            .FirstOrDefault(att => att != null && att.type == type);

        if (equippedAttachment == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"  [{type}] ✗ MISSING - Required '{requiredAttachment.name}' but nothing equipped");
            return false;
        }

        if (showDebugLogs)
            Debug.Log($"  [{type}] Found: {equippedAttachment.name}");

        // Compare by reference AND name
        bool matches = equippedAttachment == requiredAttachment ||
                       equippedAttachment.name == requiredAttachment.name;

        if (!matches)
        {
            if (showDebugLogs)
                Debug.LogWarning($"  [{type}] ✗ WRONG - Expected '{requiredAttachment.name}' got '{equippedAttachment.name}'");
            return false;
        }

        if (showDebugLogs)
            Debug.Log($"  [{type}] ✓ CORRECT - {requiredAttachment.name}");
        return true;
    }

    private void RemoveWeaponFromInventory(WeaponAttachmentSystem weaponToRemove)
    {
        if (weaponToRemove == null)
        {
            Debug.LogError("Cannot remove weapon: weaponToRemove is null!");
            return;
        }

        PlayerInventoryHolder playerInventory = GetComponent<PlayerInventoryHolder>();
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventoryHolder not found!");
            return;
        }

        var inventory = playerInventory.PrimaryInventorySystem;

        // Find and remove from inventory slot
        var weaponSlot = inventory.InventorySlots.FirstOrDefault(slot =>
            slot.ItemData == weaponToRemove.weaponData ||
            (slot.ItemData != null && slot.ItemData.Name == weaponToRemove.weaponData.Name));

        if (weaponSlot != null && weaponSlot.ItemData != null)
        {
            if (weaponSlot.StackSize > 1)
            {
                weaponSlot.RemoveFromStack(1);
                if (showDebugLogs)
                    Debug.Log($"Removed 1 from weapon stack. Remaining: {weaponSlot.StackSize}");
            }
            else
            {
                weaponSlot.ClearSlot();
                if (showDebugLogs)
                    Debug.Log($"Cleared weapon slot");
            }

            inventory.OnInventorySlotChanged?.Invoke(weaponSlot);
        }
        else
        {
            Debug.LogWarning("Could not find weapon in inventory slots!");
        }

        // Destroy the weapon GameObject (includes all attachments)
        Destroy(weaponToRemove.gameObject);

        if (showDebugLogs)
            Debug.Log($"Removed and destroyed weapon: {weaponToRemove.weaponData.Name}");
    }

    private void AddMoneyToPlayer(float amount)
    {
        PlayerMoneyManager money = GetComponent<PlayerMoneyManager>();

        if (money != null)
        {
            money.AddMoney(amount, $"Weapon delivery to {nearbyNPC.npcName}");
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

        LockCursor(true);
        EnablePlayerControls();
    }

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

        if (nearbyNPC != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, nearbyNPC.transform.position);
        }
    }
}