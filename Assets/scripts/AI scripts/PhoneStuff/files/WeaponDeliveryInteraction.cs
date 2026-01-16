using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private WeaponConfigurationInventory configInventory;
    private PlayerInventoryHolder inventoryHolder;

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

        // Get required components
        configInventory = GetComponent<WeaponConfigurationInventory>();
        inventoryHolder = GetComponent<PlayerInventoryHolder>();

        if (configInventory == null)
        {
            Debug.LogError("WeaponConfigurationInventory component not found! Please add it to the player.");
        }

        if (inventoryHolder == null)
        {
            Debug.LogError("PlayerInventoryHolder component not found!");
        }
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

        if (configInventory == null || inventoryHolder == null)
        {
            Debug.LogError("Required components missing!");
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log($"\n╔═══════════════════════════════════════╗");
            Debug.Log($"║  WEAPON DELIVERY ATTEMPT              ║");
            Debug.Log($"╚═══════════════════════════════════════╝");
        }

        // Find matching weapon configuration in inventory
        WeaponConfiguration matchingConfig = configInventory.FindMatchingConfiguration(currentOrder);

        if (matchingConfig != null)
        {
            if (showDebugLogs)
            {
                Debug.Log($"\n✓ FOUND MATCHING WEAPON!");
                Debug.Log($"Configuration ID: {matchingConfig.configId}");
                Debug.Log(matchingConfig.ToString());
            }

            // Remove weapon and attachments from inventory
            if (configInventory.RemoveWeaponConfiguration(matchingConfig, inventoryHolder.PrimaryInventorySystem))
            {
                // Add money to player
                AddMoneyToPlayer(currentOrder.agreedPrice);

                // Mark order as complete
                TextingManager.Instance.CompleteWeaponDelivery(nearbyNPC.npcName);

                Debug.Log($"✓ Delivered weapon to {nearbyNPC.npcName} for ${currentOrder.agreedPrice}");

                ClosePrompt();
            }
            else
            {
                Debug.LogError("Failed to remove weapon configuration from inventory!");

                if (promptText != null)
                {
                    promptText.text = "ERROR: Failed to remove weapon from inventory!";
                }

                Invoke(nameof(ClosePrompt), 2.5f);
            }
        }
        else
        {
            Debug.LogWarning($"✗ Order FAILED: No weapon found with exact attachments!");
            Debug.LogWarning($"Available weapon configurations: {configInventory.WeaponConfigurations.Count}");

            // Still complete the order (player loses the opportunity)
            TextingManager.Instance.CompleteWeaponDelivery(nearbyNPC.npcName);

            if (promptText != null)
            {
                promptText.text = "Delivery FAILED!\n\nYou don't have a weapon with the exact attachments required.\n\nOrder cancelled - no payment received.";
            }

            Invoke(nameof(ClosePrompt), 2.5f);
        }
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