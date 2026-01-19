using UnityEngine;
using System;

public class DayNightCycleManager : MonoBehaviour
{
    public static DayNightCycleManager Instance { get; private set; }

    // Day of week enum
    public enum DayOfWeek
    {
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
        Sunday
    }

    [Header("Time Settings")]
    public float dayLengthInMinutes = 24f;
    public float startHour = 6f;
    public DayOfWeek startingDay = DayOfWeek.Monday; // NEW

    [Header("Midnight Pause Settings")]
    public bool pauseAtMidnight = true;
    public float midnightPauseTime = 0f;
    [Tooltip("How close to midnight triggers the pause (in hours)")]
    public float midnightThreshold = 0.1f;

    [Header("References")]
    public Light directionalLight;
    public Gradient lightColor;
    public AnimationCurve lightIntensity;

    [Range(0, 24)] public float currentTimeOfDay;

    public static event Action<float> OnTimeChanged;
    public static event Action OnMidnightReached;
    public static event Action OnPlayerSlept;
    public static event Action<DayOfWeek> OnDayChanged; // NEW

    private float timeScale;
    private bool isPausedAtMidnight = false;
    private bool hasReachedMidnightToday = false;
    private int currentDay = 0;
    private DayOfWeek currentDayOfWeek; // NEW

    public bool IsPausedAtMidnight => isPausedAtMidnight;
    public DayOfWeek CurrentDayOfWeek => currentDayOfWeek; // NEW

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
        currentDayOfWeek = startingDay; // NEW
    }

    void Update()
    {
        if (isPausedAtMidnight)
        {
            return;
        }

        currentTimeOfDay += Time.deltaTime * timeScale;

        if (pauseAtMidnight && !hasReachedMidnightToday)
        {
            if (currentTimeOfDay >= (24f - midnightThreshold) || currentTimeOfDay <= midnightThreshold)
            {
                currentTimeOfDay = midnightPauseTime;
                isPausedAtMidnight = true;
                hasReachedMidnightToday = true;

                Debug.Log("Midnight reached! Time paused. Player must sleep to continue.");
                OnMidnightReached?.Invoke();
            }
        }

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

    public void PlayerSlept(float wakeUpHour = 6f)
    {
        currentTimeOfDay = wakeUpHour;
        isPausedAtMidnight = false;
        hasReachedMidnightToday = false;
        currentDay++;

        // Advance to next day of week
        AdvanceDayOfWeek(); // NEW

        Debug.Log($"Player slept! New day started. Current time: {wakeUpHour:F2}, Day: {currentDay}, {currentDayOfWeek}");

        UpdateLighting();
        OnTimeChanged?.Invoke(currentTimeOfDay);
        OnPlayerSlept?.Invoke();
    }

    // NEW: Advance to next day of week
    private void AdvanceDayOfWeek()
    {
        DayOfWeek previousDay = currentDayOfWeek;
        currentDayOfWeek = (DayOfWeek)(((int)currentDayOfWeek + 1) % 7);

        Debug.Log($"Day changed from {previousDay} to {currentDayOfWeek}");
        OnDayChanged?.Invoke(currentDayOfWeek);
    }

    // NEW: Set specific day of week
    public void SetDayOfWeek(DayOfWeek day)
    {
        DayOfWeek previousDay = currentDayOfWeek;
        currentDayOfWeek = day;

        Debug.Log($"Day manually set from {previousDay} to {currentDayOfWeek}");
        OnDayChanged?.Invoke(currentDayOfWeek);
    }

    public void ResumeTime()
    {
        if (isPausedAtMidnight)
        {
            isPausedAtMidnight = false;
            hasReachedMidnightToday = false;
            Debug.Log("Time manually resumed");
        }
    }

    public void PauseTime()
    {
        isPausedAtMidnight = true;
        Debug.Log($"Time manually paused at {currentTimeOfDay:F2}");
    }

    public bool IsCurrentlyMidnight()
    {
        return isPausedAtMidnight && Mathf.Approximately(currentTimeOfDay, midnightPauseTime);
    }

    public void SetTime(float hour)
    {
        currentTimeOfDay = hour;

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