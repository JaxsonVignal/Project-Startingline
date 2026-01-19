using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Animator), typeof(CivilianMovementController))]
public class NPCManager : MonoBehaviour
{
    public enum NPCState
    {
        Sleeping,
        Eating,
        Working,
        Idle,
        GoingToMeeting,
        Fleeing
    }

    // NEW: Schedule entry for a specific time and location
    [System.Serializable]
    public class ScheduleEntry
    {
        public float startTime;
        public float endTime;
        public NPCState state;
        public Transform location;

        public bool IsActiveAt(float hour)
        {
            if (startTime < endTime)
                return hour >= startTime && hour < endTime;
            return hour >= startTime || hour < endTime;
        }
    }

    // NEW: Daily schedule
    [System.Serializable]
    public class DailySchedule
    {
        public DayNightCycleManager.DayOfWeek dayOfWeek;
        public List<ScheduleEntry> scheduleEntries = new List<ScheduleEntry>();
    }

    [Header("NPC Info")]
    public string npcName = "Civilian";
    public NPCState currentState;

    [Header("Weekly Schedule")]
    public List<DailySchedule> weeklySchedule = new List<DailySchedule>();

    [Header("Fallback Locations (if schedule not set)")]
    public Transform bedLocation;
    public Transform workLocation;
    public Transform eatLocation;
    public Transform idleLocation;

    [Header("Fallback Times (if schedule not set)")]
    public float wakeUpTime = 6f;
    public float workStartTime = 9f;
    public float workEndTime = 17f;
    public float sleepTime = 22f;
    public float breakStartTime = 12f;
    public float breakEndTime = 13f;

    [Header("Weapon Deal Settings")]
    public float meetingWaitTime = 300f;
    public float playerNearbyRange = 5f;

    [Header("Flee Settings")]
    public float fleeDistance = 20f;
    public float fleeDuration = 10f;

    [Header("Gunshot Reaction Settings")]
    [Tooltip("If true, NPC will flee when hearing gunshots")]
    public bool fleeFromGunshots = true;
    [Tooltip("Minimum time between gunshot reactions (prevents spam)")]
    public float gunshotReactionCooldown = 2f;

    private Animator animator;
    private CivilianMovementController movement;

    private bool hasScheduledMeeting = false;
    private Transform meetingLocation;
    private float meetingTime;
    private float arrivalTime;
    private NPCState stateBeforeMeeting;

    private bool isFleeing = false;
    private float fleeEndTime;
    private float lastGunshotReactionTime = -999f;

    private DayNightCycleManager.DayOfWeek currentDayOfWeek;

    private void OnEnable()
    {
        DayNightCycleManager.OnTimeChanged += HandleTimeUpdate;
        DayNightCycleManager.OnDayChanged += HandleDayChanged; // NEW
    }

    private void OnDisable()
    {
        DayNightCycleManager.OnTimeChanged -= HandleTimeUpdate;
        DayNightCycleManager.OnDayChanged -= HandleDayChanged; // NEW
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<CivilianMovementController>();

        // Get current day of week
        if (DayNightCycleManager.Instance != null)
        {
            currentDayOfWeek = DayNightCycleManager.Instance.CurrentDayOfWeek;
        }
    }

    private void Update()
    {
        if (isFleeing && Time.time >= fleeEndTime)
        {
            isFleeing = false;

            float now = DayNightCycleManager.Instance != null ?
                DayNightCycleManager.Instance.currentTimeOfDay : 12f;
            SwitchState(DetermineState(now));
        }
    }

    // NEW: Handle day changes
    private void HandleDayChanged(DayNightCycleManager.DayOfWeek newDay)
    {
        currentDayOfWeek = newDay;
        Debug.Log($"{npcName}: Day changed to {newDay}");

        // Force re-evaluation of schedule
        if (DayNightCycleManager.Instance != null)
        {
            float currentTime = DayNightCycleManager.Instance.currentTimeOfDay;
            NPCState newState = DetermineState(currentTime);
            SwitchState(newState);
        }
    }

