using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TextingManager : MonoBehaviour
{
    public static TextingManager Instance { get; private set; }
    
    [Header("References")]
    public List<NPCManager> allNPCs = new List<NPCManager>(); // List of all NPCs in the scene
    
    [Header("Weapon and Attachment Pools")]
    public List<WeaponData> availableWeapons;
    public List<AttachmentData> sightAttachments;
    public List<AttachmentData> underbarrelAttachments;
    public List<AttachmentData> barrelAttachments;
    public List<AttachmentData> magazineAttachments;
    public List<AttachmentData> sideRailAttachments;
    
    [Header("Meeting Locations")]
    public List<Transform> meetingLocations;
    
    [Header("Timing Settings")]
    public float minTimeBetweenTexts = 120f; // 2 minutes
    public float maxTimeBetweenTexts = 300f; // 5 minutes
    
    // Data storage
    private Dictionary<string, TextConversation> conversations = new Dictionary<string, TextConversation>();
    private List<WeaponOrder> activeOrders = new List<WeaponOrder>();
    private float nextTextTime;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        ScheduleNextText();
    }
    
    private void Update()
    {
        // Check if it's time to send a random text
        if (Time.time >= nextTextTime)
        {
            SendRandomWeaponRequest();
            ScheduleNextText();
        }
        
        // Check for completed orders (NPCs waiting at location)
        CheckOrderPickups();
    }
    
    private void ScheduleNextText()
    {
        nextTextTime = Time.time + Random.Range(minTimeBetweenTexts, maxTimeBetweenTexts);
    }
    
    public void SendRandomWeaponRequest()
    {
        if (allNPCs == null || allNPCs.Count == 0)
        {
            Debug.LogWarning("No NPCs available to send texts");
            return;
        }
        
        // Get random NPC
        NPCManager randomNPC = allNPCs[Random.Range(0, allNPCs.Count)];
        string npcName = randomNPC.npcName;
        
        // Get random weapon and attachments
        WeaponData weapon = availableWeapons[Random.Range(0, availableWeapons.Count)];
        Transform location = meetingLocations[Random.Range(0, meetingLocations.Count)];
        
        // Create order
        WeaponOrder order = new WeaponOrder(npcName, weapon, location);
        
        // Randomly assign attachments (not all slots need to be filled)
        if (sightAttachments.Count > 0 && Random.value > 0.3f)
            order.sightAttachment = sightAttachments[Random.Range(0, sightAttachments.Count)];
        
        if (underbarrelAttachments.Count > 0 && Random.value > 0.3f)
            order.underbarrelAttachment = underbarrelAttachments[Random.Range(0, underbarrelAttachments.Count)];
        
        if (barrelAttachments.Count > 0 && Random.value > 0.3f)
            order.barrelAttachment = barrelAttachments[Random.Range(0, barrelAttachments.Count)];
        
        if (magazineAttachments.Count > 0 && Random.value > 0.3f)
            order.magazineAttachment = magazineAttachments[Random.Range(0, magazineAttachments.Count)];
        
        if (sideRailAttachments.Count > 0 && Random.value > 0.3f)
            order.sideRailAttachment = sideRailAttachments[Random.Range(0, sideRailAttachments.Count)];
        
        activeOrders.Add(order);
        
        // Build message
        string message = BuildWeaponRequestMessage(order);
        
        // Send message
        SendMessage(npcName, message, false, TextMessage.MessageType.WeaponRequest);
        
        Debug.Log($"New weapon request from {npcName}: {weapon.Name}");
    }
    
    private string BuildWeaponRequestMessage(WeaponOrder order)
    {
        string message = $"I need a new {order.weaponRequested.Name}";
        
        List<string> attachments = new List<string>();
        
        if (order.sightAttachment != null)
            attachments.Add($"Sight: {order.sightAttachment.Name}");
        
        if (order.underbarrelAttachment != null)
            attachments.Add($"Underbarrel: {order.underbarrelAttachment.Name}");
        
        if (order.barrelAttachment != null)
            attachments.Add($"Barrel: {order.barrelAttachment.Name}");
            
        if (order.magazineAttachment != null)
            attachments.Add($"Magazine: {order.magazineAttachment.Name}");
        
        if (order.sideRailAttachment != null)
            attachments.Add($"Side Rail: {order.sideRailAttachment.Name}");
        
        if (attachments.Count > 0)
        {
            message += " with " + string.Join(", ", attachments);
        }
        
        message += $" at {order.meetingLocation.name}.";
        
        return message;
    }
    
    public void SendMessage(string npcName, string content, bool fromPlayer, TextMessage.MessageType type = TextMessage.MessageType.General)
    {
        // Get or create conversation
        if (!conversations.ContainsKey(npcName))
        {
            conversations[npcName] = new TextConversation(npcName);
        }
        
        TextMessage message = new TextMessage(npcName, content, fromPlayer, type);
        conversations[npcName].AddMessage(message);
        
        // Notify UI to update
        if (PhoneUI.Instance != null)
        {
            PhoneUI.Instance.OnNewMessage(npcName);
        }
    }
    
    public void SendPriceOffer(string npcName, float price, float pickupTime)
    {
        // Find the active order for this NPC
        WeaponOrder order = activeOrders.Find(o => o.npcName == npcName && !o.isPriceSet);
        
        if (order != null)
        {
            order.agreedPrice = price;
            order.pickupTime = Time.time + (pickupTime * 60f); // Convert minutes to seconds
            order.isPriceSet = true;
            order.isAccepted = true;
            
            string message = $"${price:F0} in {pickupTime:F0} minutes? Sounds good, I'll meet you at {order.meetingLocation.name}.";
            SendMessage(npcName, message, false, TextMessage.MessageType.Acceptance);
            
            Debug.Log($"Order accepted by {npcName}: ${price} at {order.meetingLocation.name}");
        }
    }
    
    private void CheckOrderPickups()
    {
        foreach (WeaponOrder order in activeOrders)
        {
            if (order.isAccepted && !order.isCompleted && Time.time >= order.pickupTime)
            {
                // Spawn NPC at location (you'll need to implement this based on your NPC system)
                SpawnNPCAtLocation(order);
                order.isCompleted = true;
            }
        }
        
        // Clean up completed orders
        activeOrders.RemoveAll(o => o.isCompleted);
    }
    
    private void SpawnNPCAtLocation(WeaponOrder order)
    {
        // Find the NPC by name
        NPCManager npc = allNPCs.Find(n => n.npcName == order.npcName);
        
        if (npc != null)
        {
            // Schedule the NPC to go to the meeting location
            // The pickup time is stored as the actual game time when meeting occurs
            float timeUntilMeeting = order.pickupTime - Time.time;
            
            npc.ScheduleWeaponMeeting(order.meetingLocation, timeUntilMeeting);
            
            Debug.Log($"{order.npcName} will go to {order.meetingLocation.name} for weapon pickup");
        }
        else
        {
            Debug.LogWarning($"Could not find NPC with name: {order.npcName}");
        }
    }
    
    public TextConversation GetConversation(string npcName)
    {
        if (conversations.ContainsKey(npcName))
        {
            conversations[npcName].MarkAsRead();
            return conversations[npcName];
        }
        return null;
    }
    
    public List<string> GetAllConversationNames()
    {
        return new List<string>(conversations.Keys);
    }
    
    public bool HasUnreadMessages(string npcName)
    {
        if (conversations.ContainsKey(npcName))
        {
            return conversations[npcName].hasUnreadMessages;
        }
        return false;
    }
    
    public WeaponOrder GetActiveOrderForNPC(string npcName)
    {
        return activeOrders.Find(o => o.npcName == npcName && !o.isPriceSet);
    }
    
    /// <summary>
    /// Get the NPC manager instance for a specific NPC name
    /// </summary>
    public NPCManager GetNPCByName(string npcName)
    {
        return allNPCs.Find(n => n.npcName == npcName);
    }
    
    /// <summary>
    /// Check if NPC is at meeting location and ready for weapon delivery
    /// </summary>
    public bool IsNPCReadyForDelivery(string npcName)
    {
        NPCManager npc = GetNPCByName(npcName);
        if (npc == null) return false;
        
        return npc.IsAtMeetingLocation();
    }
    
    /// <summary>
    /// Complete a weapon delivery - call this when player successfully delivers weapon
    /// </summary>
    public void CompleteWeaponDelivery(string npcName)
    {
        WeaponOrder order = activeOrders.Find(o => o.npcName == npcName && o.isAccepted && !o.isCompleted);
        
        if (order != null)
        {
            order.isCompleted = true;
            
            NPCManager npc = GetNPCByName(npcName);
            if (npc != null)
            {
                npc.CompleteWeaponDeal();
            }
            
            // Send a thank you message
            SendMessage(npcName, "Perfect! Thanks for the delivery.", false, TextMessage.MessageType.General);
            
            Debug.Log($"Weapon delivery to {npcName} completed! Payment: ${order.agreedPrice}");
        }
    }
}
