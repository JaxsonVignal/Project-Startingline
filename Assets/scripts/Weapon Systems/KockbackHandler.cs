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
        // For full-auto weapons, allow knockback stacking instead of blocking
        if (isBeingKnockedBack)
        {
            // Already being knocked back - try to add force to existing Rigidbody
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                // Add additional force to existing knockback
                Vector3 additionalForce = direction.normalized * force * 30f;
                additionalForce.y = force * 15f;
                rb.AddForce(additionalForce, ForceMode.Impulse);
                Debug.Log($"[KnockbackHandler] Added additional force to {gameObject.name} (stacking knockback) - NOT extending duration");
                // DON'T start a new coroutine - let the existing one finish
                return;
            }
        }

        // Start new knockback
        StartCoroutine(KnockbackCoroutine(direction, force, duration));
    }

    private IEnumerator KnockbackCoroutine(Vector3 direction, float force, float duration)
    {
        isBeingKnockedBack = true;

        // Get or add Rigidbody - this is the RIGHT way to do physics
        Rigidbody rb = GetComponent<Rigidbody>();
        bool addedRigidbody = false;

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 70f;
            rb.drag = 0.5f;
            rb.angularDrag = 0.5f;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            addedRigidbody = true;
            Debug.Log($"[KnockbackHandler] Added Rigidbody to {gameObject.name}");
        }

        // Disable NavMeshAgent while physics takes over
        UnityEngine.AI.NavMeshAgent navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        CharacterController charController = GetComponent<CharacterController>();

        bool hadNav = false;
        bool hadChar = false;

        if (navAgent != null && navAgent.enabled)
        {
            hadNav = true;
            navAgent.enabled = false;
            Debug.Log($"[KnockbackHandler] Disabled NavMeshAgent");
        }

        if (charController != null && charController.enabled)
        {
            hadChar = true;
            charController.enabled = false;
            Debug.Log($"[KnockbackHandler] Disabled CharacterController");
        }

        // Make rigidbody active
        rb.isKinematic = false;
        rb.useGravity = true;

        // Apply knockback force (horizontal + upward)
        // Rigidbody needs MUCH higher forces than transform movement!
        Vector3 knockbackForce = direction.normalized * force * 30f; // Multiply by 30!
        knockbackForce.y = force * 15f; // Strong upward component

        rb.AddForce(knockbackForce, ForceMode.Impulse);

        Debug.Log($"[KnockbackHandler] Applied force: {knockbackForce} (magnitude: {knockbackForce.magnitude}) to {gameObject.name}");

        // Wait for knockback duration + time to land
        yield return new WaitForSeconds(duration + 2f);

        // Wait until grounded
        float waitTime = 0f;
        float maxWait = 5f;

        while (waitTime < maxWait)
        {
            // Check if velocity is near zero (landed)
            if (rb.velocity.magnitude < 0.5f)
            {
                Debug.Log($"[KnockbackHandler] {gameObject.name} has landed (velocity: {rb.velocity.magnitude})");
                break;
            }

            yield return new WaitForSeconds(0.1f);
            waitTime += 0.1f;
        }

        // Remove temporary rigidbody if we added it
        if (addedRigidbody)
        {
            // Check if anti-gravity effect is active - if so, keep the Rigidbody!
            AntiGravityEffect antiGrav = GetComponent<AntiGravityEffect>();
            if (antiGrav != null)
            {
                Debug.Log($"[KnockbackHandler] Keeping Rigidbody - anti-gravity effect active");
                // Just make it kinematic so anti-grav can re-enable it
                rb.isKinematic = true;
            }
            else
            {
                // No anti-gravity - safe to remove
                Destroy(rb);
                Debug.Log($"[KnockbackHandler] Removed temporary Rigidbody");
            }
            yield return new WaitForSeconds(0.1f);
        }
        else
        {
            // Make existing rigidbody kinematic again
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Re-enable NavMeshAgent
        if (hadNav && navAgent != null)
        {
            UnityEngine.AI.NavMeshHit navHit;
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out navHit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                navAgent.Warp(navHit.position);
            }
            navAgent.enabled = true;
            Debug.Log($"[KnockbackHandler] Re-enabled NavMeshAgent");
        }

        if (hadChar && charController != null)
        {
            charController.enabled = true;
            Debug.Log($"[KnockbackHandler] Re-enabled CharacterController");
        }

        isBeingKnockedBack = false;

        // Clean up this component
        Destroy(this, 1f);
    }
}