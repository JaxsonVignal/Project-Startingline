using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TextingManager : MonoBehaviour
{
    public static TextingManager Instance { get; private set; }

    [Header("References")]
    public List<NPCManager> allNPCs = new List<NPCManager>();

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
    public float minTimeBetweenTexts = 120f;
    public float maxTimeBetweenTexts = 300f;

    private Dictionary<string, TextConversation> conversations = new Dictionary<string, TextConversation>();
    private List<WeaponOrder> activeOrders = new List<WeaponOrder>();
    private float nextTextTime;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        Debug.Log("TextingManager started!");
        Debug.Log($"NPCs available: {allNPCs.Count}");
        Debug.Log($"Weapons available: {availableWeapons.Count}");
        Debug.Log($"Meeting locations: {meetingLocations.Count}");

        ScheduleNextText();
    }

    private void Update()
    {
        // MANUAL TEST: Press T to force send a weapon request
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("Manual weapon request triggered with T key!");
            SendRandomWeaponRequest();
        }

        if (Time.time >= nextTextTime)
        {
            Debug.Log($"Time to send weapon request! (Time: {Time.time}, NextTextTime: {nextTextTime})");
            SendRandomWeaponRequest();
            ScheduleNextText();
        }

        CheckOrderPickups();
    }

    private void ScheduleNextText()
    {
        nextTextTime = Time.time + Random.Range(minTimeBetweenTexts, maxTimeBetweenTexts);
        float waitTime = nextTextTime - Time.time;
        Debug.Log($"Next text scheduled in {waitTime} seconds (at Time: {nextTextTime})");
    }

    public void SendRandomWeaponRequest()
    {
        Debug.Log("=== SendRandomWeaponRequest called ===");

        if (allNPCs.Count == 0)
        {
            Debug.LogError("No NPCs in allNPCs list!");
            return;
        }

        if (availableWeapons.Count == 0)
        {
            Debug.LogError("No weapons in availableWeapons list!");
            return;
        }

        if (meetingLocations.Count == 0)
        {
            Debug.LogError("No meeting locations!");
            return;
        }

        NPCManager randomNPC = allNPCs[Random.Range(0, allNPCs.Count)];
        string npcName = randomNPC.npcName;
        Debug.Log($"Selected NPC: {npcName}");

        WeaponData weapon = availableWeapons[Random.Range(0, availableWeapons.Count)];
        Debug.Log($"Selected weapon: {weapon.Name}");

        Transform location = meetingLocations[Random.Range(0, meetingLocations.Count)];
        Debug.Log($"Selected location: {location.name}");

        WeaponOrder order = new WeaponOrder(npcName, weapon, location);

        // Random attachment logic
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
        Debug.Log($"Order added to activeOrders. Total orders: {activeOrders.Count}");

        string message = BuildWeaponRequestMessage(order);
        Debug.Log($"Built message: {message}");

        SendMessage(npcName, message, false, TextMessage.MessageType.WeaponRequest);
        Debug.Log("=== Weapon request sent! ===");
    }

    private string BuildWeaponRequestMessage(WeaponOrder order)
    {
        string message = $"I need a new {order.weaponRequested.Name}";

        List<string> pieces = new List<string>();

        if (order.sightAttachment) pieces.Add($"Sight: {order.sightAttachment.Name}");
        if (order.underbarrelAttachment) pieces.Add($"Underbarrel: {order.underbarrelAttachment.Name}");
        if (order.barrelAttachment) pieces.Add($"Barrel: {order.barrelAttachment.Name}");
        if (order.magazineAttachment) pieces.Add($"Magazine: {order.magazineAttachment.Name}");
        if (order.sideRailAttachment) pieces.Add($"Side Rail: {order.sideRailAttachment.Name}");

        if (pieces.Count > 0)
            message += " with " + string.Join(", ", pieces);

        message += $" at {order.meetingLocation.name}.";

        return message;
    }

    public void SendMessage(string npcName, string content, bool fromPlayer, TextMessage.MessageType type)
    {
        if (!conversations.ContainsKey(npcName))
            conversations[npcName] = new TextConversation(npcName);

        TextConversation convo = conversations[npcName];

        convo.AddMessage(new TextMessage(npcName, content, fromPlayer, type));

        PhoneUI.Instance?.OnNewMessage(npcName);
    }

    public void SendPriceOffer(string npcName, float price, float pickupHours)
    {
        WeaponOrder order = activeOrders.Find(o => o.npcName == npcName && !o.isPriceSet);

        if (order == null)
            return;

        order.agreedPrice = price;
        order.pickupTime = pickupHours; // Store in-game hours until meeting
        order.pickupTimeGameHour = DayNightCycleManager.Instance.currentTimeOfDay + pickupHours; // Calculate target time

        if (order.pickupTimeGameHour >= 24f)
            order.pickupTimeGameHour -= 24f;

        order.isPriceSet = true;
        order.isAccepted = true;

        SendMessage(
            npcName,
            $"${price:F0} in {pickupHours:F1} hours? Sounds good, I'll meet you at {order.meetingLocation.name}.",
            false,
            TextMessage.MessageType.Acceptance
        );

        // Add task to HUD
        if (TaskNotificationUI.Instance != null)
            TaskNotificationUI.Instance.AddTask(order);

        Debug.Log($"Order accepted: {npcName}, price ${price}, pickup in {pickupHours}h (at game hour {order.pickupTimeGameHour:F2})");
    }

    private void CheckOrderPickups()
    {
        if (DayNightCycleManager.Instance == null) return;

        foreach (WeaponOrder order in activeOrders)
        {
            // Only spawn NPC once when the scheduled time is reached
            if (order.isAccepted && !order.isCompleted)
            {
                float currentGameHour = DayNightCycleManager.Instance.currentTimeOfDay;

                // Check if we've reached or passed the scheduled meeting time
                bool timeToMeet = false;

                if (order.pickupTimeGameHour > currentGameHour)
                {
                    // Normal case: meeting is later today
                    timeToMeet = false;
                }
                else if (order.pickupTimeGameHour < 6f && currentGameHour > 20f)
                {
                    // Meeting is early morning, we're still in late evening
                    timeToMeet = false;
                }
                else
                {
                    // We've reached or passed the meeting time
                    timeToMeet = true;
                }

                if (timeToMeet)
                {
                    SpawnNPCAtLocation(order);
                    order.isCompleted = true;
                }
            }
        }
    }

    private void SpawnNPCAtLocation(WeaponOrder order)
    {
        NPCManager npc = allNPCs.Find(n => n.npcName == order.npcName);

        if (npc == null)
        {
            Debug.LogWarning($"Could not find NPC: {order.npcName}");
            return;
        }

        npc.ScheduleWeaponMeeting(order.meetingLocation, order.pickupTime);
        Debug.Log($"Spawned {order.npcName} for meeting at {order.meetingLocation.name}");
    }

    // ===== PUBLIC METHODS =====

    public List<string> GetAllConversationNames()
    {
        return new List<string>(conversations.Keys);
    }

    public bool HasUnreadMessages(string npcName)
    {
        if (conversations.ContainsKey(npcName))
            return conversations[npcName].hasUnreadMessages;
        return false;
    }

    public TextConversation GetConversation(string npcName)
    {
        if (conversations.ContainsKey(npcName))
            return conversations[npcName];
        return null;
    }

    public WeaponOrder GetActiveOrderForNPC(string npcName)
    {
        return activeOrders.Find(o => o.npcName == npcName && !o.isCompleted);
    }

    public WeaponOrder GetAcceptedOrderForNPC(string npcName)
    {
        return activeOrders.Find(o => o.npcName == npcName && o.isAccepted && !o.isCompleted);
    }

    public bool IsNPCReadyForDelivery(string npcName)
    {
        WeaponOrder order = GetAcceptedOrderForNPC(npcName);
        return order != null && order.isAccepted;
    }

    public void CompleteWeaponDelivery(string npcName)
    {
        WeaponOrder order = GetAcceptedOrderForNPC(npcName);
        if (order != null)
        {
            order.isCompleted = true;
            activeOrders.Remove(order);

            NPCManager npc = allNPCs.Find(n => n.npcName == npcName);
            if (npc != null)
                npc.CompleteWeaponDeal();

            SendMessage(npcName, "Thanks! Pleasure doing business.", false, TextMessage.MessageType.General);

            // Remove task from HUD
            if (TaskNotificationUI.Instance != null)
                TaskNotificationUI.Instance.RemoveTask(npcName);

            Debug.Log($"Completed delivery to {npcName}");
        }
    }
}