using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class CivilianMovementController : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform currentTarget;
    private bool overrideMovement = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public Transform CurrentTarget => currentTarget;

    /// <summary>
    /// Move to a target. If alwaysUpdate is true, the agent will continuously update the destination.
    /// </summary>
    public void MoveTo(Transform target, bool alwaysUpdate = false)
    {
        if (target == null || overrideMovement)
            return;

        // Only skip updating if not forced
        if (!alwaysUpdate && currentTarget == target)
            return;

        currentTarget = target;
        agent.SetDestination(target.position);
        agent.isStopped = false;
    }

    /// <summary>
    /// Temporarily override movement to a fixed position for duration seconds
    /// </summary>
    public void OverrideMovementTemporarily(Transform target, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(OverrideRoutine(target, duration));
    }

    private IEnumerator OverrideRoutine(Transform target, float duration)
    {
        overrideMovement = true;

        // Teleport and reset path
        agent.Warp(target.position);
        agent.ResetPath();
        currentTarget = target;

        float timer = 0f;
        while (timer < duration)
        {
            agent.Warp(target.position); // keep agent at target
            timer += Time.deltaTime;
            yield return null;
        }

        overrideMovement = false;
    }

    /// <summary>
    /// Stops movement immediately
    /// </summary>
    public void StopMovement()
    {
        agent.isStopped = true;
        agent.ResetPath();
        currentTarget = null;
    }

    public void MoveToPosition(Vector3 position)
    {
        if (agent == null || overrideMovement) return;

        // Create a temporary GameObject to hold the position
        GameObject tempTarget = new GameObject("TempTarget");
        tempTarget.transform.position = position;

        MoveTo(tempTarget.transform);

        // Destroy temp target after reaching it
        StartCoroutine(DestroyTempTarget(tempTarget)); // destroy after 5 seconds max
    }

    private IEnumerator DestroyTempTarget(GameObject temp)
    {
        yield return new WaitForSeconds(5f);
        if (temp != null) Destroy(temp);
    }
}
