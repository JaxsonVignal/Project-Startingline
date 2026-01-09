using System.Collections;
using UnityEngine;

/// <summary>
/// Attach this component to bullets to enable modifier effects
/// The bullet will handle all modifier logic itself
/// </summary>
public class BulletModifier : MonoBehaviour
{
    private ModifierData modifierData;

    /// <summary>
    /// Call this from PlayerShooting to pass modifier data to the bullet
    /// </summary>
    public void Initialize(ModifierData modifier)
    {
        modifierData = modifier;

        if (modifierData != null)
        {
            Debug.Log($"[BulletModifier] Initialized with modifier");
            if (modifierData.antiGravityRounds)
            {
                Debug.Log($"  - Anti-Gravity enabled (Force: {modifierData.antiGravityForce}, Duration: {modifierData.antiGravityDuration}s)");
            }
        }
    }

    /// <summary>
    /// Called when bullet collides with something
    /// This is where all modifier effects are applied
    /// </summary>
    public void OnBulletHit(Collision collision)
    {
        if (modifierData == null) return;

        // ANTI-GRAVITY EFFECT
        if (modifierData.antiGravityRounds)
        {
            ApplyAntiGravity(collision);
        }

        // ADD MORE MODIFIER EFFECTS HERE
        // Example:
        // if (modifierData.explosiveRounds)
        // {
        //     ApplyExplosion(collision);
        // }
    }

    /// <summary>
    /// Apply anti-gravity effect to the hit target
    /// Now works WITHOUT Rigidbody - just moves the transform
    /// </summary>
    private void ApplyAntiGravity(Collision collision)
    {
        if (collision.collider == null) return;

        GameObject target = collision.collider.gameObject;

        Debug.Log($"[BulletModifier] Applying anti-gravity to {target.name}");

        // Check if target already has an anti-gravity effect component
        AntiGravityEffect existingEffect = target.GetComponent<AntiGravityEffect>();

        if (existingEffect != null)
        {
            // Refresh the existing effect
            existingEffect.RefreshEffect(
                modifierData.antiGravityForce,
                modifierData.antiGravityDuration,
                modifierData.disableGravity,
                modifierData.initialUpwardImpulse,
                modifierData.bobbingAmount,
                modifierData.bobbingSpeed
            );
        }
        else
        {
            // Add new anti-gravity effect component to the target
            AntiGravityEffect effect = target.AddComponent<AntiGravityEffect>();
            effect.Initialize(
                modifierData.antiGravityForce,
                modifierData.antiGravityDuration,
                modifierData.disableGravity,
                modifierData.initialUpwardImpulse,
                modifierData.bobbingAmount,
                modifierData.bobbingSpeed
            );
        }
    }

    // ADD MORE EFFECT METHODS HERE
    // Example:
    // private void ApplyExplosion(Collision collision)
    // {
    //     Vector3 explosionPos = collision.contacts[0].point;
    //     Collider[] hits = Physics.OverlapSphere(explosionPos, modifierData.explosionRadius);
    //     
    //     foreach (var hit in hits)
    //     {
    //         var enemy = hit.GetComponent<EnemyHealth>();
    //         if (enemy != null)
    //         {
    //             enemy.TakeDamage(modifierData.explosionDamage);
    //         }
    //     }
    // }
}

/// <summary>
/// Component that applies anti-gravity effect to any object
/// This gets added to targets when they're hit by anti-gravity rounds
/// Works WITHOUT Rigidbody - just moves the transform
/// </summary>
public class AntiGravityEffect : MonoBehaviour
{
    private float liftHeight;
    private float liftSpeed;
    private float duration;
    private float timeRemaining;
    private float bobbingAmount;
    private float bobbingSpeed;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool isLifting = false;
    private bool isLowering = false;
    private Vector3 groundPosition;

    // For enemies with NavMeshAgent
    private UnityEngine.AI.NavMeshAgent navAgent;
    private bool hadNavAgent = false;

    // For enemies with CharacterController
    private CharacterController characterController;
    private bool hadCharacterController = false;

