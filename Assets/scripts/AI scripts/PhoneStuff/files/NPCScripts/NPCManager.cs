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
        Walking,    // NEW: For transitioning between locations
        GoingToMeeting,
        Fleeing,
        Aggro,      // NEW
        Attack,     // NEW
        Patrol,     // NEW: For guards who patrol between waypoints
        Stunned     // NEW: For stun effects from weapons
    }

    // NEW: Schedule entry for a specific time and location
    [System.Serializable]
    public class ScheduleEntry
    {
        public float startTime;
        public float endTime;
        public NPCState state;
        public Transform location;

        [Header("Patrol Settings (only for Patrol state)")]
        public List<Transform> patrolPoints;
        public float patrolWaitTime = 2f;

        [Header("Work Settings (only for Working state)")]
        public List<Transform> workLocations;
        public float workRotationTime = 3600f; // Time in seconds at each work location (default: 1 hour)

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

    [Header("Combat Reaction Settings")]
    [Tooltip("If true, NPC will flee when taking damage (only if enableCombat is false)")]
    public bool fleeFromDamage = true;
    [Tooltip("If true, NPC will flee when hearing gunshots")]
    public bool fleeFromGunshots = true;
    [Tooltip("How far this NPC can hear gunshots (in units)")]
    public float gunshotHearingRange = 50f;
    [Tooltip("Minimum time between gunshot reactions (prevents spam)")]
    public float gunshotReactionCooldown = 2f;

    [Header("Combat Settings")]
    [Tooltip("Enable combat behavior for this NPC (like guards)")]
    public bool enableCombat = false;
    public Transform player;
    public float sightRange = 20f;
    public float attackRange = 10f;
    public float loseAggroDistance = 30f;

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

    // NEW: Combat-related variables
    private bool hasEnteredAggro = false;

    // NEW: Patrol-related variables
    private int patrolIndex = 0;
    private float patrolWaitTimer = 0f;
    private ScheduleEntry currentPatrolEntry = null;

    // NEW: Work rotation variables
    private int workLocationIndex = 0;
    private float workRotationTimer = 0f;
    private ScheduleEntry currentWorkEntry = null;

    // NEW: Stun tracking variables
    private bool isStunned = false;
    private NPCState stateBeforeStun;

    // NEW: Sleeping state tracking
    private bool isAsleep = false;
    private bool isGoingToBed = false;
    private Transform bedDestination = null;

    // NEW: General walking/transition tracking
    private bool isTransitioning = false;
    private NPCState targetState = NPCState.Idle;
    private Transform targetDestination = null;

    // NEW: Initialization tracking to prevent premature state updates
    private bool isInitialized = false;

    private void Awake()
    {
        // If NPC was disabled (sleeping) and is now being re-enabled, handle wake-up
        // This happens BEFORE OnEnable, so we can prepare for the day change event
    }

    private void OnEnable()
    {
        DayNightCycleManager.OnTimeChanged += HandleTimeUpdate;
        DayNightCycleManager.OnDayChanged += HandleDayChanged;

        // Wake-up logic is now handled by NotifyDayChanged() called from NPCSleepManager
        // OnEnable just subscribes to events
    }

    private void OnDisable()
    {
        DayNightCycleManager.OnTimeChanged -= HandleTimeUpdate;
        DayNightCycleManager.OnDayChanged -= HandleDayChanged;
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<CivilianMovementController>();

        // Explicitly initialize combat state
        hasEnteredAggro = false;

        Debug.Log($"{npcName} Start() - enableCombat: {enableCombat}, hasEnteredAggro: {hasEnteredAggro}");

        // Get current day of week and initialize state
        if (DayNightCycleManager.Instance != null)
        {
            currentDayOfWeek = DayNightCycleManager.Instance.CurrentDayOfWeek;

            // Initialize state based on current time
            float currentTime = DayNightCycleManager.Instance.currentTimeOfDay;
            NPCState initialState = DetermineState(currentTime);
            Debug.Log($"{npcName} initializing to state: {initialState} at time {currentTime}");
            SwitchState(initialState);
        }

        // Mark as initialized
        isInitialized = true;

        Debug.Log($"{npcName} Start() complete - currentState: {currentState}, hasEnteredAggro: {hasEnteredAggro}");
    }

    private void Update()
    {
        // Debug: Log if hasEnteredAggro changes
        if (enableCombat && hasEnteredAggro && Time.frameCount % 60 == 0) // Log once per second
        {
            Debug.Log($"{npcName} Update - IN COMBAT MODE - currentState: {currentState}");
        }

        // PRIORITY 0: Stunned state overrides everything
        if (isStunned || currentState == NPCState.Stunned)
        {
            // Don't process any other logic while stunned
            return;
        }

        // PRIORITY 0.5: Sleeping state - NPC is disabled
        if (isAsleep)
        {
            // Don't process any logic while asleep - NPC should be disabled
            return;
        }

        // Check if NPC is transitioning to a new state and has arrived
        if (isTransitioning && currentState == NPCState.Walking)
        {
            CheckTransitionArrival();
        }

        // Handle fleeing state
        if (isFleeing && Time.time >= fleeEndTime)
        {
            isFleeing = false;

            float now = DayNightCycleManager.Instance != null ?
                DayNightCycleManager.Instance.currentTimeOfDay : 12f;
            SwitchState(DetermineState(now));
        }

        // PRIORITY 1: Handle combat states (if enabled)
        if (enableCombat && hasEnteredAggro)
        {
            HandleCombatBehavior();
            return; // Don't process other states when in combat
        }

        // PRIORITY 2: Handle patrol state
        if (currentState == NPCState.Patrol)
        {
            HandlePatrol();

            // Check for player while patrolling (if combat enabled)
            if (enableCombat)
            {
                CheckForPlayer();
            }
        }
        // PRIORITY 3: Handle working state with multiple locations
        else if (currentState == NPCState.Working)
        {
            HandleWorkRotation();

            // Check for player while working (if combat enabled)
            if (enableCombat)
            {
                CheckForPlayer();
            }
        }
        else if (enableCombat)
        {
            // Check for player in other non-combat states
            CheckForPlayer();
        }
    }

    // NEW: Handle day changes
    private void HandleDayChanged(DayNightCycleManager.DayOfWeek newDay)
    {
        DayNightCycleManager.DayOfWeek previousDay = currentDayOfWeek;
        currentDayOfWeek = newDay;
        Debug.Log($"{npcName}: Day changed from {previousDay} to {newDay}");

        // If NPC was asleep, wake them up and spawn at first location of new day
        if (isAsleep)
        {
            WakeUpAndSpawnAtFirstLocation();
            return; // Don't process further, wake-up handles everything
        }

        // Day changed while NPC was awake - spawn at first location for new day
        Transform spawnLocation = GetFirstScheduledLocation();
        if (spawnLocation != null)
        {
            transform.position = spawnLocation.position;
            transform.rotation = spawnLocation.rotation;
            Debug.Log($"{npcName} day changed while awake, teleported to first location: {spawnLocation.name}");
        }

        // Determine and set state for current time (don't trigger walking transition)
        if (DayNightCycleManager.Instance != null)
        {
            float currentTime = DayNightCycleManager.Instance.currentTimeOfDay;
            NPCState newState = DetermineState(currentTime);

            // Set state directly without walking transition
            currentState = newState;
            Debug.Log($"{npcName} set to {newState} state for new day");
        }
    }

    /// <summary>
    /// Public method for external systems (like NPCSleepManager) to notify NPC of day change
    /// Used when NPC was disabled and missed the event
    /// </summary>
    public void NotifyDayChanged(DayNightCycleManager.DayOfWeek newDay)
    {
        Debug.Log($"{npcName}: NotifyDayChanged called with {newDay} (was {currentDayOfWeek})");

        // Clear sleep state
        isAsleep = false;
        isGoingToBed = false;
        bedDestination = null;

        // Update to new day
        currentDayOfWeek = newDay;

        // If NPC just woke up, spawn at first location
        Transform spawnLocation = GetFirstScheduledLocation();
        if (spawnLocation != null)
        {
            transform.position = spawnLocation.position;
            transform.rotation = spawnLocation.rotation;
            Debug.Log($"{npcName} spawned at first location for {newDay}: {spawnLocation.name}");
        }
        else
        {
            Debug.LogWarning($"{npcName} GetFirstScheduledLocation returned NULL for {newDay}!");
        }

        // Determine appropriate state for current time and set it DIRECTLY (no walking transition)
        if (DayNightCycleManager.Instance != null)
        {
            float currentTime = DayNightCycleManager.Instance.currentTimeOfDay;
            NPCState newState = DetermineState(currentTime);

            // Set state directly without triggering SwitchState (which would make them walk)
            currentState = newState;
            Debug.Log($"{npcName} starting day in {newState} state at {spawnLocation?.name}");
        }

        // Mark as initialized so HandleTimeUpdate can start processing
        isInitialized = true;
    }

    private void HandleTimeUpdate(float hour)
    {
        // Don't process time updates if asleep or not yet initialized
        if (isAsleep || !isInitialized)
        {
            Debug.Log($"{npcName} HandleTimeUpdate blocked - isAsleep: {isAsleep}, isInitialized: {isInitialized}");
            return;
        }

        Debug.Log($"{npcName} HandleTimeUpdate processing - hour: {hour}, currentState: {currentState}");

        // Don't override combat or fleeing states with time-based updates
        if (isFleeing || hasEnteredAggro)
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
        GameObject playerObj = FindPlayer();
        if (playerObj == null)
            return false;

        float distance = Vector3.Distance(transform.position, playerObj.transform.position);
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
        Debug.Log($"{npcName} SwitchState called: {currentState} -> {newState}\nStack trace: {System.Environment.StackTrace}");

        if (newState == currentState)
            return;

        // Don't interrupt combat states with walking transitions
        if (hasEnteredAggro && (currentState == NPCState.Aggro || currentState == NPCState.Attack))
        {
            Debug.Log($"{npcName} is in combat, ignoring state change to {newState}");
            return;
        }

        // NEW: Handle sleeping state - walk to bed first, then sleep when arrived
        if (newState == NPCState.Sleeping)
        {
            Transform bedLocation = GetLocationForState(NPCState.Sleeping);
            if (bedLocation != null)
            {
                // Set up transition to sleeping state
                isTransitioning = true;
                targetState = NPCState.Sleeping;
                targetDestination = bedLocation;

                // Enter walking state while traveling to bed
                currentState = NPCState.Walking;

                // Walk to bed location
                movement?.MoveTo(bedLocation);
                Debug.Log($"{npcName} entering Walking state, going to bed location");
            }
            else
            {
                // No bed location, just sleep immediately
                Debug.LogWarning($"{npcName} has no bed location, sleeping immediately");
                currentState = newState;
                GoToSleep();
            }
            return;
        }

        // NEW: Handle patrol state specially
        if (newState == NPCState.Patrol)
        {
            // Find the current patrol entry
            DailySchedule todaySchedule = GetScheduleForDay(currentDayOfWeek);

            if (todaySchedule != null && todaySchedule.scheduleEntries.Count > 0)
            {
                float currentHour = DayNightCycleManager.Instance != null ?
                    DayNightCycleManager.Instance.currentTimeOfDay : 12f;

                foreach (var entry in todaySchedule.scheduleEntries)
                {
                    if (entry.IsActiveAt(currentHour) && entry.state == NPCState.Patrol)
                    {
                        currentPatrolEntry = entry;

                        if (entry.patrolPoints != null && entry.patrolPoints.Count > 0)
                        {
                            patrolIndex = 0;
                            patrolWaitTimer = 0f;

                            // Enter Walking state first, then patrol when arrived
                            isTransitioning = true;
                            targetState = NPCState.Patrol;
                            targetDestination = entry.patrolPoints[patrolIndex];
                            currentState = NPCState.Walking;

                            movement?.MoveTo(entry.patrolPoints[patrolIndex]);
                            Debug.Log($"{npcName} entering Walking state, going to start patrol with {entry.patrolPoints.Count} points");
                        }
                        else
                        {
                            Debug.LogWarning($"{npcName} is in Patrol state but has no patrol points!");
                        }
                        return;
                    }
                }
            }

            Debug.LogWarning($"{npcName} switched to Patrol but couldn't find patrol entry in schedule!");
            return;
        }

        // NEW: Handle working state with multiple locations
        if (newState == NPCState.Working)
        {
            // Find the current work entry
            DailySchedule todaySchedule = GetScheduleForDay(currentDayOfWeek);

            if (todaySchedule != null && todaySchedule.scheduleEntries.Count > 0)
            {
                float currentHour = DayNightCycleManager.Instance != null ?
                    DayNightCycleManager.Instance.currentTimeOfDay : 12f;

                foreach (var entry in todaySchedule.scheduleEntries)
                {
                    if (entry.IsActiveAt(currentHour) && entry.state == NPCState.Working)
                    {
                        currentWorkEntry = entry;

                        if (entry.workLocations != null && entry.workLocations.Count > 0)
                        {
                            workLocationIndex = 0;
                            workRotationTimer = 0f;

                            // Enter Walking state first, then work when arrived
                            isTransitioning = true;
                            targetState = NPCState.Working;
                            targetDestination = entry.workLocations[workLocationIndex];
                            currentState = NPCState.Walking;

                            movement?.MoveTo(entry.workLocations[workLocationIndex]);
                            Debug.Log($"{npcName} entering Walking state, going to start work rotation with {entry.workLocations.Count} locations");
                        }
                        else if (entry.location != null)
                        {
                            // Fall back to single location if no work locations list
                            isTransitioning = true;
                            targetState = NPCState.Working;
                            targetDestination = entry.location;
                            currentState = NPCState.Walking;

                            movement?.MoveTo(entry.location);
                            Debug.Log($"{npcName} entering Walking state, going to work at single location");
                        }
                        else
                        {
                            Debug.LogWarning($"{npcName} is in Working state but has no work locations!");
                        }
                        return;
                    }
                }
            }

            // Fallback to default work location
            Transform workDestination = GetLocationForState(newState);
            if (workDestination != null)
            {
                isTransitioning = true;
                targetState = NPCState.Working;
                targetDestination = workDestination;
                currentState = NPCState.Walking;

                movement?.MoveTo(workDestination);
                Debug.Log($"{npcName} entering Walking state, going to default work location");
            }
            return;
        }

        // Handle normal states (Eating, Idle, etc.) - walk to location first
        Transform destination = GetLocationForState(newState);
        if (destination != null)
        {
            isTransitioning = true;
            targetState = newState;
            targetDestination = destination;
            currentState = NPCState.Walking;

            movement?.MoveTo(destination);
            Debug.Log($"{npcName} entering Walking state, going to {newState} location ({destination.name})");
        }
        else
        {
            // No location for this state - this is a problem, log warning and stay in current state
            Debug.LogWarning($"{npcName} tried to switch to {newState} but has no location assigned! Staying in {currentState}");
        }
    }

    // ===================================================================
    // WALKING/TRANSITION SYSTEM
    // ===================================================================

    /// <summary>
    /// Checks if NPC has arrived at their destination and switches to the target state
    /// </summary>
    private void CheckTransitionArrival()
    {
        // Don't complete transitions if in combat
        if (hasEnteredAggro)
        {
            Debug.Log($"{npcName} entered combat during transition, canceling transition");
            isTransitioning = false;
            targetDestination = null;
            return;
        }

        if (targetDestination == null)
        {
            Debug.LogWarning($"{npcName} is transitioning but targetDestination is null!");
            isTransitioning = false;
            currentState = targetState;

            // Special handling for sleeping state
            if (targetState == NPCState.Sleeping)
            {
                GoToSleep();
            }
            return;
        }

        float distance = Vector3.Distance(transform.position, targetDestination.position);

        // Arrived at destination (within 2.5 units)
        if (distance < 2.5f)
        {
            Debug.Log($"{npcName} arrived at {targetState} location");
            isTransitioning = false;
            targetDestination = null;

            // Switch to the target state
            currentState = targetState;

            // Special handling for sleeping state
            if (currentState == NPCState.Sleeping)
            {
                GoToSleep();
            }
        }
    }

    // ===================================================================
    // SLEEPING SYSTEM
    // ===================================================================

    /// <summary>
    /// Puts NPC to sleep - disables the gameobject until wake time
    /// </summary>
    private void GoToSleep()
    {
        Debug.Log($"{npcName} going to sleep - disabling until wake time");

        isAsleep = true;
        isGoingToBed = false;
        isInitialized = false; // Reset initialization flag

        // Stop all movement
        if (movement != null)
        {
            movement.StopMovement();
        }

        // Disable the entire NPC gameobject
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Wakes up NPC and spawns them at the first scheduled location for the new day
    /// </summary>
    private void WakeUpAndSpawnAtFirstLocation()
    {
        // UPDATE: Get the CURRENT day from the manager
        if (DayNightCycleManager.Instance != null)
        {
            currentDayOfWeek = DayNightCycleManager.Instance.CurrentDayOfWeek;
        }

        Debug.Log($"{npcName} waking up for new day: {currentDayOfWeek}");

        isAsleep = false;

        // Get the first scheduled location for today
        Transform spawnLocation = GetFirstScheduledLocation();

        if (spawnLocation != null)
        {
            // Teleport to first location
            transform.position = spawnLocation.position;
            transform.rotation = spawnLocation.rotation;
            Debug.Log($"{npcName} spawning at first scheduled location: {spawnLocation.name}");
        }
        else
        {
            Debug.LogWarning($"{npcName} has no first scheduled location for {currentDayOfWeek}, using current time-based state");
        }

        // Re-enable the NPC gameobject (if it's not already enabled)
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Gets the first scheduled location for the current day
    /// Returns the location from the earliest schedule entry, or the location for the current state
    /// </summary>
    private Transform GetFirstScheduledLocation()
    {
        Debug.Log($"{npcName} GetFirstScheduledLocation() for {currentDayOfWeek}");

        DailySchedule todaySchedule = GetScheduleForDay(currentDayOfWeek);

        if (todaySchedule != null && todaySchedule.scheduleEntries.Count > 0)
        {
            Debug.Log($"{npcName} found schedule for {currentDayOfWeek} with {todaySchedule.scheduleEntries.Count} entries");

            // Find the earliest schedule entry (lowest startTime)
            ScheduleEntry earliestEntry = null;
            float earliestTime = 24f;

            foreach (var entry in todaySchedule.scheduleEntries)
            {
                Debug.Log($"{npcName} checking entry: {entry.state} at {entry.startTime}");
                if (entry.startTime < earliestTime)
                {
                    earliestTime = entry.startTime;
                    earliestEntry = entry;
                }
            }

            if (earliestEntry != null)
            {
                Debug.Log($"{npcName} earliest entry is {earliestEntry.state} at {earliestTime}");

                // For patrol state, use first patrol point
                if (earliestEntry.state == NPCState.Patrol &&
                    earliestEntry.patrolPoints != null &&
                    earliestEntry.patrolPoints.Count > 0)
                {
                    Debug.Log($"{npcName} returning first patrol point: {earliestEntry.patrolPoints[0].name}");
                    return earliestEntry.patrolPoints[0];
                }

                // For working state with multiple locations, use first work location
                if (earliestEntry.state == NPCState.Working &&
                    earliestEntry.workLocations != null &&
                    earliestEntry.workLocations.Count > 0)
                {
                    Debug.Log($"{npcName} returning first work location: {earliestEntry.workLocations[0].name}");
                    return earliestEntry.workLocations[0];
                }

                // Otherwise use the entry's location
                if (earliestEntry.location != null)
                {
                    Debug.Log($"{npcName} returning entry location: {earliestEntry.location.name}");
                    return earliestEntry.location;
                }
                else
                {
                    Debug.LogWarning($"{npcName} earliest entry {earliestEntry.state} has no location assigned!");
                }
            }
        }
        else
        {
            Debug.LogWarning($"{npcName} no schedule found for {currentDayOfWeek}");
        }

        // Fallback: determine current state and get its location
        if (DayNightCycleManager.Instance != null)
        {
            float currentTime = DayNightCycleManager.Instance.currentTimeOfDay;
            NPCState currentStateForTime = DetermineState(currentTime);
            Debug.Log($"{npcName} using fallback - current time {currentTime}, determined state: {currentStateForTime}");
            Transform fallbackLocation = GetLocationForState(currentStateForTime);

            if (fallbackLocation != null)
            {
                Debug.Log($"{npcName} returning fallback location: {fallbackLocation.name}");
            }
            else
            {
                Debug.LogWarning($"{npcName} fallback location is null for state {currentStateForTime}");
            }

            return fallbackLocation;
        }

        // Ultimate fallback - bed location
        Debug.LogWarning($"{npcName} returning bed location as ultimate fallback");
        return bedLocation;
    }

    // ===================================================================
    // PATROL SYSTEM
    // ===================================================================

    private void HandlePatrol()
    {
        // Don't patrol if in combat
        if (hasEnteredAggro)
            return;

        if (currentPatrolEntry == null ||
            currentPatrolEntry.patrolPoints == null ||
            currentPatrolEntry.patrolPoints.Count == 0)
        {
            return;
        }

        Transform target = currentPatrolEntry.patrolPoints[patrolIndex];
        if (target == null)
        {
            Debug.LogWarning($"{npcName} patrol point {patrolIndex} is null!");
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance < 1f)
        {
            patrolWaitTimer += Time.deltaTime;

            if (patrolWaitTimer >= currentPatrolEntry.patrolWaitTime)
            {
                patrolIndex = (patrolIndex + 1) % currentPatrolEntry.patrolPoints.Count;
                patrolWaitTimer = 0f;

                if (currentPatrolEntry.patrolPoints[patrolIndex] != null)
                {
                    movement?.MoveTo(currentPatrolEntry.patrolPoints[patrolIndex]);
                    Debug.Log($"{npcName} moving to patrol point {patrolIndex}");
                }
            }
        }
    }

    // ===================================================================
    // WORK ROTATION SYSTEM
    // ===================================================================

    private void HandleWorkRotation()
    {
        // Don't rotate work locations if in combat
        if (hasEnteredAggro)
            return;

        if (currentWorkEntry == null ||
            currentWorkEntry.workLocations == null ||
            currentWorkEntry.workLocations.Count == 0)
        {
            return;
        }

        Transform target = currentWorkEntry.workLocations[workLocationIndex];
        if (target == null)
        {
            Debug.LogWarning($"{npcName} work location {workLocationIndex} is null!");
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        // If at the work location (increased tolerance to 2.5f), start the timer
        if (distance < 2.5f)
        {
            workRotationTimer += Time.deltaTime;

            // Debug log every 10 seconds to track progress
            if (Mathf.FloorToInt(workRotationTimer) % 10 == 0 && workRotationTimer > 0)
            {
                Debug.Log($"{npcName} at work location {workLocationIndex} for {workRotationTimer:F0}s / {currentWorkEntry.workRotationTime}s");
            }

            // Time to move to next work location
            if (workRotationTimer >= currentWorkEntry.workRotationTime)
            {
                workLocationIndex = (workLocationIndex + 1) % currentWorkEntry.workLocations.Count;
                workRotationTimer = 0f;

                if (currentWorkEntry.workLocations[workLocationIndex] != null)
                {
                    movement?.MoveTo(currentWorkEntry.workLocations[workLocationIndex]);
                    Debug.Log($"{npcName} rotating to work location {workLocationIndex} ({currentWorkEntry.workLocations[workLocationIndex].name})");
                }
            }
        }
        else
        {
            // Still traveling to location
            if (workRotationTimer > 0)
            {
                Debug.Log($"{npcName} moved away from work location, resetting timer. Distance: {distance:F2}");
                workRotationTimer = 0f; // Reset timer if they move away
            }
        }
    }

    // ===================================================================
    // COMBAT SYSTEM (from GuardManager)
    // ===================================================================

    // NEW: Player detection for combat-enabled NPCs
    private void CheckForPlayer()
    {
        // Passive check - only used in non-combat states
        // NPC enters combat only when damaged (via OnDamaged())
    }

    // NEW: Handle combat behavior (aggro and attack)
    private void HandleCombatBehavior()
    {
        if (!player) return;

        // Clear any walking transitions when in combat
        if (isTransitioning)
        {
            isTransitioning = false;
            targetDestination = null;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        bool canSeePlayer = CanSeePlayer();

        // Player escaped too far - return to schedule
        if (distance > loseAggroDistance)
        {
            Debug.Log($"{npcName} lost player, returning to routine.");
            hasEnteredAggro = false;
            movement.StopMovement();

            if (DayNightCycleManager.Instance != null)
            {
                // Determine what state to return to based on current time
                float currentTime = DayNightCycleManager.Instance.currentTimeOfDay;
                NPCState returnState = DetermineState(currentTime);
                SwitchState(returnState);
            }
            return;
        }

        // Can see player AND within attack range - ATTACK
        if (canSeePlayer && distance <= attackRange)
        {
            if (currentState != NPCState.Attack)
            {
                currentState = NPCState.Attack;
                animator.SetTrigger("Attack");
                Debug.Log($"{npcName} ATTACKING player!");
            }

            movement.StopMovement();
            FaceTarget(player.position);
            return;
        }

        // Lost LOS or out of range - CHASE
        if (currentState != NPCState.Aggro)
        {
            currentState = NPCState.Aggro;
            Debug.Log($"{npcName} chasing player!");
        }

        movement.MoveTo(player, true);
        FaceTarget(player.position);
    }

    // NEW: Line of sight check
    private bool CanSeePlayer()
    {
        if (!player) return false;

        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 playerPos = player.position + Vector3.up * 1.5f;
        Vector3 directionToPlayer = (playerPos - origin).normalized;
        float distanceToPlayer = Vector3.Distance(origin, playerPos);

        // Raycast without layer mask - simple obstacle check
        if (Physics.Raycast(origin, directionToPlayer, distanceToPlayer))
        {
            // Something is blocking - do a more precise check
            if (Physics.Raycast(origin, directionToPlayer, out RaycastHit hit, distanceToPlayer))
            {
                return hit.transform == player.transform;
            }
            return false;
        }

        return true;
    }

    // NEW: Face target for combat
    private void FaceTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 5f);
        }
    }

    // NEW: Called when NPC takes damage - enters aggro state or flees
    public void OnDamaged()
    {
        // If combat is enabled, NPC will fight back
        if (enableCombat)
        {
            Debug.Log($"{npcName} OnDamaged called! Current state: {currentState}, hasEnteredAggro: {hasEnteredAggro}");

            if (!hasEnteredAggro)
            {
                hasEnteredAggro = true;

                // Clear any walking transitions
                isTransitioning = false;
                targetDestination = null;

                // Store previous state for debugging
                NPCState previousState = currentState;
                currentState = NPCState.Aggro;

                // Stop any current movement
                movement?.StopMovement();

                Debug.Log($"{npcName} entered AGGRO state! (was: {previousState}, now: {currentState})");
            }
            else
            {
                Debug.Log($"{npcName} already in aggro, current state: {currentState}");
            }
            return;
        }

        // If combat is disabled and fleeFromDamage is enabled, NPC will flee
        if (fleeFromDamage)
        {
            Debug.Log($"{npcName} was damaged but combat is not enabled, fleeing...");
            RunAwayFromPlayer();
        }
        else
        {
            Debug.Log($"{npcName} was damaged but fleeFromDamage is disabled, ignoring damage");
        }
    }

    // ===================================================================
    // STUN SYSTEM
    // ===================================================================

    /// <summary>
    /// Called by StunEffect when NPC is stunned
    /// Enters Stunned state and stops all movement/shooting
    /// </summary>
    public void EnterStunnedState()
    {
        if (isStunned)
        {
            Debug.Log($"{npcName} is already stunned");
            return;
        }

        Debug.Log($"{npcName} entering STUNNED state (was: {currentState})");

        // Store current state to return to later
        stateBeforeStun = currentState;

        // Enter stunned state
        isStunned = true;
        currentState = NPCState.Stunned;

        // Stop all movement
        if (movement != null)
        {
            movement.StopMovement();
        }

        // Trigger stunned animation if available
        if (animator != null)
        {
            animator.SetTrigger("Stunned");
        }
    }

    /// <summary>
    /// Called by StunEffect when stun duration ends
    /// Returns NPC to their previous state
    /// </summary>
    public void ExitStunnedState()
    {
        if (!isStunned)
        {
            Debug.Log($"{npcName} was not stunned");
            return;
        }

        Debug.Log($"{npcName} exiting STUNNED state (returning to: {stateBeforeStun})");

        isStunned = false;

        // Return to previous state
        if (DayNightCycleManager.Instance != null)
        {
            // Check if we should still be in that state, or if time has moved on
            float currentTime = DayNightCycleManager.Instance.currentTimeOfDay;
            NPCState timeBasedState = DetermineState(currentTime);

            // If in combat before stun and combat is still relevant, stay in combat
            if ((stateBeforeStun == NPCState.Aggro || stateBeforeStun == NPCState.Attack) && hasEnteredAggro)
            {
                currentState = stateBeforeStun;
                Debug.Log($"{npcName} returning to combat state: {stateBeforeStun}");
            }
            // If fleeing before stun and still in flee time window
            else if (stateBeforeStun == NPCState.Fleeing && isFleeing)
            {
                currentState = NPCState.Fleeing;
                Debug.Log($"{npcName} returning to fleeing state");
            }
            // Otherwise, use time-based state (in case time has changed significantly)
            else
            {
                SwitchState(timeBasedState);
                Debug.Log($"{npcName} returning to time-based state: {timeBasedState}");
            }
        }
        else
        {
            // No time manager - just return to previous state
            currentState = stateBeforeStun;
        }
    }

    // ===================================================================
    // STATIC GUNSHOT NOTIFICATION SYSTEM
    // ===================================================================

    /// <summary>
    /// Call this static method from weapon scripts to notify all NPCs of a gunshot.
    /// Each NPC will individually decide if they can hear it and how to react.
    /// </summary>
    /// <param name="gunshotPosition">World position where the gunshot occurred</param>
    public static void NotifyGunshotFired(Vector3 gunshotPosition)
    {
        // Find all NPCs in the scene
        NPCManager[] allNPCs = FindObjectsOfType<NPCManager>();

        int reactedCount = 0;
        foreach (NPCManager npc in allNPCs)
        {
            if (npc != null && npc.CanHearGunshot(gunshotPosition))
            {
                npc.OnGunshotHeard(gunshotPosition);
                reactedCount++;
            }
        }

        Debug.Log($"Gunshot fired at {gunshotPosition}. {reactedCount}/{allNPCs.Length} NPCs reacted.");
    }

    /// <summary>
    /// Check if this NPC can hear a gunshot at the given position
    /// </summary>
    private bool CanHearGunshot(Vector3 gunshotPosition)
    {
        if (!fleeFromGunshots)
            return false;

        float distance = Vector3.Distance(transform.position, gunshotPosition);
        return distance <= gunshotHearingRange;
    }

    // ===================================================================
    // MEETING SYSTEM
    // ===================================================================

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

    // ===================================================================
    // FLEE SYSTEM
    // ===================================================================

    /// <summary>
    /// Called by GunshotDetectionSystem when a gunshot is heard nearby.
    /// NPC will flee if fleeFromGunshots is enabled and not in combat.
    /// </summary>
    /// <param name="gunshotPosition">World position where the gunshot occurred</param>
    public void OnGunshotHeard(Vector3 gunshotPosition)
    {
        // Check if NPC should react to gunshots
        if (!fleeFromGunshots)
        {
            Debug.Log($"{npcName} heard gunshot but fleeFromGunshots is disabled");
            return;
        }

        // Check cooldown to prevent spam
        if (Time.time - lastGunshotReactionTime < gunshotReactionCooldown)
        {
            Debug.Log($"{npcName} heard gunshot but is on cooldown");
            return;
        }

        // Don't interrupt combat
        if (hasEnteredAggro)
        {
            Debug.Log($"{npcName} heard gunshot but is in combat");
            return;
        }

        // Don't flee if already fleeing
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

        GameObject playerObj = FindPlayer();

        if (playerObj != null)
        {
            RunAwayFromPosition(playerObj.transform.position);
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

    // ===================================================================
    // UTILITY METHODS
    // ===================================================================

    private GameObject FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj == null)
            playerObj = GameObject.Find("Player");

        if (playerObj == null)
            playerObj = GameObject.Find("FPSController");

        if (playerObj == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                playerObj = mainCam.transform.root.gameObject;
        }

        return playerObj;
    }

    public void ResetToBed()
    {
        hasScheduledMeeting = false;
        meetingLocation = null;
        isFleeing = false;
        hasEnteredAggro = false;
        isAsleep = false;
        isGoingToBed = false;
        bedDestination = null;
        isTransitioning = false; // NEW: Reset transition flag
        targetState = NPCState.Idle; // NEW: Reset target state
        targetDestination = null; // NEW: Reset target destination

        // Make sure NPC is enabled
        gameObject.SetActive(true);

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

        Debug.Log($"{npcName} has been reset to bed.");
    }
}