    private void HandleTimeUpdate(float hour)
    {
        if (isFleeing)
            return;

        if (hasScheduledMeeting)
        {
            if (currentState != NPCState.GoingToMeeting &&
                IsTimeBetween(hour, arrivalTime, meetingTime))
            {
                GoToMeeting();
                return;
            }

            float meetingEnd = meetingTime + (meetingWaitTime / 3600f);

            if (IsTimePast(hour, meetingEnd))
            {
                if (IsPlayerNearby())
                {
                    Debug.Log($"{npcName}: Meeting time expired but player is nearby, waiting...");
                    return;
                }

                Debug.Log($"{npcName}: Meeting time expired, leaving meeting location");
                CompleteMeeting();
                return;
            }
        }

        if (!hasScheduledMeeting || currentState != NPCState.GoingToMeeting)
        {
            NPCState newState = DetermineState(hour);
            if (newState != currentState)
                SwitchState(newState);
        }
    }

    private bool IsPlayerNearby()
    {
        GameObject player = FindPlayer();
        if (player == null)
            return false;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        return distance <= playerNearbyRange;
    }

    private bool IsTimeBetween(float now, float start, float end)
    {
        if (start < end)
            return now >= start && now < end;

        return now >= start || now < end;
    }

    private bool IsTimePast(float now, float target)
    {
        if (target >= 24f) target -= 24f;
        if (now >= target) return true;

        if (target < 2f && now > 22f)
            return true;

        return false;
    }

    // NEW: Determine state using weekly schedule or fallback
    private NPCState DetermineState(float hour)
    {
        // Try to find schedule for current day
        DailySchedule todaySchedule = GetScheduleForDay(currentDayOfWeek);

        if (todaySchedule != null && todaySchedule.scheduleEntries.Count > 0)
        {
            // Use schedule-based system
            foreach (var entry in todaySchedule.scheduleEntries)
            {
                if (entry.IsActiveAt(hour))
                {
                    return entry.state;
                }
            }
        }

        // Fallback to old time-based system
        if (hour >= sleepTime || hour < wakeUpTime)
            return NPCState.Sleeping;

        if (hour >= breakStartTime && hour < breakEndTime)
            return NPCState.Eating;

        if (hour >= workStartTime && hour < workEndTime)
            return NPCState.Working;

        return NPCState.Idle;
    }

    // NEW: Get location for current state
    private Transform GetLocationForState(NPCState state)
    {
        // Try to find from schedule
        DailySchedule todaySchedule = GetScheduleForDay(currentDayOfWeek);

        if (todaySchedule != null && todaySchedule.scheduleEntries.Count > 0)
        {
            float currentHour = DayNightCycleManager.Instance != null ?
                DayNightCycleManager.Instance.currentTimeOfDay : 12f;

            foreach (var entry in todaySchedule.scheduleEntries)
            {
                if (entry.IsActiveAt(currentHour) && entry.state == state && entry.location != null)
                {
                    return entry.location;
                }
            }
        }

        // Fallback to default locations
        switch (state)
        {
            case NPCState.Sleeping:
                return bedLocation;
            case NPCState.Eating:
                return eatLocation;
            case NPCState.Working:
                return workLocation;
            case NPCState.Idle:
                return idleLocation;
            default:
                return null;
        }
    }

    // NEW: Get schedule for specific day
    private DailySchedule GetScheduleForDay(DayNightCycleManager.DayOfWeek day)
    {
        foreach (var schedule in weeklySchedule)
        {
            if (schedule.dayOfWeek == day)
                return schedule;
        }
        return null;
    }

    private void SwitchState(NPCState newState)
    {
        if (newState == currentState)
            return;

        currentState = newState;

        animator?.SetTrigger(newState.ToString());

        Transform destination = GetLocationForState(newState);
        if (destination != null)
        {
            movement?.MoveTo(destination);
        }
    }

    // Meeting system methods remain the same
    public void ScheduleWeaponMeeting(Transform location, float pickupHours)
    {
        meetingLocation = location;
        hasScheduledMeeting = true;

        float now = DayNightCycleManager.Instance.currentTimeOfDay;

        meetingTime = now + pickupHours;
        if (meetingTime >= 24f)
            meetingTime -= 24f;

        arrivalTime = meetingTime - 1f;
        if (arrivalTime < 0f)
            arrivalTime += 24f;

        stateBeforeMeeting = currentState;

        Debug.Log($"{npcName} meeting: NOW {now:F2}, ARR {arrivalTime:F2}, MEET {meetingTime:F2}");

        if (IsTimeBetween(now, arrivalTime, meetingTime))
            GoToMeeting();
    }

