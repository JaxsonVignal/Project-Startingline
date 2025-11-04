using UnityEngine;
using System;

public class DayNightCycleManager : MonoBehaviour
{

    public static DayNightCycleManager Instance { get; private set; }
    [Header("Time Settings")]
    public float dayLengthInMinutes = 24f; // 1 in-game day = 24 real minutes
    public float startHour = 6f;           // Start at 6 AM

    [Header("References")]
    public Light directionalLight;         // Your Sun Light
    public Gradient lightColor;            // For changing sun color through the day
    public AnimationCurve lightIntensity;  // For controlling brightness curve

    // Current time (0–24)
    [Range(0, 24)] public float currentTimeOfDay;
    public static event Action<float> OnTimeChanged; // Broadcasts the current hour

    private float timeScale; // How fast time moves

    void Start()
    {
        timeScale = 24f / (dayLengthInMinutes * 60f); // 24 hours per chosen real-time length
        currentTimeOfDay = startHour;
    }

    void Update()
    {
        // Advance time
        currentTimeOfDay += Time.deltaTime * timeScale;
        if (currentTimeOfDay >= 24f)
            currentTimeOfDay -= 24f;

        UpdateLighting();
        OnTimeChanged?.Invoke(currentTimeOfDay); // Broadcast current time
    }

    

    private void Awake()
    {
        Instance = this;
    }

    void UpdateLighting()
    {
        if (directionalLight)
        {
            // Rotate sun based on time of day
            directionalLight.transform.localRotation =
                Quaternion.Euler((currentTimeOfDay / 24f) * 360f - 90f, 170f, 0);

            // Adjust color and intensity
            if (lightColor != null)
                directionalLight.color = lightColor.Evaluate(currentTimeOfDay / 24f);

            if (lightIntensity != null)
                directionalLight.intensity = lightIntensity.Evaluate(currentTimeOfDay / 24f);
        }
    }

    public void SetTime(float hour)
    {
        currentTimeOfDay = hour;      // Set your internal time variable
        UpdateLighting();        // Update sun, sky, etc.

        // Trigger event if you have one
        OnTimeChanged?.Invoke(currentTimeOfDay);
    }

    public int GetHour() => Mathf.FloorToInt(currentTimeOfDay);
    public int GetMinute() => Mathf.FloorToInt((currentTimeOfDay % 1f) * 60f);

    /// <summary>
    /// Convert real-time seconds to in-game hours based on your day/night cycle speed
    /// </summary>
    /// <param name="realTimeSeconds">Real-time in seconds</param>
    /// <returns>In-game hours</returns>
    public float GetGameTimeFromRealTime(float realTimeSeconds)
    {
        // Example: If your full day cycle is 20 minutes (1200 seconds) in real-time
        // Then 1200 real seconds = 24 game hours
        // So: gameHours = (realSeconds / secondsPerFullDay) * 24

        // ADJUST THIS VALUE based on your day/night cycle settings
        float secondsPerFullDay = 1200f; // Change this to match your actual day length

        float gameHours = (realTimeSeconds / secondsPerFullDay) * 24f;

        return gameHours;
    }

    /// <summary>
    /// Convert in-game hours to real-time seconds
    /// </summary>
    /// <param name="gameHours">In-game hours</param>
    /// <returns>Real-time seconds</returns>
    public float GetRealTimeFromGameTime(float gameHours)
    {
        // ADJUST THIS VALUE based on your day/night cycle settings
        float secondsPerFullDay = 1200f; // Change this to match your actual day length

        float realSeconds = (gameHours / 24f) * secondsPerFullDay;

        return realSeconds;
    }
}

