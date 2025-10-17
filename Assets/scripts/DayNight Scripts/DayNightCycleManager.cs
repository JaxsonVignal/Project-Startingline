using UnityEngine;
using System;

public class DayNightCycleManager : MonoBehaviour
{
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

    public int GetHour() => Mathf.FloorToInt(currentTimeOfDay);
    public int GetMinute() => Mathf.FloorToInt((currentTimeOfDay % 1f) * 60f);
}