    public void Initialize(float antiGravityForce, float antiGravityDuration, bool disableGravity, float initialImpulse, float bobAmount, float bobSpeed)
    {
        // antiGravityForce is now used as lift height (in units/feet)
        liftHeight = antiGravityForce;
        liftSpeed = initialImpulse; // initialImpulse is now lift speed
        duration = antiGravityDuration;
        timeRemaining = duration;
        bobbingAmount = bobAmount;
        bobbingSpeed = bobSpeed;

        startPosition = transform.position;
        targetPosition = startPosition + Vector3.up * liftHeight;
        groundPosition = startPosition; // Remember where ground level is
        isLifting = true;

        // Disable NavMeshAgent if present
        navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null)
        {
            hadNavAgent = true;
            navAgent.enabled = false;
            Debug.Log($"[AntiGravityEffect] Disabled NavMeshAgent on {gameObject.name}");
        }

        // Disable CharacterController if present
        characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            hadCharacterController = true;
            characterController.enabled = false;
            Debug.Log($"[AntiGravityEffect] Disabled CharacterController on {gameObject.name}");
        }

        Debug.Log($"[AntiGravityEffect] Started on {gameObject.name} - lifting {liftHeight} units over {duration}s at speed {liftSpeed} (bobbing: {bobbingAmount})");
    }

    /// <summary>
    /// Refresh the effect if hit by another anti-gravity bullet
    /// </summary>
    public void RefreshEffect(float antiGravityForce, float antiGravityDuration, bool disableGravity, float initialImpulse, float bobAmount, float bobSpeed)
    {
        liftHeight = antiGravityForce;
        liftSpeed = initialImpulse;
        duration = antiGravityDuration;
        timeRemaining = duration; // Reset timer
        bobbingAmount = bobAmount;
        bobbingSpeed = bobSpeed;

        // If we were lowering, stop and start lifting again
        if (isLowering)
        {
            isLowering = false;
            isLifting = true;
        }

        // Update target position from current position
        startPosition = transform.position;
        targetPosition = startPosition + Vector3.up * liftHeight;

        Debug.Log($"[AntiGravityEffect] Refreshed on {gameObject.name}");
    }

    private void Update()
    {
        if (isLowering)
        {
            // Lower back to ground
            transform.position = Vector3.MoveTowards(
                transform.position,
                groundPosition,
                liftSpeed * Time.deltaTime
            );

            // Check if we've reached the ground
            if (Vector3.Distance(transform.position, groundPosition) < 0.1f)
            {
                transform.position = groundPosition;
                EndEffect();
            }
            return;
        }

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            // Start lowering back to ground
            StartLowering();
            return;
        }

        if (isLifting)
        {
            // Smoothly lift the object upward
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                liftSpeed * Time.deltaTime
            );

            // Check if we've reached the target height
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                isLifting = false;
                Debug.Log($"[AntiGravityEffect] {gameObject.name} reached target height, now floating");
            }
        }
        else
        {
            // Keep floating at target height (with bobbing effect)
            float bob = Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount;
            Vector3 floatPosition = new Vector3(targetPosition.x, targetPosition.y + bob, targetPosition.z);
            transform.position = Vector3.Lerp(transform.position, floatPosition, Time.deltaTime * 2f);
        }
    }

    private void StartLowering()
    {
        Debug.Log($"[AntiGravityEffect] {gameObject.name} starting to lower back to ground");
        isLifting = false;
        isLowering = true;
    }

    private void EndEffect()
    {
        Debug.Log($"[AntiGravityEffect] Ended on {gameObject.name}");

        // Re-enable NavMeshAgent if it had one
        if (hadNavAgent && navAgent != null)
        {
            navAgent.enabled = true;
            Debug.Log($"[AntiGravityEffect] Re-enabled NavMeshAgent on {gameObject.name}");
        }

        // Re-enable CharacterController if it had one
        if (hadCharacterController && characterController != null)
        {
            characterController.enabled = true;
            Debug.Log($"[AntiGravityEffect] Re-enabled CharacterController on {gameObject.name}");
        }

        Destroy(this);
    }

    private void OnDestroy()
    {
        // Restore components if this is destroyed prematurely
        if (hadNavAgent && navAgent != null)
        {
            navAgent.enabled = true;
        }

        if (hadCharacterController && characterController != null)
        {
            characterController.enabled = true;
        }
    }
}