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
    public Text promptText;
    public Button deliverButton;
    public Button cancelButton;

    [Header("Interaction Settings")]
    public float interactionRange = 3f;
    public KeyCode interactKey = KeyCode.E;

    private NPCManager nearbyNPC;
    private WeaponOrder currentOrder;

    private void Start()
    {
        if (deliveryPromptUI != null)
            deliveryPromptUI.SetActive(false);

        if (deliverButton != null)
            deliverButton.onClick.AddListener(DeliverWeapon);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(ClosePrompt);
    }

    private void Update()
    {
        CheckForNearbyNPC();

        if (nearbyNPC != null && currentOrder != null)
        {
            if (Input.GetKeyDown(interactKey))
                ShowDeliveryPrompt();
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
                // Check: Is NPC waiting at meeting?
                if (TextingManager.Instance.IsNPCReadyForDelivery(npc.npcName))
                {
                    nearbyNPC = npc;

                    // FIX: now retrieves accepted orders, not pending price orders
                    currentOrder = TextingManager.Instance.GetAcceptedOrderForNPC(npc.npcName);

                    break;
                }
            }
        }
    }

    private void ShowDeliveryPrompt()
    {
        if (deliveryPromptUI == null || currentOrder == null) return;

        deliveryPromptUI.SetActive(true);

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
    }

    private void DeliverWeapon()
    {
        if (currentOrder == null || nearbyNPC == null) return;

        if (CheckPlayerHasItems())
        {
            RemoveItemsFromInventory();
            AddMoneyToPlayer(currentOrder.agreedPrice);

            // COMPLETE DELIVERY
            TextingManager.Instance.CompleteWeaponDelivery(nearbyNPC.npcName);

            Debug.Log($"Delivered weapon to {nearbyNPC.npcName}");

            ClosePrompt();
        }
        else
        {
            Debug.LogWarning("Player does NOT have the required items!");
            ClosePrompt();
        }
    }

    private bool CheckPlayerHasItems()
    {
        // TODO: hook into your inventory
        return true;
    }

    private void RemoveItemsFromInventory()
    {
        // TODO: remove items
        Debug.Log("Removed weapon from inventory (implement)");
    }

    private void AddMoneyToPlayer(float amount)
    {
        // TODO: add money
        Debug.Log($"Added ${amount} to player (implement)");
    }

    private void ClosePrompt()
    {
        if (deliveryPromptUI != null)
            deliveryPromptUI.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