    private void GoToMeeting()
    {
        if (currentState == NPCState.GoingToMeeting)
            return;

        currentState = NPCState.GoingToMeeting;

        movement?.MoveTo(meetingLocation);
        animator?.SetTrigger("Walking");

        Debug.Log($"{npcName} is now going to meeting location");
    }

    private void CompleteMeeting()
    {
        hasScheduledMeeting = false;
        meetingLocation = null;

        float now = DayNightCycleManager.Instance.currentTimeOfDay;
        SwitchState(DetermineState(now));

        Debug.Log($"{npcName} meeting completed, returning to schedule");
    }

    public void CompleteWeaponDeal()
    {
        Debug.Log($"{npcName} weapon deal completed!");

        if (hasScheduledMeeting)
            CompleteMeeting();
    }

    // Gunshot and flee methods remain the same
    public void OnGunshotHeard(Vector3 gunshotPosition)
    {
        if (!fleeFromGunshots)
        {
            Debug.Log($"{npcName} heard gunshot but is set to not flee");
            return;
        }

        if (Time.time - lastGunshotReactionTime < gunshotReactionCooldown)
        {
            Debug.Log($"{npcName} heard gunshot but is on cooldown");
            return;
        }

        if (isFleeing)
        {
            Debug.Log($"{npcName} heard gunshot but is already fleeing");
            return;
        }

        lastGunshotReactionTime = Time.time;

        Debug.Log($"{npcName} reacting to gunshot at {gunshotPosition}!");

        RunAwayFromPosition(gunshotPosition);
    }

    public void RunAwayFromPlayer()
    {
        if (isFleeing)
        {
            Debug.Log($"{npcName} is already fleeing!");
            return;
        }

        Debug.Log($"{npcName} RunAwayFromPlayer() called!");

        GameObject player = FindPlayer();

        if (player != null)
        {
            RunAwayFromPosition(player.transform.position);
        }
        else
        {
            Debug.LogError($"{npcName} couldn't find player to flee from!");
        }
    }

    public void RunAwayFromPosition(Vector3 dangerPosition)
    {
        if (isFleeing)
        {
            Debug.Log($"{npcName} is already fleeing!");
            return;
        }

        Debug.Log($"{npcName} fleeing from position {dangerPosition}!");

        isFleeing = true;
        fleeEndTime = Time.time + fleeDuration;
        currentState = NPCState.Fleeing;

        Vector3 directionAwayFromDanger = (transform.position - dangerPosition).normalized;
        Vector3 fleePosition = transform.position + directionAwayFromDanger * fleeDistance;

        if (Physics.Raycast(fleePosition + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
        {
            fleePosition = hit.point;
        }

        Debug.Log($"{npcName} fleeing to position: {fleePosition}");

        if (movement != null)
        {
            GameObject tempFleePoint = new GameObject("TempFleePoint_" + npcName);
            tempFleePoint.transform.position = fleePosition;
            movement.MoveTo(tempFleePoint.transform);
            Destroy(tempFleePoint, 2f);
        }
        else
        {
            Debug.LogWarning($"{npcName} has no movement controller!");
        }

        animator?.SetTrigger("Running");

        Debug.Log($"{npcName} is fleeing!");
    }

    private GameObject FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
            player = GameObject.Find("Player");

        if (player == null)
            player = GameObject.Find("FPSController");

        if (player == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                player = mainCam.transform.root.gameObject;
        }

        return player;
    }

    public void ResetToBed()
    {
        hasScheduledMeeting = false;
        meetingLocation = null;
        isFleeing = false;

        currentState = NPCState.Sleeping;

        Transform bed = GetLocationForState(NPCState.Sleeping);
        if (bed != null)
        {
            transform.position = bed.position;
            transform.rotation = bed.rotation;

            movement?.MoveTo(bed);
        }
        else
        {
            Debug.LogWarning($"{npcName} has no bed location assigned!");
        }

        animator?.SetTrigger("Sleeping");

        Debug.Log($"{npcName} has been reset to bed.");
    }
}