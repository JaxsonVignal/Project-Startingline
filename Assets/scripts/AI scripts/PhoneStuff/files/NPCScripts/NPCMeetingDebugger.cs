using UnityEngine;

/// <summary>
/// Attach this to an NPC to debug meeting scheduling issues
/// Shows current time, meeting times, and state info
/// </summary>
public class NPCMeetingDebugger : MonoBehaviour
{
    private NPCManager npcManager;
    
    [Header("Debug Info - READ ONLY")]
    public string currentGameTime;
    public string currentState;
    public bool hasScheduledMeeting;
    public string arrivalTime;
    public string meetingTime;
    public string meetingLocation;
    
    private void Start()
    {
        npcManager = GetComponent<NPCManager>();
    }
    
    private void Update()
    {
        if (npcManager == null || DayNightCycleManager.Instance == null)
            return;
        
        // Update debug display
        float currentTime = DayNightCycleManager.Instance.currentTimeOfDay;
        currentGameTime = FormatTime(currentTime);
        currentState = npcManager.currentState.ToString();
        
        // Use reflection to access private fields (for debugging only)
        var type = typeof(NPCManager);
        
        var hasScheduledField = type.GetField("hasScheduledMeeting", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (hasScheduledField != null)
            hasScheduledMeeting = (bool)hasScheduledField.GetValue(npcManager);
        
        var arrivalTimeField = type.GetField("arrivalTime", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (arrivalTimeField != null)
        {
            float arrival = (float)arrivalTimeField.GetValue(npcManager);
            arrivalTime = FormatTime(arrival);
        }
        
        var meetingTimeField = type.GetField("meetingTime", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (meetingTimeField != null)
        {
            float meeting = (float)meetingTimeField.GetValue(npcManager);
            meetingTime = FormatTime(meeting);
        }
        
        var meetingLocationField = type.GetField("meetingLocation", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (meetingLocationField != null)
        {
            Transform loc = (Transform)meetingLocationField.GetValue(npcManager);
            meetingLocation = loc != null ? loc.name : "None";
        }
    }
    
    private string FormatTime(float hours)
    {
        int h = Mathf.FloorToInt(hours);
        int m = Mathf.FloorToInt((hours - h) * 60f);
        return $"{h:00}:{m:00}";
    }
    
    private void OnGUI()
    {
        if (!hasScheduledMeeting)
            return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Box($"NPC: {npcManager.npcName}");
        GUILayout.Label($"Current Time: {currentGameTime}");
        GUILayout.Label($"State: {currentState}");
        GUILayout.Label($"Arrival Time: {arrivalTime}");
        GUILayout.Label($"Meeting Time: {meetingTime}");
        GUILayout.Label($"Location: {meetingLocation}");
        GUILayout.EndArea();
    }
}
