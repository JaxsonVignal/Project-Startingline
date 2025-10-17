using UnityEngine;

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
        else if (hour >= wakeUpTime && hour < workStartTime)
            return NPCState.Eating;
        else if (hour >= workStartTime && hour < workEndTime)
            return NPCState.Working;
        else
            return NPCState.Idle;
    }

    private void SwitchState(NPCState newState)
    {
        currentState = newState;
        Debug.Log($"{npcName} switched to {newState}");

        // Animation trigger
        if (animator) animator.SetTrigger(newState.ToString());

        // Movement target
        switch (newState)
        {
            case NPCState.Sleeping:
                movement.MoveTo(bedLocation);
                break;
            case NPCState.Eating:
                movement.MoveTo(eatLocation);
                break;
            case NPCState.Working:
                movement.MoveTo(workLocation);
                break;
            case NPCState.Idle:
                movement.MoveTo(idleLocation);
                break;
        }
    }
}
