using UnityEngine;

/// <summary>
/// Add this to any GameObject to test and visualize time conversion
/// Shows real-time to game-time conversion
/// </summary>
public class TimeConversionTester : MonoBehaviour
{
    [Header("Test Settings")]
    [Tooltip("Real-time seconds to convert")]
    public float testRealTimeSeconds = 600f; // 10 minutes default
    
    [Header("Results - Read Only")]
    public float currentGameTime;
    public string currentGameTimeFormatted;
    public float gameTimeFromRealTime;
    public float meetingTime;
    public string meetingTimeFormatted;
    public float arrivalTime;
    public string arrivalTimeFormatted;
    
    [Header("Day/Night Cycle Info")]
    public float secondsPerFullDay;
    public float currentTimeSpeed;
    
    private void Update()
    {
        if (DayNightCycleManager.Instance == null)
        {
            Debug.LogWarning("DayNightCycleManager.Instance is NULL!");
            return;
        }
        
        // Get current game time
        currentGameTime = DayNightCycleManager.Instance.currentTimeOfDay;
        currentGameTimeFormatted = FormatTime(currentGameTime);
        
        // Test conversion
        gameTimeFromRealTime = DayNightCycleManager.Instance.GetGameTimeFromRealTime(testRealTimeSeconds);
        
        // Calculate meeting and arrival times
        meetingTime = currentGameTime + gameTimeFromRealTime;
        if (meetingTime >= 24f) meetingTime -= 24f;
        meetingTimeFormatted = FormatTime(meetingTime);
        
        arrivalTime = meetingTime - 1f;
        if (arrivalTime < 0f) arrivalTime += 24f;
        arrivalTimeFormatted = FormatTime(arrivalTime);
        
        // Try to get day cycle info
        // You may need to adjust this based on your DayNightCycleManager implementation
        // secondsPerFullDay = ???
        // currentTimeSpeed = ???
    }
    
    private string FormatTime(float hours)
    {
        int h = Mathf.FloorToInt(hours);
        int m = Mathf.FloorToInt((hours - h) * 60f);
        return $"{h:00}:{m:00}";
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 400, 300));
        GUILayout.Box("=== TIME CONVERSION TEST ===");
        
        GUILayout.Label($"Current Game Time: {currentGameTimeFormatted} ({currentGameTime:F2}h)");
        GUILayout.Space(10);
        
        GUILayout.Label($"Testing: {testRealTimeSeconds} real seconds");
        GUILayout.Label($"Converts to: {gameTimeFromRealTime:F2} game hours");
        GUILayout.Space(10);
        
        GUILayout.Label($"If order placed NOW:");
        GUILayout.Label($"  Meeting Time: {meetingTimeFormatted}");
        GUILayout.Label($"  Arrival Time (1h early): {arrivalTimeFormatted}");
        GUILayout.Space(10);
        
        GUILayout.Label("=== EXPECTED BEHAVIOR ===");
        GUILayout.Label($"1. NPC should leave at {arrivalTimeFormatted}");
        GUILayout.Label($"2. NPC should arrive by {meetingTimeFormatted}");
        GUILayout.Label($"3. Player has until {meetingTimeFormatted} to deliver");
        
        GUILayout.EndArea();
    }
    
    [ContextMenu("Print Detailed Info")]
    public void PrintDetailedInfo()
    {
        Debug.Log("=== TIME CONVERSION DIAGNOSTIC ===");
        Debug.Log($"Current Game Time: {currentGameTimeFormatted}");
        Debug.Log($"Real Time Input: {testRealTimeSeconds} seconds");
        Debug.Log($"Converts To: {gameTimeFromRealTime:F2} game hours");
        Debug.Log($"Meeting Would Be At: {meetingTimeFormatted}");
        Debug.Log($"NPC Would Leave At: {arrivalTimeFormatted}");
        Debug.Log("================================");
    }
}
