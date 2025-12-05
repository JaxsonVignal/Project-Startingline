using UnityEngine;
using System;

public class DayNightCycleManager : MonoBehaviour
{
    public static DayNightCycleManager Instance { get; private set; }

    [Header("Time Settings")]
    public float dayLengthInMinutes = 24f;
    public float startHour = 6f;

    [Header("References")]
    public Light directionalLight;
    public Gradient lightColor;
    public AnimationCurve lightIntensity;

    [Range(0, 24)] public float currentTimeOfDay;
    public static event Action<float> OnTimeChanged;

    private float timeScale;

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
        currentTimeOfDay += Time.deltaTime * timeScale;

        if (currentTimeOfDay >= 24f)
            currentTimeOfDay -= 24f;

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

    public void SetTime(float hour)
    {
        currentTimeOfDay = hour;
        UpdateLighting();
        OnTimeChanged?.Invoke(currentTimeOfDay);
    }

    public int GetHour() => Mathf.FloorToInt(currentTimeOfDay);
    public int GetMinute() => Mathf.FloorToInt((currentTimeOfDay % 1f) * 60f);

    /// <summary>
    /// Converts real-time seconds into in-game hours.
    /// </summary>
    public float GetGameTimeFromRealTime(float realTimeSeconds)
    {
        float secondsPerFullDay = 1200f; // YOUR VALUE HERE
        return (realTimeSeconds / secondsPerFullDay) * 24f;
    }

    public float GetRealTimeFromGameTime(float gameHours)
    {
        float secondsPerFullDay = 1200f; // YOUR VALUE HERE
        return (gameHours / 24f) * secondsPerFullDay;
    }

}
