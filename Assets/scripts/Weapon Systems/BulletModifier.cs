using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this component to bullets to enable modifier effects
/// The bullet will handle all modifier logic itself
/// </summary>
public class BulletModifier : MonoBehaviour
{
    private ModifierData modifierData;
    private float weaponDamage; // Store weapon damage for explosive calculations

    /// <summary>
    /// Call this from PlayerShooting to pass modifier data to the bullet
    /// </summary>
    public void Initialize(ModifierData modifier, float damage)
    {
        modifierData = modifier;
        weaponDamage = damage;

        if (modifierData != null)
        {
            Debug.Log($"[BulletModifier] Initialized with modifier (Weapon Damage: {weaponDamage})");
            if (modifierData.antiGravityRounds)
            {
                Debug.Log($"  - Anti-Gravity enabled (Force: {modifierData.antiGravityForce}, Duration: {modifierData.antiGravityDuration}s)");
            }
            if (modifierData.cryoRounds)
            {
                Debug.Log($"  - Cryo enabled (Duration: {modifierData.cryoDuration}s)");
            }
            if (modifierData.explosiveRounds)
            {
                Debug.Log($"  - Explosive enabled (Radius: {modifierData.explosionRadius}, Damage Multiplier: {modifierData.explosionDamageMultiplier}x)");
            }
            if (modifierData.tracerRounds)
            {
                Debug.Log($"  - Tracer enabled (Color: {modifierData.tracerColor}, Intensity: {modifierData.tracerIntensity})");
                ApplyTracerEffect();
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

        // EXPLOSIVE EFFECT - Apply first so it can damage multiple enemies
        if (modifierData.explosiveRounds)
        {
            ApplyExplosion(collision);
        }

        // ANTI-GRAVITY EFFECT
        if (modifierData.antiGravityRounds)
        {
            ApplyAntiGravity(collision);
        }

        // CRYO EFFECT
        if (modifierData.cryoRounds)
        {
            ApplyCryo(collision);
        }
    }

    /// <summary>
    /// Apply anti-gravity effect to the hit target
    /// Now works WITHOUT Rigidbody - just moves the transform
    /// Only affects objects on the "Enemies" layer
    /// </summary>
    private void ApplyAntiGravity(Collision collision)
    {
        if (collision.collider == null) return;

        GameObject target = collision.collider.gameObject;

        // Check if target is on the "Enemies" layer
        if (target.layer != LayerMask.NameToLayer("Enemies"))
        {
            Debug.Log($"[BulletModifier] Skipping anti-gravity on {target.name} - not on Enemies layer");
            return;
        }

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

    /// <summary>
    /// Apply cryo/freeze effect to the hit target
    /// Freezes target in place
    /// Only affects objects on the "Enemies" layer
    /// </summary>
    private void ApplyCryo(Collision collision)
    {
        if (collision.collider == null) return;

        GameObject target = collision.collider.gameObject;

        // Check if target is on the "Enemies" layer
        if (target.layer != LayerMask.NameToLayer("Enemies"))
        {
            Debug.Log($"[BulletModifier] Skipping cryo on {target.name} - not on Enemies layer");
            return;
        }

        Debug.Log($"[BulletModifier] Applying cryo effect to {target.name}");

        // Check if target already has a cryo effect component
        CryoEffect existingEffect = target.GetComponent<CryoEffect>();

        if (existingEffect != null)
        {
            // Refresh the existing effect
            existingEffect.RefreshEffect(
                modifierData.cryoDuration,
                modifierData.freezeAnimation,
                modifierData.freezeTintColor,
                modifierData.freezeTintStrength,
                modifierData.iceEffectPrefab
            );
        }
        else
        {
            // Add new cryo effect component to the target
            CryoEffect effect = target.AddComponent<CryoEffect>();
            effect.Initialize(
                modifierData.cryoDuration,
                modifierData.freezeAnimation,
                modifierData.freezeTintColor,
                modifierData.freezeTintStrength,
                modifierData.iceEffectPrefab
            );
        }
    }

    /// <summary>
    /// Apply explosion effect at impact point
    /// Damages all enemies in radius and applies physics force
    /// </summary>
    private void ApplyExplosion(Collision collision)
    {
        if (collision.contacts.Length == 0) return;

        Vector3 explosionPos = collision.contacts[0].point;

        Debug.Log($"[BulletModifier] Creating explosion at {explosionPos} with radius {modifierData.explosionRadius}");

        // Calculate explosion damage
        float explosionDamage = weaponDamage * modifierData.explosionDamageMultiplier;

        // Spawn explosion effect
        if (modifierData.explosionEffectPrefab != null)
        {
            GameObject explosion = Instantiate(modifierData.explosionEffectPrefab, explosionPos, Quaternion.identity);
            Destroy(explosion, 5f); // Clean up after 5 seconds
            Debug.Log($"[BulletModifier] Spawned explosion effect");
        }

        // Find all colliders in explosion radius
        Collider[] hitColliders = Physics.OverlapSphere(explosionPos, modifierData.explosionRadius);

        int enemiesHit = 0;

        foreach (Collider hitCollider in hitColliders)
        {
            // Only damage objects on Enemies layer
            if (hitCollider.gameObject.layer != LayerMask.NameToLayer("Enemies"))
            {
                continue;
            }

            // Calculate distance-based damage falloff
            float distance = Vector3.Distance(explosionPos, hitCollider.transform.position);
            float damageMultiplier = 1f - (distance / modifierData.explosionRadius);
            float finalDamage = explosionDamage * damageMultiplier;

            // Apply damage to enemy
            EnemyHealth enemyHealth = hitCollider.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(finalDamage);
                enemiesHit++;
                Debug.Log($"[BulletModifier] Explosion hit {hitCollider.gameObject.name} for {finalDamage} damage (distance: {distance:F1})");
            }

            // Apply physics force if enabled
            if (modifierData.applyExplosionForce)
            {
                Rigidbody rb = hitCollider.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(modifierData.explosionForceStrength, explosionPos, modifierData.explosionRadius);
                    Debug.Log($"[BulletModifier] Applied explosion force to {hitCollider.gameObject.name}");
                }
            }
        }

        Debug.Log($"[BulletModifier] Explosion hit {enemiesHit} enemies");

        // Debug visualization
        if (modifierData.showExplosionRadius)
        {
            Debug.DrawLine(explosionPos, explosionPos + Vector3.up * modifierData.explosionRadius, Color.red, 2f);
        }
    }

    /// <summary>
    /// Apply tracer effect to the bullet
    /// Makes bullet glow and emit light as it flies
    /// Called immediately when bullet spawns (from Initialize)
    /// </summary>
    private void ApplyTracerEffect()
    {
        Debug.Log($"[BulletModifier] Applying tracer effect to bullet");

        // Add light component to bullet
        Light tracerLight = gameObject.GetComponent<Light>();
        if (tracerLight == null)
        {
            tracerLight = gameObject.AddComponent<Light>();
        }

        tracerLight.type = LightType.Point;
        tracerLight.color = modifierData.tracerColor;
        tracerLight.intensity = modifierData.tracerIntensity;
        tracerLight.range = modifierData.tracerLightRange;
        tracerLight.renderMode = LightRenderMode.ForcePixel;

        Debug.Log($"[BulletModifier] Added point light to bullet (Color: {modifierData.tracerColor}, Intensity: {modifierData.tracerIntensity})");

        // Add emission to bullet material
        if (modifierData.addEmission)
        {
            Renderer bulletRenderer = GetComponent<Renderer>();
            if (bulletRenderer != null && bulletRenderer.material != null)
            {
                Material mat = bulletRenderer.material;

                // Enable emission keyword
                mat.EnableKeyword("_EMISSION");

                // Set emission color (color * intensity for HDR glow)
                Color emissionColor = modifierData.tracerColor * modifierData.emissionIntensity;
                mat.SetColor("_EmissionColor", emissionColor);

                Debug.Log($"[BulletModifier] Enabled emission on bullet material");
            }
        }

        // Add trail effect if prefab provided
        if (modifierData.trailEffectPrefab != null)
        {
            GameObject trailObj = Instantiate(modifierData.trailEffectPrefab, transform.position, Quaternion.identity, transform);

            // Configure trail renderer if it has one
            TrailRenderer trail = trailObj.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.time = modifierData.trailDuration;
                trail.startColor = modifierData.tracerColor;
                trail.endColor = new Color(modifierData.tracerColor.r, modifierData.tracerColor.g, modifierData.tracerColor.b, 0f);
                Debug.Log($"[BulletModifier] Added trail effect to bullet");
            }
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

/// <summary>
/// Component that applies cryo/freeze effect to targets
/// Freezes them in place and optionally tints them blue
/// </summary>
public class CryoEffect : MonoBehaviour
{
    private float duration;
    private float timeRemaining;
    private bool freezeAnimation;
    private Color tintColor;
    private float tintStrength;

    // Components to freeze
    private UnityEngine.AI.NavMeshAgent navAgent;
    private bool hadNavAgent = false;
    private bool wasNavAgentOnNavMesh = false;
    private CharacterController characterController;
    private bool hadCharacterController = false;
    private Animator animator;
    private bool hadAnimator = false;
    private float originalAnimatorSpeed;

    // Visual effects
    private GameObject iceEffectInstance;
    private Renderer[] renderers;
    private Dictionary<Renderer, Color[]> originalColors = new Dictionary<Renderer, Color[]>();

    public void Initialize(float freezeDuration, bool shouldFreezeAnimation, Color freezeColor, float tintAmount, GameObject iceEffectPrefab)
    {
        duration = freezeDuration;
        timeRemaining = duration;
        freezeAnimation = shouldFreezeAnimation;
        tintColor = freezeColor;
        tintStrength = tintAmount;

        // Disable movement components
        navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null)
        {
            hadNavAgent = true;
            wasNavAgentOnNavMesh = navAgent.isOnNavMesh;

            // Stop the agent but keep it enabled so it stays on the NavMesh
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;

            Debug.Log($"[CryoEffect] Stopped NavMeshAgent on {gameObject.name}");
        }

        characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            hadCharacterController = true;
            characterController.enabled = false;
            Debug.Log($"[CryoEffect] Disabled CharacterController on {gameObject.name}");
        }

        // Freeze animation
        if (freezeAnimation)
        {
            animator = GetComponent<Animator>();
            if (animator != null)
            {
                hadAnimator = true;
                originalAnimatorSpeed = animator.speed;
                animator.speed = 0f;
                Debug.Log($"[CryoEffect] Froze Animator on {gameObject.name}");
            }
        }

        // Apply visual tint
        if (tintStrength > 0f)
        {
            ApplyFreezeTint();
        }

        // Spawn ice effect
        if (iceEffectPrefab != null)
        {
            iceEffectInstance = Instantiate(iceEffectPrefab, transform.position, Quaternion.identity, transform);
            Debug.Log($"[CryoEffect] Spawned ice effect on {gameObject.name}");
        }

        Debug.Log($"[CryoEffect] Started on {gameObject.name} for {duration}s");
    }

    public void RefreshEffect(float freezeDuration, bool shouldFreezeAnimation, Color freezeColor, float tintAmount, GameObject iceEffectPrefab)
    {
        duration = freezeDuration;
        timeRemaining = duration; // Reset timer
        freezeAnimation = shouldFreezeAnimation;
        tintColor = freezeColor;
        tintStrength = tintAmount;

        Debug.Log($"[CryoEffect] Refreshed on {gameObject.name}");
    }

    private void ApplyFreezeTint()
    {
        renderers = GetComponentsInChildren<Renderer>();

        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;

            // Store original colors
            Material[] materials = renderer.materials;
            Color[] colors = new Color[materials.Length];

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i].HasProperty("_Color"))
                {
                    colors[i] = materials[i].color;
                    // Apply tint
                    Color tintedColor = Color.Lerp(colors[i], tintColor, tintStrength);
                    materials[i].color = tintedColor;
                }
            }

            originalColors[renderer] = colors;
            renderer.materials = materials;
        }

        Debug.Log($"[CryoEffect] Applied freeze tint to {renderers.Length} renderers");
    }

    private void RemoveFreezeTint()
    {
        if (renderers == null) return;

        foreach (var renderer in renderers)
        {
            if (renderer == null || !originalColors.ContainsKey(renderer)) continue;

            Material[] materials = renderer.materials;
            Color[] colors = originalColors[renderer];

            for (int i = 0; i < materials.Length && i < colors.Length; i++)
            {
                if (materials[i].HasProperty("_Color"))
                {
                    materials[i].color = colors[i];
                }
            }

            renderer.materials = materials;
        }

        Debug.Log($"[CryoEffect] Removed freeze tint");
    }

    private void Update()
    {
        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            EndEffect();
        }
    }

    private void EndEffect()
    {
        Debug.Log($"[CryoEffect] Ended on {gameObject.name}");

        // Re-enable NavMeshAgent and resume movement
        if (hadNavAgent && navAgent != null)
        {
            navAgent.isStopped = false;

            // If the agent was on NavMesh and has a valid path, it should resume
            if (wasNavAgentOnNavMesh && navAgent.isOnNavMesh)
            {
                Debug.Log($"[CryoEffect] Resumed NavMeshAgent on {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[CryoEffect] NavMeshAgent not on NavMesh for {gameObject.name}");
            }
        }

        // Re-enable CharacterController
        if (hadCharacterController && characterController != null)
        {
            characterController.enabled = true;
            Debug.Log($"[CryoEffect] Re-enabled CharacterController on {gameObject.name}");
        }

        // Unfreeze animation
        if (hadAnimator && animator != null)
        {
            animator.speed = originalAnimatorSpeed;
            Debug.Log($"[CryoEffect] Unfroze Animator on {gameObject.name}");
        }

        // Remove visual effects
        RemoveFreezeTint();

        if (iceEffectInstance != null)
        {
            Destroy(iceEffectInstance);
        }

        Destroy(this);
    }

    private void OnDestroy()
    {
        // Restore components if destroyed prematurely
        if (hadNavAgent && navAgent != null)
        {
            navAgent.isStopped = false;
        }

        if (hadCharacterController && characterController != null)
        {
            characterController.enabled = true;
        }

        if (hadAnimator && animator != null)
        {
            animator.speed = originalAnimatorSpeed;
        }

        // Clean up visual effects
        RemoveFreezeTint();

        if (iceEffectInstance != null)
        {
            Destroy(iceEffectInstance);
        }
    }
}