using System.Collections;
using UnityEngine;

/// <summary>
/// Handles knockback effects for an enemy
/// This component gets added to enemies temporarily to handle knockback
/// Lives on the ENEMY so the coroutine doesn't die when the bullet is destroyed
/// </summary>
public class KnockbackHandler : MonoBehaviour
{
    private bool isBeingKnockedBack = false;

    public bool IsBeingKnockedBack => isBeingKnockedBack;

    /// <summary>
    /// Apply knockback to this enemy
    /// </summary>
    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        if (isBeingKnockedBack)
        {
            Debug.Log($"[KnockbackHandler] {gameObject.name} already being knocked back - ignoring new knockback");
            return;
        }

        StartCoroutine(KnockbackCoroutine(direction, force, duration));
    }

    private IEnumerator KnockbackCoroutine(Vector3 direction, float force, float duration)
    {
        isBeingKnockedBack = true;

        float elapsed = 0f;

        // Disable NavMeshAgent AND CharacterController temporarily
        UnityEngine.AI.NavMeshAgent navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        CharacterController charController = GetComponent<CharacterController>();

        bool hadNav = false;
        bool hadChar = false;

        if (navAgent != null && navAgent.enabled)
        {
            hadNav = true;
            navAgent.enabled = false;
            Debug.Log($"[KnockbackHandler] Disabled NavMeshAgent on {gameObject.name}");
        }

        if (charController != null && charController.enabled)
        {
            hadChar = true;
            charController.enabled = false;
            Debug.Log($"[KnockbackHandler] Disabled CharacterController on {gameObject.name}");
        }

        float verticalVelocity = force * 0.5f; // Initial upward velocity

        Vector3 startPosition = transform.position;
        Debug.Log($"[KnockbackHandler] Starting knockback on {gameObject.name} - Force: {force}, Duration: {duration}");

        // Knockback phase
        while (elapsed < duration)
        {
            if (this == null || gameObject == null)
            {
                Debug.LogError($"[KnockbackHandler] GameObject destroyed during knockback!");
                yield break;
            }

            // Calculate horizontal knockback this frame (decays over duration)
            float remainingTime = 1f - (elapsed / duration);
            float currentForce = force * remainingTime;

            // Apply horizontal knockback
            Vector3 horizontalDirection = new Vector3(direction.x, 0f, direction.z).normalized;
            transform.position += horizontalDirection * currentForce * Time.deltaTime;

            // Apply vertical velocity (goes up then down due to gravity)
            transform.position += Vector3.up * verticalVelocity * Time.deltaTime;

            // Apply gravity to vertical velocity
            verticalVelocity -= 9.81f * Time.deltaTime;

            elapsed += Time.deltaTime;
            yield return null;
        }

        Vector3 peakPosition = transform.position;
        Debug.Log($"[KnockbackHandler] ===== KNOCKBACK PHASE COMPLETE ===== Distance: {Vector3.Distance(startPosition, peakPosition):F2}");

        // Simplified falling - just move down until we hit ground
        int fallFrames = 0;
        int maxFallFrames = 300; // ~5 seconds at 60fps

        Debug.Log($"[KnockbackHandler] ===== STARTING FALL PHASE ===== from height {transform.position.y}");

        while (fallFrames < maxFallFrames)
        {
            if (this == null || gameObject == null)
            {
                Debug.LogError($"[KnockbackHandler] GameObject destroyed during fall!");
                yield break;
            }

            fallFrames++;

            // Simply move down each frame
            float fallSpeed = 5f; // Units per second
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;

            // Check if we hit ground
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 1f))
            {
                // Hit ground! Place on ground
                transform.position = hit.point;
                Debug.Log($"[KnockbackHandler] {gameObject.name} HIT GROUND at {hit.point} after {fallFrames} frames");
                break;
            }

            // Also check if we're below Y=0 (definitely on ground)
            if (transform.position.y <= 0.1f)
            {
                Debug.Log($"[KnockbackHandler] {gameObject.name} reached Y=0, placing on ground");
                transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
                break;
            }

            yield return null;
        }

        // If we timed out, force to Y=0
        if (fallFrames >= maxFallFrames)
        {
            Debug.LogWarning($"[KnockbackHandler] Fall timeout! Forcing {gameObject.name} to Y=0");
            transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
        }

        Debug.Log($"[KnockbackHandler] Fall complete for {gameObject.name} - Final position: {transform.position}");

        // Re-enable components
        if (hadNav && navAgent != null)
        {
            // Find nearest NavMesh point
            UnityEngine.AI.NavMeshHit navHit;
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out navHit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                navAgent.Warp(navHit.position);
            }
            navAgent.enabled = true;
            Debug.Log($"[KnockbackHandler] Re-enabled NavMeshAgent on {gameObject.name}");
        }

        if (hadChar && charController != null)
        {
            charController.enabled = true;
            Debug.Log($"[KnockbackHandler] Re-enabled CharacterController on {gameObject.name}");
        }

        isBeingKnockedBack = false;

        // Clean up this component after a delay
        Destroy(this, 1f);
    }
}