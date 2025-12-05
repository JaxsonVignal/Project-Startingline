using UnityEngine;
using System.Collections;

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

    [Header("Waypoints")]
    public Transform bedLocation;
    public Transform workLocation;
    public Transform eatLocation;
    public Transform idleLocation;

    [Header("Weapon Deal Settings")]
    public float meetingWaitTime = 300f;

    [Header("Flee Settings")]
    public float fleeDistance = 20f;
    public float fleeDuration = 10f;

    private Animator animator;
    private CivilianMovementController movement;

    private bool hasScheduledMeeting = false;
    private Transform meetingLocation;
    private float meetingTime;
    private float arrivalTime;
    private NPCState stateBeforeMeeting;

    private bool isFleeing = false;
    private float fleeEndTime;

    private void OnEnable()
    {
        DayNightCycleManager.OnTimeChanged += HandleTimeUpdate;
    }

    private void OnDisable()
    {
        DayNightCycleManager.OnTimeChanged -= HandleTimeUpdate;
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<CivilianMovementController>();
    }

    private void Update()
    {
        // Check if flee duration has ended
        if (isFleeing && Time.time >= fleeEndTime)
        {
            isFleeing = false;

            // Return to normal schedule
            float now = DayNightCycleManager.Instance != null ?
                DayNightCycleManager.Instance.currentTimeOfDay : 12f;
            SwitchState(DetermineState(now));
        }
    }

    private void HandleTimeUpdate(float hour)
    {
        // Don't update schedule while fleeing
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

    private NPCState DetermineState(float hour)
    {
        if (hour >= sleepTime || hour < wakeUpTime)
            return NPCState.Sleeping;

        if (hour >= breakStartTime && hour < breakEndTime)
            return NPCState.Eating;

        if (hour >= workStartTime && hour < workEndTime)
            return NPCState.Working;

        return NPCState.Idle;
    }

    private void SwitchState(NPCState newState)
    {
        if (newState == currentState)
            return;

        currentState = newState;

        animator?.SetTrigger(newState.ToString());

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

    // ---------------------
    // MEETING SYSTEM
    // ---------------------
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
    }

    private void CompleteMeeting()
    {
        hasScheduledMeeting = false;
        meetingLocation = null;

        float now = DayNightCycleManager.Instance.currentTimeOfDay;
        SwitchState(DetermineState(now));
    }

    public void CompleteWeaponDeal()
    {
        if (hasScheduledMeeting)
            CompleteMeeting();
    }

    // ---------------------
    // FLEE SYSTEM
    // ---------------------
    public void RunAwayFromPlayer()
    {
        if (isFleeing)
        {
            Debug.Log($"{npcName} is already fleeing!");
            return; // Already fleeing
        }

        Debug.Log($"{npcName} RunAwayFromPlayer() called!");

        isFleeing = true;
        fleeEndTime = Time.time + fleeDuration;
        currentState = NPCState.Fleeing;

        // Find player - try multiple methods
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            // Try finding by name if tag doesn't work
            player = GameObject.Find("Player");
        }

        if (player == null)
        {
            // Try finding FPSController
            player = GameObject.Find("FPSController");
        }

        if (player == null)
        {
            // Last resort: find any camera and use its parent
            Camera mainCam = Camera.main;
            if (mainCam != null)
                player = mainCam.transform.root.gameObject;
        }

        if (player != null)
        {
            Debug.Log($"{npcName} found player at {player.transform.position}");

            // Calculate flee direction (away from player)
            Vector3 directionAwayFromPlayer = (transform.position - player.transform.position).normalized;
            Vector3 fleePosition = transform.position + directionAwayFromPlayer * fleeDistance;

            // Make sure the flee position is on the ground
            if (Physics.Raycast(fleePosition + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
            {
                fleePosition = hit.point;
            }

            Debug.Log($"{npcName} fleeing to position: {fleePosition}");

            // Move to flee position
            if (movement != null)
            {
                // Create a temporary transform for the flee location
                GameObject tempFleePoint = new GameObject("TempFleePoint_" + npcName);
                tempFleePoint.transform.position = fleePosition;
                movement.MoveTo(tempFleePoint.transform);
                Destroy(tempFleePoint, 2f); // Clean up after movement starts
            }
            else
            {
                Debug.LogWarning($"{npcName} has no movement controller!");
            }

            animator?.SetTrigger("Running");

            Debug.Log($"{npcName} is fleeing from player!");
        }
        else
        {
            Debug.LogError($"{npcName} couldn't find player to flee from!");
            isFleeing = false;
        }
    }

    // ---------------------
    // RESET SYSTEM
    // ---------------------
    public void ResetToBed()
    {
        // Cancel any ongoing activities
        hasScheduledMeeting = false;
        meetingLocation = null;
        isFleeing = false;

        // Force NPC to bed
        currentState = NPCState.Sleeping;

        if (bedLocation != null)
        {
            // Teleport to bed immediately
            transform.position = bedLocation.position;
            transform.rotation = bedLocation.rotation;

            // Update movement controller
            movement?.MoveTo(bedLocation);
        }
        else
        {
            Debug.LogWarning($"{npcName} has no bed location assigned!");
        }

        animator?.SetTrigger("Sleeping");

        Debug.Log($"{npcName} has been reset to bed.");
    }
}
