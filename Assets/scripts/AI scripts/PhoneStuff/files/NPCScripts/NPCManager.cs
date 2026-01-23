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

    // NEW: Stun tracking variables
    private bool isStunned = false;
    private NPCState stateBeforeStun;

    private void OnEnable()
    {
        DayNightCycleManager.OnTimeChanged += HandleTimeUpdate;
        DayNightCycleManager.OnDayChanged += HandleDayChanged;
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

        // Get current day of week and initialize state
        if (DayNightCycleManager.Instance != null)
        {
            currentDayOfWeek = DayNightCycleManager.Instance.CurrentDayOfWeek;

            // Initialize state based on current time
            float currentTime = DayNightCycleManager.Instance.currentTimeOfDay;
            NPCState initialState = DetermineState(currentTime);
            SwitchState(initialState);
        }
    }

    private void Update()
    {
        // PRIORITY 0: Stunned state overrides everything
        if (isStunned || currentState == NPCState.Stunned)
        {
            // Don't process any other logic while stunned
            return;
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
        else if (enableCombat)
        {
            // Check for player in other non-combat states
            CheckForPlayer();
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
        if (newState == currentState)
            return;

        currentState = newState;

        animator?.SetTrigger(newState.ToString());

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
                            movement?.MoveTo(entry.patrolPoints[patrolIndex]);
                            Debug.Log($"{npcName} starting patrol with {entry.patrolPoints.Count} points");
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

        // Handle normal states
        Transform destination = GetLocationForState(newState);
        if (destination != null)
        {
            movement?.MoveTo(destination);
        }
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
        hasEnteredAggro = false; // NEW: Reset aggro state

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