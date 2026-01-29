using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TaskNotificationUI : MonoBehaviour
{
    public static TaskNotificationUI Instance { get; private set; }

    [Header("Task Panel")]
    public GameObject taskPanel;
    public Transform taskListContent;
    public GameObject taskItemPrefab;

    [Header("New Task Notification")]
    public GameObject newTaskNotification;
    public TextMeshProUGUI notificationText;
    public float notificationDuration = 3f;

    private Dictionary<string, GameObject> activeTaskItems = new Dictionary<string, GameObject>();
    private float notificationTimer;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (taskPanel != null)
            taskPanel.SetActive(false);

        if (newTaskNotification != null)
            newTaskNotification.SetActive(false);

        // Subscribe to time changes to update countdown
        DayNightCycleManager.OnTimeChanged += UpdateAllTaskTimers;
    }

    private void OnDestroy()
    {
        DayNightCycleManager.OnTimeChanged -= UpdateAllTaskTimers;
    }

    private void Update()
    {
        // Handle notification timer
        if (newTaskNotification != null && newTaskNotification.activeSelf)
        {
            notificationTimer -= Time.deltaTime;
            if (notificationTimer <= 0)
                newTaskNotification.SetActive(false);
        }

        // Toggle task panel with Tab key
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleTaskPanel();
        }
    }

    private void ToggleTaskPanel()
    {
        if (taskPanel != null)
            taskPanel.SetActive(!taskPanel.activeSelf);
    }

    public void AddTask(WeaponOrder order)
    {
        if (order == null || taskItemPrefab == null || taskListContent == null)
            return;

        // Remove old task for this NPC if exists
        if (activeTaskItems.ContainsKey(order.npcName))
        {
            Destroy(activeTaskItems[order.npcName]);
            activeTaskItems.Remove(order.npcName);
        }

        // Create new task item
        GameObject taskItem = Instantiate(taskItemPrefab, taskListContent);
        activeTaskItems[order.npcName] = taskItem;

        // Update task display
        UpdateTaskItem(taskItem, order);

        // Show task panel briefly
        if (taskPanel != null)
            taskPanel.SetActive(true);

        // Show notification
        ShowNewTaskNotification(order);
    }

    private void UpdateTaskItem(GameObject taskItem, WeaponOrder order)
    {
        if (taskItem == null || order == null)
            return;

        // Find text components (adjust names based on your prefab)
        TextMeshProUGUI npcText = taskItem.transform.Find("NPCName")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI weaponText = taskItem.transform.Find("WeaponInfo")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI locationText = taskItem.transform.Find("Location")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI timeText = taskItem.transform.Find("TimeRemaining")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI priceText = taskItem.transform.Find("Price")?.GetComponent<TextMeshProUGUI>();

        if (npcText != null)
            npcText.text = $"Client: {order.npcName}";

        if (weaponText != null)
        {
            string weaponInfo = order.weaponRequested.Name;
            List<string> attachments = new List<string>();

            if (order.sightAttachment != null) attachments.Add(order.sightAttachment.Name);
            if (order.underbarrelAttachment != null) attachments.Add(order.underbarrelAttachment.Name);
            if (order.barrelAttachment != null) attachments.Add(order.barrelAttachment.Name);
            if (order.magazineAttachment != null) attachments.Add(order.magazineAttachment.Name);
            if (order.sideRailAttachment != null) attachments.Add(order.sideRailAttachment.Name);

            if (attachments.Count > 0)
                weaponInfo += "\n+ " + string.Join(", ", attachments);

            weaponText.text = weaponInfo;
        }

        if (locationText != null)
            locationText.text = $"Location: {order.meetingLocation.name}";

        if (priceText != null)
            priceText.text = $"Payment: ${order.agreedPrice:F0}";

        if (timeText != null)
            UpdateTimeRemaining(timeText, order);
    }

    private void UpdateTimeRemaining(TextMeshProUGUI timeText, WeaponOrder order)
    {
        if (timeText == null || DayNightCycleManager.Instance == null)
            return;

        float currentTime = DayNightCycleManager.Instance.currentTimeOfDay;

        // PHASE 1: Before NPC arrives - show time until meeting
        if (!order.hasArrived)
        {
            float timeUntilMeeting;

            // Handle day rollover
            if (order.pickupTimeGameHour >= currentTime)
            {
                timeUntilMeeting = order.pickupTimeGameHour - currentTime;
            }
            else
            {
                // Meeting is tomorrow
                timeUntilMeeting = (24f - currentTime) + order.pickupTimeGameHour;
            }

            int meetingHour = Mathf.FloorToInt(order.pickupTimeGameHour);
            int meetingMinute = Mathf.FloorToInt((order.pickupTimeGameHour % 1f) * 60f);

            // Check if meeting time has arrived
            if (timeUntilMeeting <= 0f || currentTime >= order.pickupTimeGameHour)
            {
                timeText.color = Color.yellow;
                timeText.text = $"Meet at {meetingHour:00}:{meetingMinute:00} (En Route...)";
            }
            else
            {
                int hours = Mathf.FloorToInt(timeUntilMeeting);
                int minutes = Mathf.FloorToInt((timeUntilMeeting % 1f) * 60f);

                // Color code based on urgency
                if (timeUntilMeeting < 0.5f)
                    timeText.color = Color.red;
                else if (timeUntilMeeting < 1f)
                    timeText.color = Color.yellow;
                else
                    timeText.color = Color.white;

                timeText.text = $"Meet at {meetingHour:00}:{meetingMinute:00} ({hours}h {minutes}m)";
            }
        }
        // PHASE 2: After NPC arrives - show countdown until they leave
        else
        {
            // Get wait duration from TextingManager
            float waitDurationHours = 1f; // Default 1 hour
            if (TextingManager.Instance != null)
            {
                waitDurationHours = TextingManager.Instance.meetingWindowDuration / 3600f;
            }

            // Calculate time waited
            float timeWaited = currentTime - order.arrivalGameHour;
            if (timeWaited < 0f)
                timeWaited += 24f;

            // Calculate time remaining until NPC leaves
            float timeUntilLeaving = waitDurationHours - timeWaited;

            if (timeUntilLeaving <= 0f)
            {
                // NPC should be leaving/has left
                timeText.color = Color.red;
                timeText.text = "EXPIRED - NPC Left!";
            }
            else
            {
                int hours = Mathf.FloorToInt(timeUntilLeaving);
                int minutes = Mathf.FloorToInt((timeUntilLeaving % 1f) * 60f);
                int seconds = Mathf.FloorToInt(((timeUntilLeaving % 1f) * 60f % 1f) * 60f);

                // Color code based on urgency
                if (timeUntilLeaving < 0.083f) // Less than 5 minutes
                    timeText.color = Color.red;
                else if (timeUntilLeaving < 0.25f) // Less than 15 minutes
                    timeText.color = Color.yellow;
                else
                    timeText.color = Color.green;

                // Show different formats based on time remaining
                if (timeUntilLeaving >= 1f)
                {
                    // Show hours and minutes
                    timeText.text = $"WAITING - Leaves in {hours}h {minutes}m";
                }
                else if (timeUntilLeaving >= 0.0166f) // More than 1 minute
                {
                    // Show minutes and seconds
                    timeText.text = $"WAITING - Leaves in {minutes}m {seconds}s";
                }
                else
                {
                    // Show seconds only
                    timeText.text = $"WAITING - Leaves in {seconds}s!";
                }
            }
        }
    }

    private void UpdateAllTaskTimers(float currentTime)
    {
        if (TextingManager.Instance == null)
            return;

        foreach (var kvp in activeTaskItems)
        {
            WeaponOrder order = TextingManager.Instance.GetAcceptedOrderForNPC(kvp.Key);
            if (order != null)
            {
                TextMeshProUGUI timeText = kvp.Value.transform.Find("TimeRemaining")?.GetComponent<TextMeshProUGUI>();
                if (timeText != null)
                    UpdateTimeRemaining(timeText, order);
            }
        }
    }

    public void RemoveTask(string npcName)
    {
        if (activeTaskItems.ContainsKey(npcName))
        {
            Destroy(activeTaskItems[npcName]);
            activeTaskItems.Remove(npcName);

            Debug.Log($"Task removed for {npcName}. Remaining tasks: {activeTaskItems.Count}");
        }

        // Hide panel if no tasks remain
        if (activeTaskItems.Count == 0)
        {
            if (taskPanel != null && taskPanel.activeSelf)
            {
                taskPanel.SetActive(false);
                Debug.Log("Task panel closed - no tasks remaining");
            }
        }
    }

    private void ShowNewTaskNotification(WeaponOrder order)
    {
        if (newTaskNotification == null || notificationText == null)
            return;

        int meetingHour = Mathf.FloorToInt(order.pickupTimeGameHour);
        int meetingMinute = Mathf.FloorToInt((order.pickupTimeGameHour % 1f) * 60f);

        notificationText.text = $"NEW ORDER: {order.weaponRequested.Name}\n" +
                               $"Meet {order.npcName} at {order.meetingLocation.name}\n" +
                               $"Time: {meetingHour:00}:{meetingMinute:00}";

        newTaskNotification.SetActive(true);
        notificationTimer = notificationDuration;
    }

    public void RefreshTasks()
    {
        if (TextingManager.Instance == null)
            return;

        // Clear all tasks
        foreach (var item in activeTaskItems.Values)
            Destroy(item);
        activeTaskItems.Clear();

        // Recreate from active orders (you'll need to expose this in TextingManager)
        // For now, this is a placeholder for manual refresh
    }
}