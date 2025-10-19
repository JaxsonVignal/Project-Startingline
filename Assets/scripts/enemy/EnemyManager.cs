using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(CivilianMovementController))]
public class GuardManager : MonoBehaviour
{
    public enum GuardState { MorningStartZone, Patrol, Station, Aggro, Attack }

    [Header("Guard Info")]
    public string guardName = "Guard";
    public GuardState currentState;

    [Header("Schedule Settings")]
    public float morningStartTime = 6f;
    public float patrolStartTime = 9f;
    public float stationStartTime = 18f;

    [Header("Waypoints")]
    public Transform morningStartLocation;
    public List<Transform> patrolPoints;
    public Transform stationLocation;

    private int patrolIndex;
    private float patrolWaitTimer;
    public float patrolWaitTime = 2f;

    [Header("Combat Settings")]
    public Transform player;
    public float sightRange = 20f;
    public float attackRange = 10f;
    public float loseAggroDistance = 30f;

    private Animator animator;
    private CivilianMovementController movement;
    private bool hasEnteredAggro;
    private float losCheckTimer;

    private void OnEnable() => DayNightCycleManager.OnTimeChanged += HandleTimeUpdate;
    private void OnDisable() => DayNightCycleManager.OnTimeChanged -= HandleTimeUpdate;

    private void Start()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<CivilianMovementController>();

        if (DayNightCycleManager.Instance != null)
            HandleTimeUpdate(DayNightCycleManager.Instance.currentTimeOfDay);
    }

    private void Update()
    {
        switch (currentState)
        {
            case GuardState.MorningStartZone:
            case GuardState.Station:
                CheckForPlayer();
                break;

            case GuardState.Patrol:
                HandlePatrol();
                CheckForPlayer();
                break;

            case GuardState.Aggro:
            case GuardState.Attack:
                HandleCombatBehavior();
                break;
        }
    }

    // TIME-BASED STATE HANDLING
    private void HandleTimeUpdate(float hour)
    {
        if (hasEnteredAggro) return;

        GuardState newState = DetermineState(hour);
        if (newState != currentState)
            SwitchState(newState);
    }

    private GuardState DetermineState(float hour)
    {
        if (hour >= stationStartTime)
            return GuardState.Station;
        else if (hour >= patrolStartTime)
            return GuardState.Patrol;
        else
            return GuardState.MorningStartZone;
    }

    private void SwitchState(GuardState newState)
    {
        currentState = newState;
        Debug.Log($"{guardName} switched to {newState}");

        switch (newState)
        {
            case GuardState.MorningStartZone:
                if (morningStartLocation) movement.MoveTo(morningStartLocation);
                break;
            case GuardState.Patrol:
                if (patrolPoints != null && patrolPoints.Count > 0)
                    movement.MoveTo(patrolPoints[patrolIndex]);
                break;
            case GuardState.Station:
                if (stationLocation) movement.MoveTo(stationLocation);
                break;
        }
    }

    // PATROL LOGIC
    private void HandlePatrol()
    {
        if (patrolPoints == null || patrolPoints.Count == 0) return;

        Transform target = patrolPoints[patrolIndex];
        float distance = Vector3.Distance(transform.position, target.position);

        if (distance < 1f)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWaitTime)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Count;
                patrolWaitTimer = 0f;
                movement.MoveTo(patrolPoints[patrolIndex]);
            }
        }
    }

    // COMBAT LOGIC
    private void HandleCombatBehavior()
    {
        if (!player) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool canSeePlayer = CanSeePlayer();

        // Player escaped too far - return to schedule
        if (distance > loseAggroDistance)
        {
            Debug.Log($"{guardName} lost player, returning to routine.");
            hasEnteredAggro = false;
            movement.StopMovement();
            HandleTimeUpdate(DayNightCycleManager.Instance.currentTimeOfDay);
            return;
        }

        // Can see player AND within attack range - ATTACK
        if (canSeePlayer && distance <= attackRange)
        {
            if (currentState != GuardState.Attack)
            {
                currentState = GuardState.Attack;
                animator.SetTrigger("Attack");
                Debug.Log($"{guardName} ATTACKING player!");
            }

            movement.StopMovement();
            FaceTarget(player.position);
            return;
        }

        // Lost LOS or out of range - CHASE
        if (currentState != GuardState.Aggro)
        {
            currentState = GuardState.Aggro;
            Debug.Log($"{guardName} chasing player!");
        }

        movement.MoveTo(player, true);
        FaceTarget(player.position);
    }

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

    // PLAYER DETECTION
    private void CheckForPlayer()
    {
        // Passive check - only used in non-combat states
        // Guard enters combat only when damaged
    }

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

    public void OnDamaged()
    {
        if (!hasEnteredAggro)
        {
            hasEnteredAggro = true;
            currentState = GuardState.Aggro;
            Debug.Log($"{guardName} entered AGGRO state!");
        }
    }
}