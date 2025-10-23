using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator), typeof(CivilianMovementController))]
public class NPCManager : MonoBehaviour
{
    public enum NPCState { Sleeping, Eating, Working, Idle }

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

    private Animator animator;
    private CivilianMovementController movement;

    private void OnEnable() => DayNightCycleManager.OnTimeChanged += HandleTimeUpdate;
    private void OnDisable() => DayNightCycleManager.OnTimeChanged -= HandleTimeUpdate;

    private void Start()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<CivilianMovementController>();
    }

    private void HandleTimeUpdate(float hour)
    {
        NPCState newState = DetermineState(hour);
        if (newState != currentState)
            SwitchState(newState);
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

    public void ResetToBed()
    {
        if (bedLocation == null)
        {
            Debug.LogWarning($"{npcName} has no bed assigned!");
            return;
        }

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
