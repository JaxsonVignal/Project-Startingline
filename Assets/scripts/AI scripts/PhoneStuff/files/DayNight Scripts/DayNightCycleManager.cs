using UnityEngine;
using System;

public class DayNightCycleManager : MonoBehaviour
{
    public static DayNightCycleManager Instance { get; private set; }

    [Header("Time Settings")]
    public float dayLengthInMinutes = 24f;
    public float startHour = 6f;

    [Header("Midnight Pause Settings")]
    public bool pauseAtMidnight = true;
    public float midnightPauseTime = 0f; // 0 = midnight
    [Tooltip("How close to midnight triggers the pause (in hours)")]
    public float midnightThreshold = 0.1f; // Pauses when time is within 0.1 hours of midnight

    [Header("References")]
    public Light directionalLight;
    public Gradient lightColor;
    public AnimationCurve lightIntensity;

    [Range(0, 24)] public float currentTimeOfDay;

    public static event Action<float> OnTimeChanged;
    public static event Action OnMidnightReached; // Event when midnight is reached
    public static event Action OnPlayerSlept; // Event when player sleeps and time resumes

    private float timeScale;
    private bool isPausedAtMidnight = false;
    private bool hasReachedMidnightToday = false;
    private int currentDay = 0;

    // Public property to check if time is paused
    public bool IsPausedAtMidnight => isPausedAtMidnight;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        timeScale = 24f / (dayLengthInMinutes * 60f);
        currentTimeOfDay = startHour;
    }

    void Update()
    {
        // Don't update time if paused at midnight
        if (isPausedAtMidnight)
        {
            return;
        }

        currentTimeOfDay += Time.deltaTime * timeScale;

        // Check if we've reached midnight
        if (pauseAtMidnight && !hasReachedMidnightToday)
        {
            if (currentTimeOfDay >= (24f - midnightThreshold) || currentTimeOfDay <= midnightThreshold)
            {
                // Snap to exactly midnight
                currentTimeOfDay = midnightPauseTime;
                isPausedAtMidnight = true;
                hasReachedMidnightToday = true;

                Debug.Log("Midnight reached! Time paused. Player must sleep to continue.");
                OnMidnightReached?.Invoke();
            }
        }

        // Handle day rollover when NOT paused
        if (currentTimeOfDay >= 24f)
        {
            currentTimeOfDay -= 24f;
            currentDay++;
        }

        UpdateLighting();
        OnTimeChanged?.Invoke(currentTimeOfDay);
    }

    void UpdateLighting()
    {
        if (directionalLight)
        {
            directionalLight.transform.localRotation =
                Quaternion.Euler((currentTimeOfDay / 24f) * 360f - 90f, 170f, 0);
            if (lightColor != null)
                directionalLight.color = lightColor.Evaluate(currentTimeOfDay / 24f);
            if (lightIntensity != null)
                directionalLight.intensity = lightIntensity.Evaluate(currentTimeOfDay / 24f);
        }
    }

    /// <summary>
    /// Call this when the player sleeps to resume time and start a new day
    /// </summary>
    public void PlayerSlept(float wakeUpHour = 6f)
    {
        // Reset time to morning
        currentTimeOfDay = wakeUpHour;
        isPausedAtMidnight = false;
        hasReachedMidnightToday = false;
        currentDay++;

        Debug.Log($"Player slept! New day started. Current time: {wakeUpHour:F2}, Day: {currentDay}");

        UpdateLighting();
        OnTimeChanged?.Invoke(currentTimeOfDay);
        OnPlayerSlept?.Invoke();
    }

    /// <summary>
    /// Force resume time without sleeping (for debugging or special cases)
    /// </summary>
    public void ResumeTime()
    {
        if (isPausedAtMidnight)
        {
            isPausedAtMidnight = false;
            hasReachedMidnightToday = false;
            Debug.Log("Time manually resumed");
        }
    }

    /// <summary>
    /// Force pause time at current hour
    /// </summary>
    public void PauseTime()
    {
        isPausedAtMidnight = true;
        Debug.Log($"Time manually paused at {currentTimeOfDay:F2}");
    }

    /// <summary>
    /// Check if it's currently midnight and paused
    /// </summary>
    public bool IsCurrentlyMidnight()
    {
        return isPausedAtMidnight && Mathf.Approximately(currentTimeOfDay, midnightPauseTime);
    }

    public void SetTime(float hour)
    {
        currentTimeOfDay = hour;

        // Reset midnight pause if setting time away from midnight
        if (Mathf.Abs(hour - midnightPauseTime) > midnightThreshold)
        {
            isPausedAtMidnight = false;
            hasReachedMidnightToday = false;
        }

        UpdateLighting();
        OnTimeChanged?.Invoke(currentTimeOfDay);
    }

    public int GetHour() => Mathf.FloorToInt(currentTimeOfDay);
    public int GetMinute() => Mathf.FloorToInt((currentTimeOfDay % 1f) * 60f);
    public int GetCurrentDay() => currentDay;

    /// <summary>
    /// Converts real-time seconds into in-game hours.
    /// </summary>
    public float GetGameTimeFromRealTime(float realTimeSeconds)
    {
        float secondsPerFullDay = dayLengthInMinutes * 60f;
        return (realTimeSeconds / secondsPerFullDay) * 24f;
    }

    public float GetRealTimeFromGameTime(float gameHours)
    {
        float secondsPerFullDay = dayLengthInMinutes * 60f;
        return (gameHours / 24f) * secondsPerFullDay;
    }
}