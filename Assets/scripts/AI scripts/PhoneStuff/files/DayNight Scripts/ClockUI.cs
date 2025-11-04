using UnityEngine;
using TMPro;

public class UICalendarClock : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI clockText;
    [SerializeField] private DayNightCycleManager timeManager;

    void Start()
    {
        if (!clockText)
            clockText = GetComponent<TextMeshProUGUI>();

        if (!timeManager)
            timeManager = FindObjectOfType<DayNightCycleManager>();
    }

    void Update()
    {
        if (timeManager == null) return;

        int hour = timeManager.GetHour();
        int minute = timeManager.GetMinute();

        string suffix = "AM";
        int displayHour = hour;

        if (hour >= 12)
        {
            suffix = "PM";
            if (hour > 12)
                displayHour = hour - 12;
        }
        else if (hour == 0)
        {
            displayHour = 12; // midnight
        }

        clockText.text = $"{displayHour:00}:{minute:00} {suffix}";
    }
}
