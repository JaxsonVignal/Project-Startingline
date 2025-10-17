using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CivilianMovementController : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform currentDestination;

    [Header("Movement Settings")]
    public float stoppingDistance = 1f;
    public float repathDelay = 1f; // How often to re-path (in seconds)

    private float nextPathTime;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (currentDestination == null)
            return;

        // Refresh the path occasionally
        if (Time.time > nextPathTime)
        {
            agent.SetDestination(currentDestination.position);
            nextPathTime = Time.time + repathDelay;
        }
    }

    /// <summary>
    /// Makes the civilian move to a target location.
    /// </summary>
    public void MoveTo(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning($"{gameObject.name}: Tried to move to a null target!");
            return;
        }

        currentDestination = target;
        agent.isStopped = false;
        agent.stoppingDistance = stoppingDistance;
        agent.SetDestination(target.position);
    }

    /// <summary>
    /// Stops movement immediately.
    /// </summary>
    public void Stop()
    {
        if (agent == null) return;

        agent.isStopped = true;
        currentDestination = null;
    }

    /// <summary>
    /// Checks if the NPC reached its current destination.
    /// </summary>
    public bool IsAtDestination()
    {
        if (currentDestination == null || agent == null)
            return false;

        return !agent.pathPending && agent.remainingDistance <= stoppingDistance;
    }
}
