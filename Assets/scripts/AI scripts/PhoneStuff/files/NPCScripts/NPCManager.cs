using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator), typeof(CivilianMovementController))]
public class NPCManager : MonoBehaviour
{
    public enum NPCState { Sleeping, Eating, Working, Idle, GoingToMeeting }

    [Header("NPC Info")]
    public string npcName = "Civilian";
    public NPCState currentState;

    [Header("Schedule Settings")]
    public float wakeUpTime = 6f;
    public float workStartTime = 9f;
    public float workEndTime = 17f;
    public float sleepTime = 22f;
    public float breakStartTime = 12f;
    public float breakEndTime = 13f;
    public float fleeDuration = 20f; // how long the NPC runs away

    [Header("Waypoints")]
    public Transform bedLocation;
    public Transform workLocation;
    public Transform eatLocation;
    public Transform idleLocation;

    [Header("Weapon Deal Settings")]
    public float meetingWaitTime = 300f; // 5 minutes to wait at location

    private Animator animator;
    private CivilianMovementController movement;

    // Weapon deal tracking
    private bool hasScheduledMeeting = false;
    private Transform meetingLocation;
    private float meetingTime;
    private float arrivalTime; // When to start walking (1 hour before)
    private NPCState stateBeforeMeeting;

    private void OnEnable() => DayNightCycleManager.OnTimeChanged += HandleTimeUpdate;
    private void OnDisable() => DayNightCycleManager.OnTimeChanged -= HandleTimeUpdate;

    private void Start()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<CivilianMovementController>();
    }

    private void HandleTimeUpdate(float hour)
    {
        // Check if we need to go to meeting
        if (hasScheduledMeeting && hour >= arrivalTime && hour < meetingTime)
        {
            if (currentState != NPCState.GoingToMeeting)
            {
                GoToMeeting();
            }
        }
        // Check if meeting time has passed and we need to resume schedule
        else if (hasScheduledMeeting && hour >= meetingTime + (meetingWaitTime / 3600f))
        {
            CompleteMeeting();
        }
        // Normal schedule
        else if (!hasScheduledMeeting || currentState != NPCState.GoingToMeeting)
        {
            NPCState newState = DetermineState(hour);
            if (newState != currentState)
                SwitchState(newState);
        }
    }

    private NPCState DetermineState(float hour)
    {
        if (hour >= sleepTime || hour < wakeUpTime)
            return NPCState.Sleeping;
        else if (hour >= breakStartTime && hour < breakEndTime)
            return NPCState.Eating;
        else if (hour >= workStartTime && hour < workEndTime)
            return NPCState.Working;
        else
            return NPCState.Idle;
    }

    private void SwitchState(NPCState newState)
    {
        if (newState == currentState) return; // Prevent unnecessary MoveTo

        currentState = newState;
        Debug.Log($"{npcName} switched to {newState}");

        // Animation trigger
        if (animator) animator.SetTrigger(newState.ToString());

        // Movement target
        switch (newState)
        {
            case NPCState.Sleeping:
                movement?.MoveTo(bedLocation);
                break;
            case NPCState.Eating:
                movement?.MoveTo(eatLocation);
                break;
            case NPCState.Working:
                movement?.MoveTo(workLocation);
                break;
            case NPCState.Idle:
                movement?.MoveTo(idleLocation);
                break;
        }
    }

    /// <summary>
    /// Schedule a weapon deal meeting at a specific location and time
    /// </summary>
    /// <param name="location">Where to meet</param>
    /// <param name="pickupTime">Real-time seconds until meeting</param>
    public void ScheduleWeaponMeeting(Transform location, float pickupTime)
    {
        if (location == null)
        {
            Debug.LogWarning($"{npcName} received null meeting location!");
            return;
        }

        meetingLocation = location;

        // Convert real-time seconds to in-game hours
        float realTimeToGameTime = DayNightCycleManager.Instance.GetGameTimeFromRealTime(pickupTime);
        float currentGameTime = DayNightCycleManager.Instance.currentTimeOfDay;

        meetingTime = currentGameTime + realTimeToGameTime;

        // Wrap around if it goes past 24 hours
        if (meetingTime >= 24f)
            meetingTime -= 24f;

        // Set arrival time to 1 in-game hour before meeting
        arrivalTime = meetingTime - 1f;
        if (arrivalTime < 0f)
            arrivalTime += 24f;

        hasScheduledMeeting = true;
        stateBeforeMeeting = currentState;

        Debug.Log($"{npcName} scheduled meeting at {location.name}. Current time: {currentGameTime:F2}, Arrival: {arrivalTime:F2}, Meeting: {meetingTime:F2}");

        // If the arrival time is very soon or already passed, go immediately
        if (Mathf.Abs(currentGameTime - arrivalTime) < 0.1f ||
            (currentGameTime > arrivalTime && currentGameTime < meetingTime))
        {
            GoToMeeting();
        }
    }

    private void GoToMeeting()
    {
        if (currentState == NPCState.GoingToMeeting) return;

        currentState = NPCState.GoingToMeeting;

        Debug.Log($"{npcName} is now heading to meeting at {meetingLocation.name}");

        // Move to meeting location
        movement?.MoveTo(meetingLocation);

        // Set animation if you have one for walking/moving
        if (animator) animator.SetTrigger("Walking");
    }

    private void CompleteMeeting()
    {
        Debug.Log($"{npcName} completed meeting at {meetingLocation.name}");

        hasScheduledMeeting = false;
        meetingLocation = null;

        // Resume normal schedule
        float currentHour = DayNightCycleManager.Instance.currentTimeOfDay;
        NPCState newState = DetermineState(currentHour);
        SwitchState(newState);
    }

    /// <summary>
    /// Check if NPC is currently at the meeting location and waiting
    /// </summary>
    public bool IsAtMeetingLocation()
    {
        if (!hasScheduledMeeting || meetingLocation == null)
            return false;

        float distance = Vector3.Distance(transform.position, meetingLocation.position);
        return distance < 3f && currentState == NPCState.GoingToMeeting;
    }

    /// <summary>
    /// Called when player successfully delivers the weapon
    /// </summary>
    public void CompleteWeaponDeal()
    {
        if (hasScheduledMeeting)
        {
            Debug.Log($"{npcName} received weapon delivery!");
            CompleteMeeting();
        }
    }

    public void ResetToBed()
    {
        if (bedLocation == null)
        {
            Debug.LogWarning($"{npcName} has no bed assigned!");
            return;
        }

        // Cancel any scheduled meetings
        hasScheduledMeeting = false;

        // Stop NavMeshAgent and teleport
        if (movement != null)
        {
            movement.OverrideMovementTemporarily(bedLocation, 2f); // hold at bed for 2 seconds
        }
        else
        {
            transform.position = bedLocation.position;
        }

        currentState = NPCState.Sleeping;
        Debug.Log($"{npcName} has been reset to bed.");
    }

    public void RunAwayFromPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null || movement == null) return;

        Vector3 fleeDir = (transform.position - player.transform.position).normalized;
        Vector3 fleeTarget = transform.position + fleeDir * 10f;

        movement.MoveToPosition(fleeTarget);
        Debug.Log($"{name} is running away from the player!");

        // Start timer to resume schedule
        StopCoroutine("ResumeSchedule");
        StartCoroutine(ResumeSchedule());
    }

    private IEnumerator ResumeSchedule()
    {
        yield return new WaitForSeconds(fleeDuration);

        // Let the schedule system take over
        HandleTimeUpdate(DayNightCycleManager.Instance.currentTimeOfDay);
    }
}
