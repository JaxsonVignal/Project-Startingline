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

    public void MoveTo(Transform target, bool alwaysUpdate = false)
    {
        if (target == null || overrideMovement)
            return;

        if (!alwaysUpdate && currentTarget == target)
            return;

        currentTarget = target;
        agent.isStopped = false;
        agent.SetDestination(target.position);
    }

    public void OverrideMovementTemporarily(Transform target, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(OverrideRoutine(target, duration));
    }

    private IEnumerator OverrideRoutine(Transform target, float duration)
    {
        overrideMovement = true;
        agent.isStopped = true;
        agent.ResetPath();

        // Position once
        transform.position = target.position;

        float timer = 0f;
        while (timer < duration)
        {
            transform.position = target.position; // keep them there
            timer += Time.deltaTime;
            yield return null;
        }

        overrideMovement = false;
        agent.isStopped = false;
    }

    public void StopMovement()
    {
        agent.isStopped = true;
        agent.ResetPath();
        currentTarget = null;
    }

    public void MoveToPosition(Vector3 position)
    {
        if (agent == null || overrideMovement) return;

        GameObject tempTarget = new GameObject("TempTarget");
        tempTarget.transform.position = position;

        MoveTo(tempTarget.transform);

        StartCoroutine(DestroyTempTarget(tempTarget));
    }

    private IEnumerator DestroyTempTarget(GameObject temp)
    {
        yield return new WaitForSeconds(5f);
        if (temp != null) Destroy(temp);
    }
}
