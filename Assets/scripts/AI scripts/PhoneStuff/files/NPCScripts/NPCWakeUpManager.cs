using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages sleeping NPCs and wakes them up when the day changes
/// This script stays active even when NPCs are disabled
/// </summary>
public class NPCSleepManager : MonoBehaviour
{
    public static NPCSleepManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // Track all NPCs in the scene
    private List<NPCManager> allNPCs = new List<NPCManager>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        // Subscribe to day change events
        DayNightCycleManager.OnDayChanged += HandleDayChanged;
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        DayNightCycleManager.OnDayChanged -= HandleDayChanged;
    }

    private void Start()
    {
        // Find all NPCs in the scene
        RegisterAllNPCs();
    }

    /// <summary>
    /// Finds and registers all NPCs in the scene
    /// Call this after spawning new NPCs or loading a new scene
    /// </summary>
    public void RegisterAllNPCs()
    {
        allNPCs.Clear();

        // Find all NPCManager components, including disabled ones
        NPCManager[] foundNPCs = FindObjectsOfType<NPCManager>(true);

        allNPCs.AddRange(foundNPCs);

        if (showDebugLogs)
        {
            Debug.Log($"NPCSleepManager: Registered {allNPCs.Count} NPCs");
        }
    }

    /// <summary>
    /// Manually register a single NPC (useful for spawned NPCs)
    /// </summary>
    public void RegisterNPC(NPCManager npc)
    {
        if (npc != null && !allNPCs.Contains(npc))
        {
            allNPCs.Add(npc);

            if (showDebugLogs)
            {
                Debug.Log($"NPCSleepManager: Registered {npc.npcName}");
            }
        }
    }

    /// <summary>
    /// Manually unregister an NPC (useful when destroying NPCs)
    /// </summary>
    public void UnregisterNPC(NPCManager npc)
    {
        if (allNPCs.Contains(npc))
        {
            allNPCs.Remove(npc);

            if (showDebugLogs)
            {
                Debug.Log($"NPCSleepManager: Unregistered {npc.npcName}");
            }
        }
    }

    /// <summary>
    /// Called when the day changes - wakes up all sleeping NPCs
    /// </summary>
    private void HandleDayChanged(DayNightCycleManager.DayOfWeek newDay)
    {
        if (showDebugLogs)
        {
            Debug.Log($"NPCSleepManager: Day changed to {newDay}, checking for sleeping NPCs...");
        }

        int wokeUpCount = 0;

        // Clean up null references (destroyed NPCs)
        allNPCs.RemoveAll(npc => npc == null);

        // Wake up all sleeping NPCs
        foreach (NPCManager npc in allNPCs)
        {
            if (npc != null && !npc.gameObject.activeSelf)
            {
                // NPC is disabled (sleeping), wake them up
                WakeUpNPC(npc, newDay);
                wokeUpCount++;
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"NPCSleepManager: Woke up {wokeUpCount} NPCs for {newDay}");
        }
    }

    /// <summary>
    /// Wakes up a specific NPC
    /// </summary>
    private void WakeUpNPC(NPCManager npc, DayNightCycleManager.DayOfWeek newDay)
    {
        if (npc == null) return;

        if (showDebugLogs)
        {
            Debug.Log($"NPCSleepManager: Waking up {npc.npcName} for {newDay}");
        }

        // Re-enable the NPC GameObject
        npc.gameObject.SetActive(true);

        // Manually notify the NPC of the day change (they missed the event while disabled)
        npc.NotifyDayChanged(newDay);
    }

    /// <summary>
    /// Debug method - wake all NPCs immediately
    /// </summary>
    [ContextMenu("Wake All NPCs Now")]
    public void WakeAllNPCsNow()
    {
        if (DayNightCycleManager.Instance != null)
        {
            HandleDayChanged(DayNightCycleManager.Instance.CurrentDayOfWeek);
        }
        else
        {
            Debug.LogWarning("NPCSleepManager: Cannot wake NPCs - DayNightCycleManager not found!");
        }
    }

    /// <summary>
    /// Debug method - list all registered NPCs
    /// </summary>
    [ContextMenu("List Registered NPCs")]
    public void ListRegisteredNPCs()
    {
        Debug.Log($"=== NPCSleepManager: {allNPCs.Count} Registered NPCs ===");

        foreach (NPCManager npc in allNPCs)
        {
            if (npc != null)
            {
                string status = npc.gameObject.activeSelf ? "AWAKE" : "SLEEPING";
                Debug.Log($"  - {npc.npcName} ({status})");
            }
            else
            {
                Debug.Log($"  - NULL (destroyed)");
            }
        }
    }
}