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

    public void MoveTo(Transform target)
    {
        if (target == null || overrideMovement)
            return;

        if (currentTarget == target)
            return; // Already moving there

        currentTarget = target;
        agent.SetDestination(target.position);
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
            // Keep agent stopped at target
            agent.Warp(target.position);
            timer += Time.deltaTime;
            yield return null;
        }

        overrideMovement = false;
    }
}
