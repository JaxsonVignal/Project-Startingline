using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Example script showing how to handle weapon delivery to NPCs
/// Attach this to your player or create an interaction system
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
        // Check for nearby NPCs waiting for delivery
        CheckForNearbyNPC();
        
        // Show interaction prompt if near NPC with active order
        if (nearbyNPC != null && currentOrder != null)
        {
            if (Input.GetKeyDown(interactKey))
            {
                ShowDeliveryPrompt();
            }
        }
    }
    
    private void CheckForNearbyNPC()
    {
        nearbyNPC = null;
        currentOrder = null;
        
        // Find all NPCs in range
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRange);
        
        foreach (Collider col in colliders)
        {
            NPCManager npc = col.GetComponent<NPCManager>();
            if (npc != null)
            {
                // Check if this NPC is waiting at meeting location
                if (TextingManager.Instance.IsNPCReadyForDelivery(npc.npcName))
                {
                    nearbyNPC = npc;
                    currentOrder = TextingManager.Instance.GetActiveOrderForNPC(npc.npcName);
                    break;
                }
            }
        }
    }
    
    private void ShowDeliveryPrompt()
    {
        if (deliveryPromptUI == null || currentOrder == null) return;
        
        deliveryPromptUI.SetActive(true);
        
        // Build prompt text
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
            promptText.text = $"Deliver to {nearbyNPC.npcName}?\n\n" +
                             $"Weapon: {weaponInfo}" +
                             attachmentInfo +
                             $"\n\nPayment: ${currentOrder.agreedPrice:F0}";
        }
    }
    
    private void DeliverWeapon()
    {
        if (currentOrder == null || nearbyNPC == null) return;
        
        // Check if player has the items in inventory
        // YOU NEED TO IMPLEMENT THIS based on your inventory system
        if (CheckPlayerHasItems())
        {
            // Remove items from inventory
            RemoveItemsFromInventory();
            
            // Add money to player
            // YOU NEED TO IMPLEMENT THIS based on your money system
            AddMoneyToPlayer(currentOrder.agreedPrice);
            
            // Complete the delivery
            TextingManager.Instance.CompleteWeaponDelivery(nearbyNPC.npcName);
            
            Debug.Log($"Successfully delivered weapon to {nearbyNPC.npcName} for ${currentOrder.agreedPrice}");
            
            ClosePrompt();
        }
        else
        {
            Debug.LogWarning("Player doesn't have the required items!");
            // You could show an error message here
            ClosePrompt();
        }
    }
    
    private bool CheckPlayerHasItems()
    {
        // IMPLEMENT THIS: Check if player has weapon and attachments in inventory
        // Example:
        // return inventory.HasWeapon(currentOrder.weaponRequested) &&
        //        inventory.HasAttachment(currentOrder.sightAttachment) &&
        //        ... etc
        
        // For now, always return true (you need to implement this)
        return true;
    }
    
    private void RemoveItemsFromInventory()
    {
        // IMPLEMENT THIS: Remove weapon and attachments from inventory
        // Example:
        // inventory.RemoveWeapon(currentOrder.weaponRequested);
        // if (currentOrder.sightAttachment != null)
        //     inventory.RemoveAttachment(currentOrder.sightAttachment);
        // ... etc
        
        Debug.Log("Removed items from inventory (implement this!)");
    }
    
    private void AddMoneyToPlayer(float amount)
    {
        // IMPLEMENT THIS: Add money to player
        // Example:
        // PlayerMoney.Instance.AddMoney(amount);
        
        Debug.Log($"Added ${amount} to player (implement this!)");
    }
    
    private void ClosePrompt()
    {
        if (deliveryPromptUI != null)
            deliveryPromptUI.SetActive(false);
    }
    
    private void OnDrawGizmosSelected()
    {
        // Visualize interaction range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
