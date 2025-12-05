using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple test panel to verify the texting system is working
/// Add this to a Canvas and hook up the buttons
/// </summary>
public class TextingSystemTester : MonoBehaviour
{
    [Header("UI References")]
    public Button sendTestOrderButton;
    public Button forceNPCGoButton;
    public Text statusText;
    
    [Header("Test Settings")]
    public float testPickupTimeSeconds = 60f; // 1 minute for quick testing
    
    private void Start()
    {
        if (sendTestOrderButton != null)
            sendTestOrderButton.onClick.AddListener(SendTestOrder);
        
        if (forceNPCGoButton != null)
            forceNPCGoButton.onClick.AddListener(ForceNPCGo);
        
        UpdateStatus("Ready to test");
    }
    
    /// <summary>
    /// Send a test weapon order from a random NPC
    /// </summary>
    public void SendTestOrder()
    {
        if (TextingManager.Instance == null)
        {
            UpdateStatus("ERROR: TextingManager not found!");
            Debug.LogError("TextingManager.Instance is null!");
            return;
        }
        
        UpdateStatus("Sending test order...");
        TextingManager.Instance.SendRandomWeaponRequest();
        UpdateStatus("Order sent! Check phone.");
    }
    
    /// <summary>
    /// Force an NPC to go to their meeting immediately (for testing)
    /// </summary>
    public void ForceNPCGo()
    {
        NPCManager[] npcs = FindObjectsOfType<NPCManager>();
        
        if (npcs.Length == 0)
        {
            UpdateStatus("ERROR: No NPCs found!");
            return;
        }
        
        // Find an NPC with a scheduled meeting
        NPCManager npcWithMeeting = null;
        foreach (var npc in npcs)
        {
            // Use reflection to check if has scheduled meeting
            var field = typeof(NPCManager).GetField("hasScheduledMeeting", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null && (bool)field.GetValue(npc))
            {
                npcWithMeeting = npc;
                break;
            }
        }
        
        if (npcWithMeeting != null)
        {
            // Use reflection to call GoToMeeting
            var method = typeof(NPCManager).GetMethod("GoToMeeting", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method != null)
            {
                method.Invoke(npcWithMeeting, null);
                UpdateStatus($"Forced {npcWithMeeting.npcName} to go to meeting");
            }
        }
        else
        {
            UpdateStatus("No NPCs have scheduled meetings");
        }
    }
    
    /// <summary>
    /// Print diagnostic information
    /// </summary>
    [ContextMenu("Print System Status")]
    public void PrintSystemStatus()
    {
        Debug.Log("=== TEXTING SYSTEM STATUS ===");
        
        // Check TextingManager
        if (TextingManager.Instance == null)
        {
            Debug.LogError("❌ TextingManager.Instance is NULL!");
        }
        else
        {
            Debug.Log("✓ TextingManager found");
            
            var npcsField = typeof(TextingManager).GetField("allNPCs", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (npcsField != null)
            {
                var npcList = npcsField.GetValue(TextingManager.Instance) as System.Collections.IList;
                Debug.Log($"  NPCs registered: {npcList?.Count ?? 0}");
            }
        }
        
        // Check PhoneUI
        if (PhoneUI.Instance == null)
        {
            Debug.LogError("❌ PhoneUI.Instance is NULL!");
        }
        else
        {
            Debug.Log("✓ PhoneUI found");
        }
        
        // Check DayNightCycleManager
        if (DayNightCycleManager.Instance == null)
        {
            Debug.LogError("❌ DayNightCycleManager.Instance is NULL!");
        }
        else
        {
            Debug.Log("✓ DayNightCycleManager found");
            Debug.Log($"  Current time: {DayNightCycleManager.Instance.currentTimeOfDay:F2}");
            
            // Test time conversion
            float testSeconds = 60f;
            try
            {
                float gameHours = DayNightCycleManager.Instance.GetGameTimeFromRealTime(testSeconds);
                Debug.Log($"  Time conversion: 60 real seconds = {gameHours:F2} game hours");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Time conversion failed: {e.Message}");
            }
        }
        
        // Check NPCs
        NPCManager[] npcs = FindObjectsOfType<NPCManager>();
        Debug.Log($"✓ Found {npcs.Length} NPC(s) in scene");
        foreach (var npc in npcs)
        {
            Debug.Log($"  - {npc.npcName} (State: {npc.currentState})");
        }
        
        Debug.Log("============================");
    }
    
    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        
        Debug.Log($"[Tester] {message}");
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 200));
        GUILayout.Box("=== QUICK TEST ===");
        
        if (GUILayout.Button("Send Test Order"))
            SendTestOrder();
        
        if (GUILayout.Button("Force NPC Go Now"))
            ForceNPCGo();
        
        if (GUILayout.Button("Print System Status"))
            PrintSystemStatus();
        
        GUILayout.EndArea();
    }
}
