using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this component to bullets to enable modifier effects
/// The bullet will handle all modifier logic itself
/// NOW AFFECTS BOTH "Enemies" AND "NPCs" LAYERS!
/// </summary>
public class BulletModifier : MonoBehaviour
{
    private ModifierData modifierData;
    private float weaponDamage; // Store weapon damage for explosive calculations

    // Ricochet tracking
    private int bouncesRemaining = 0;
    private float currentDamageMultiplier = 1f;

    // Helper method to check if an object is a valid target (Enemies OR NPCs layer)
    private bool IsValidTarget(GameObject obj)
    {
        return obj.layer == LayerMask.NameToLayer("Enemies") ||
               obj.layer == LayerMask.NameToLayer("NPCs");
    }

    private void Start()
    {
        // Setup phase collision if enabled
        if (modifierData != null && modifierData.phaseRounds)
        {
            SetupPhaseCollision();
        }
    }

    /// <summary>
    /// Call this from PlayerShooting to pass modifier data to the bullet
    /// </summary>
    public void Initialize(ModifierData modifier, float damage)
    {
        modifierData = modifier;
        weaponDamage = damage;

        // Setup ricochet tracking
        if (modifierData != null && modifierData.ricochetRounds)
        {
            bouncesRemaining = modifierData.maxBounces;
            currentDamageMultiplier = 1f;
        }

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
            if (modifierData.ricochetRounds)
            {
                Debug.Log($"  - Ricochet enabled (Max Bounces: {modifierData.maxBounces}, Speed Mult: {modifierData.bounceSpeedMultiplier}x, Damage Mult: {modifierData.bounceDamageMultiplier}x)");
            }
            if (modifierData.phaseRounds)
            {
                Debug.Log($"  - Phase enabled (Transparency: {modifierData.phaseTransparency})");
                ApplyPhaseEffect();
            }
            if (modifierData.incendiaryRounds)
            {
                Debug.Log($"  - Incendiary enabled (Duration: {modifierData.incendiaryDuration}s, DPS: {modifierData.incendiaryDamagePerSecond})");
            }
            if (modifierData.disorientationRounds)
            {
                Debug.Log($"  - Disorientation enabled (Duration: {modifierData.disorientationDuration}s, Spin Speed: {modifierData.disorientationSpinSpeed}°/s)");
            }
            if (modifierData.stunRounds)
            {
                Debug.Log($"  - Stun enabled (Duration: {modifierData.stunDuration}s, Freeze Anim: {modifierData.stunFreezeAnimation}, Disable AI: {modifierData.stunDisableAI})");
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

        // TELEPORT EFFECT - Apply FIRST before other effects!
        // This way if you combine teleport + gravity, they teleport first, THEN float up
        if (modifierData.teleportRounds)
        {
            ApplyTeleport(collision);
        }

        // RICOCHET CHECK - If enabled and bounces remaining, bounce instead of applying effects
        if (modifierData.ricochetRounds && bouncesRemaining > 0)
        {
            // Check if we hit a valid target (enemy or NPC) - if so, don't ricochet, just apply effects
            if (IsValidTarget(collision.collider.gameObject))
            {
                Debug.Log($"[BulletModifier] Hit target - applying effects without ricochet");
                bouncesRemaining = 0; // Use up bounces
            }
            else
            {
                // Hit a surface - ricochet!
                ApplyRicochet(collision);
                return; // Don't apply other effects yet - wait until after bounce(s)
            }
        }

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

        // GRAVITY WELL EFFECT
        if (modifierData.gravityWellRounds)
        {
            ApplyGravityWell(collision);
        }

        // INCENDIARY EFFECT
        if (modifierData.incendiaryRounds)
        {
            ApplyIncendiary(collision);
        }

        // DISORIENTATION EFFECT
        if (modifierData.disorientationRounds)
        {
            ApplyDisorientation(collision);
        }

        // STUN EFFECT
        if (modifierData.stunRounds)
        {
            ApplyStun(collision);
        }
    }

    /// <summary>
    /// Apply anti-gravity effect to the hit target
    /// Now works WITHOUT Rigidbody - just moves the transform
    /// Affects objects on BOTH "Enemies" AND "NPCs" layers
    /// </summary>
    private void ApplyAntiGravity(Collision collision)
    {
        if (collision.collider == null) return;

        GameObject target = collision.collider.gameObject;

        // Check if target is on a valid layer (Enemies OR NPCs)
        if (!IsValidTarget(target))
        {
            Debug.Log($"[BulletModifier] Skipping anti-gravity on {target.name} - not on Enemies or NPCs layer");
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
            Debug.Log($"[BulletModifier] Refreshed existing anti-gravity effect");
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
            Debug.Log($"[BulletModifier] Added new anti-gravity effect");
        }
    }

    /// <summary>
    /// Apply cryo/freeze effect to the hit target
    /// Freezes target in place
    /// Affects objects on BOTH "Enemies" AND "NPCs" layers
    /// </summary>
    private void ApplyCryo(Collision collision)
    {
        if (collision.collider == null) return;

        GameObject target = collision.collider.gameObject;

        // Check if target is on a valid layer (Enemies OR NPCs)
        if (!IsValidTarget(target))
        {
            Debug.Log($"[BulletModifier] Skipping cryo on {target.name} - not on Enemies or NPCs layer");
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
    /// Damages all enemies AND NPCs in radius and applies physics force
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

        int targetsHit = 0;

        // Track which root transforms we've already processed (avoid duplicate effects)
        HashSet<Transform> processedRoots = new HashSet<Transform>();

        foreach (Collider hitCollider in hitColliders)
        {
            // Only damage objects on valid layers (Enemies OR NPCs)
            if (!IsValidTarget(hitCollider.gameObject))
            {
                continue;
            }

            // Get root transform to avoid processing same target multiple times
            Transform rootTransform = hitCollider.transform.root;

            // Skip if we already processed this target
            if (processedRoots.Contains(rootTransform))
            {
                continue;
            }
            processedRoots.Add(rootTransform);

            // Calculate distance-based damage falloff
            float distance = Vector3.Distance(explosionPos, hitCollider.transform.position);
            float damageMultiplier = 1f - (distance / modifierData.explosionRadius);
            float finalDamage = explosionDamage * damageMultiplier;

            // Apply damage
            EnemyHealth enemyHealth = hitCollider.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(finalDamage);
                targetsHit++;
                Debug.Log($"[BulletModifier] Explosion hit {hitCollider.gameObject.name} for {finalDamage} damage (distance: {distance:F1})");
            }

            // APPLY TELEPORT IF ENABLED!
            if (modifierData.teleportRounds)
            {
                Debug.Log($"[BulletModifier] Applying teleport to explosion victim {rootTransform.name}");
                ApplyTeleportToTarget(rootTransform);
            }

            // Apply knockback if enabled (no Rigidbody needed!)
            if (modifierData.applyExplosionKnockback)
            {
                // Calculate knockback direction (away from explosion)
                Vector3 knockbackDirection = (rootTransform.position - explosionPos).normalized;

                // Use full knockback force (NOT reduced by distance - damage falloff is enough!)
                float knockbackStrength = modifierData.explosionKnockbackForce;

                // Get or add KnockbackHandler component on the target (not the bullet!)
                KnockbackHandler knockbackHandler = rootTransform.GetComponent<KnockbackHandler>();
                if (knockbackHandler == null)
                {
                    knockbackHandler = rootTransform.gameObject.AddComponent<KnockbackHandler>();
                }

                // Apply knockback (handler will check if already being knocked back)
                knockbackHandler.ApplyKnockback(knockbackDirection, knockbackStrength, modifierData.explosionKnockbackDuration);

                Debug.Log($"[BulletModifier] Applied knockback to {rootTransform.name} (force: {knockbackStrength:F1}, direction: {knockbackDirection})");
            }

            // APPLY ANTI-GRAVITY IF ENABLED!
            if (modifierData.antiGravityRounds)
            {
                Debug.Log($"[BulletModifier] Applying anti-gravity to explosion victim {rootTransform.name}");

                // Check if target already has an anti-gravity effect component
                AntiGravityEffect existingEffect = rootTransform.GetComponent<AntiGravityEffect>();

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
                    Debug.Log($"[BulletModifier] Refreshed anti-gravity on explosion victim");
                }
                else
                {
                    // Add new anti-gravity effect component to the target
                    AntiGravityEffect effect = rootTransform.gameObject.AddComponent<AntiGravityEffect>();
                    effect.Initialize(
                        modifierData.antiGravityForce,
                        modifierData.antiGravityDuration,
                        modifierData.disableGravity,
                        modifierData.initialUpwardImpulse,
                        modifierData.bobbingAmount,
                        modifierData.bobbingSpeed
                    );
                    Debug.Log($"[BulletModifier] Added anti-gravity to explosion victim");
                }
            }

            // APPLY CRYO IF ENABLED!
            if (modifierData.cryoRounds)
            {
                Debug.Log($"[BulletModifier] Applying cryo to explosion victim {rootTransform.name}");

                CryoEffect existingCryo = rootTransform.GetComponent<CryoEffect>();

                if (existingCryo != null)
                {
                    existingCryo.RefreshEffect(
                        modifierData.cryoDuration,
                        modifierData.freezeAnimation,
                        modifierData.freezeTintColor,
                        modifierData.freezeTintStrength,
                        modifierData.iceEffectPrefab
                    );
                }
                else
                {
                    CryoEffect effect = rootTransform.gameObject.AddComponent<CryoEffect>();
                    effect.Initialize(
                        modifierData.cryoDuration,
                        modifierData.freezeAnimation,
                        modifierData.freezeTintColor,
                        modifierData.freezeTintStrength,
                        modifierData.iceEffectPrefab
                    );
                }
            }

            // APPLY INCENDIARY IF ENABLED!
            if (modifierData.incendiaryRounds)
            {
                Debug.Log($"[BulletModifier] Applying incendiary to explosion victim {rootTransform.name}");

                IncendiaryEffect existingIncendiary = rootTransform.GetComponent<IncendiaryEffect>();

                if (existingIncendiary != null)
                {
                    existingIncendiary.RefreshEffect(
                        modifierData.incendiaryDuration,
                        modifierData.incendiaryDamagePerSecond,
                        modifierData.incendiaryTickInterval,
                        modifierData.incendiarySpreadEnabled,
                        modifierData.incendiarySpreadRadius,
                        modifierData.incendiarySpreadChance,
                        modifierData.incendiaryFireEffectPrefab,
                        modifierData.incendiaryTintColor,
                        modifierData.incendiaryTintStrength,
                        modifierData.incendiaryAddEmission,
                        modifierData.incendiaryEmissionIntensity
                    );
                }
                else
                {
                    IncendiaryEffect effect = rootTransform.gameObject.AddComponent<IncendiaryEffect>();
                    effect.Initialize(
                        modifierData.incendiaryDuration,
                        modifierData.incendiaryDamagePerSecond,
                        modifierData.incendiaryTickInterval,
                        modifierData.incendiarySpreadEnabled,
                        modifierData.incendiarySpreadRadius,
                        modifierData.incendiarySpreadChance,
                        modifierData.incendiaryFireEffectPrefab,
                        modifierData.incendiaryTintColor,
                        modifierData.incendiaryTintStrength,
                        modifierData.incendiaryAddEmission,
                        modifierData.incendiaryEmissionIntensity
                    );
                }
            }

            // APPLY DISORIENTATION IF ENABLED!
            if (modifierData.disorientationRounds)
            {
                Debug.Log($"[BulletModifier] Applying disorientation (SPINBOT) to explosion victim {rootTransform.name}");

                DisorientationEffect existingDisorientation = rootTransform.GetComponent<DisorientationEffect>();

                if (existingDisorientation != null)
                {
                    existingDisorientation.RefreshEffect(
                        modifierData.disorientationDuration,
                        modifierData.disorientationSpinSpeed,
                        modifierData.disorientationEffectPrefab,
                        modifierData.showDisorientationDebug
                    );
                }
                else
                {
                    DisorientationEffect effect = rootTransform.gameObject.AddComponent<DisorientationEffect>();
                    effect.Initialize(
                        modifierData.disorientationDuration,
                        modifierData.disorientationSpinSpeed,
                        modifierData.disorientationEffectPrefab,
                        modifierData.showDisorientationDebug
                    );
                }
            }

            // APPLY STUN IF ENABLED!
            if (modifierData.stunRounds)
            {
                Debug.Log($"[BulletModifier] Applying stun to explosion victim {rootTransform.name}");

                StunEffect existingStun = rootTransform.GetComponent<StunEffect>();

                if (existingStun != null)
                {
                    existingStun.RefreshEffect(
                        modifierData.stunDuration,
                        modifierData.stunFreezeAnimation,
                        modifierData.stunDisableAI,
                        modifierData.stunTintColor,
                        modifierData.stunTintStrength,
                        modifierData.stunEffectPrefab,
                        modifierData.showStunDebug
                    );
                }
                else
                {
                    StunEffect effect = rootTransform.gameObject.AddComponent<StunEffect>();
                    effect.Initialize(
                        modifierData.stunDuration,
                        modifierData.stunFreezeAnimation,
                        modifierData.stunDisableAI,
                        modifierData.stunTintColor,
                        modifierData.stunTintStrength,
                        modifierData.stunEffectPrefab,
                        modifierData.showStunDebug
                    );
                }
            }
        }

        Debug.Log($"[BulletModifier] Explosion hit {targetsHit} targets");

        // Debug visualization
        if (modifierData.showExplosionRadius)
        {
            Debug.DrawLine(explosionPos, explosionPos + Vector3.up * modifierData.explosionRadius, Color.red, 2f);
        }
    }

    /// <summary>
    /// Create gravity well at impact point that pulls enemies AND NPCs toward center
    /// </summary>
    private void ApplyGravityWell(Collision collision)
    {
        if (collision.contacts.Length == 0) return;

        Vector3 wellPosition = collision.contacts[0].point;

        Debug.Log($"[BulletModifier] Creating gravity well at {wellPosition} with radius {modifierData.gravityWellRadius}");

        // Create gravity well GameObject
        GameObject wellObject = new GameObject("GravityWell");
        wellObject.transform.position = wellPosition;

        // Add the gravity well component
        GravityWellEffect wellEffect = wellObject.AddComponent<GravityWellEffect>();
        wellEffect.Initialize(
            wellPosition,
            modifierData.gravityWellRadius,
            modifierData.gravityWellDuration,
            modifierData.gravityWellStrength,
            modifierData.gravityWellDamagePerSecond,
            modifierData.gravityWellEffectPrefab,
            modifierData.showGravityWellDebug
        );

        // Destroy after duration
        Destroy(wellObject, modifierData.gravityWellDuration);
    }

    /// <summary>
    /// Teleport enemy OR NPC to random nearby location (with collision checks)
    /// </summary>
    private void ApplyTeleport(Collision collision)
    {
        if (collision.collider == null) return;

        GameObject target = collision.collider.gameObject;

        // Check if target is on a valid layer (Enemies OR NPCs)
        if (!IsValidTarget(target))
        {
            Debug.Log($"[BulletModifier] Skipping teleport on {target.name} - not on Enemies or NPCs layer");
            return;
        }

        // Get root transform (whole target, not just a body part)
        Transform rootTransform = target.transform.root;

        // Use helper method
        ApplyTeleportToTarget(rootTransform);
    }

    /// <summary>
    /// Apply teleport to a specific target transform
    /// Helper method used by both direct hits and explosion hits
    /// </summary>
    private void ApplyTeleportToTarget(Transform rootTransform)
    {
        Vector3 originalPosition = rootTransform.position;

        Debug.Log($"[BulletModifier] Attempting to teleport {rootTransform.name} from {originalPosition}");

        // Spawn departure effect
        if (modifierData.teleportDepartureEffectPrefab != null)
        {
            GameObject departureEffect = Instantiate(modifierData.teleportDepartureEffectPrefab, originalPosition, Quaternion.identity);
            Destroy(departureEffect, 3f);
            Debug.Log($"[BulletModifier] Spawned departure effect at {originalPosition}");
        }

        // Try to find valid teleport location
        Vector3 teleportPosition;
        bool foundValidPosition = FindValidTeleportPosition(originalPosition, out teleportPosition);

        if (foundValidPosition)
        {
            // Check if anti-gravity is active
            AntiGravityEffect antiGrav = rootTransform.GetComponent<AntiGravityEffect>();
            bool antiGravActive = (antiGrav != null);

            // Get NavMeshAgent
            UnityEngine.AI.NavMeshAgent navAgent = rootTransform.GetComponent<UnityEngine.AI.NavMeshAgent>();

            if (antiGravActive)
            {
                // Anti-gravity active - just move transform directly (they're floating anyway)
                Debug.Log($"[BulletModifier] Anti-gravity active - teleporting transform directly");

                rootTransform.position = teleportPosition;

                // Update anti-gravity to use new position as ground
                antiGrav.UpdateGroundPosition(teleportPosition);

                Debug.Log($"[BulletModifier] Teleported {rootTransform.name} to {teleportPosition} (direct transform)");

                // Spawn arrival effect
                if (modifierData.teleportArrivalEffectPrefab != null)
                {
                    GameObject arrivalEffect = Instantiate(modifierData.teleportArrivalEffectPrefab, teleportPosition, Quaternion.identity);
                    Destroy(arrivalEffect, 3f);
                    Debug.Log($"[BulletModifier] Spawned arrival effect at {teleportPosition}");
                }

                // Debug visualization
                if (modifierData.showTeleportDebug)
                {
                    Debug.DrawLine(originalPosition, teleportPosition, Color.magenta, 5f);
                    Debug.DrawLine(teleportPosition, teleportPosition + Vector3.up * 3f, Color.cyan, 5f);
                }
            }
            else if (navAgent != null && navAgent.isOnNavMesh)
            {
                // No anti-gravity - use NavMesh for safe teleport
                UnityEngine.AI.NavMeshHit navHit;
                if (UnityEngine.AI.NavMesh.SamplePosition(teleportPosition, out navHit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    // Teleport using NavMeshAgent.Warp (safe method)
                    navAgent.Warp(navHit.position);

                    Debug.Log($"[BulletModifier] Teleported {rootTransform.name} to {navHit.position}");

                    // Spawn arrival effect
                    if (modifierData.teleportArrivalEffectPrefab != null)
                    {
                        GameObject arrivalEffect = Instantiate(modifierData.teleportArrivalEffectPrefab, navHit.position, Quaternion.identity);
                        Destroy(arrivalEffect, 3f);
                        Debug.Log($"[BulletModifier] Spawned arrival effect at {navHit.position}");
                    }

                    // Debug visualization
                    if (modifierData.showTeleportDebug)
                    {
                        Debug.DrawLine(originalPosition, navHit.position, Color.magenta, 5f);
                        Debug.DrawLine(navHit.position, navHit.position + Vector3.up * 3f, Color.cyan, 5f);
                    }
                }
                else
                {
                    Debug.LogWarning($"[BulletModifier] No NavMesh found near teleport position {teleportPosition}");
                }
            }
            else
            {
                Debug.LogWarning($"[BulletModifier] Target {rootTransform.name} has no NavMeshAgent or not on NavMesh");
            }
        }
        else
        {
            Debug.LogWarning($"[BulletModifier] Failed to find valid teleport position for {rootTransform.name} after {modifierData.teleportMaxAttempts} attempts");
        }
    }

    /// <summary>
    /// Find a valid teleport position that isn't inside objects
    /// </summary>
    private bool FindValidTeleportPosition(Vector3 originalPosition, out Vector3 validPosition)
    {
        validPosition = originalPosition;

        for (int i = 0; i < modifierData.teleportMaxAttempts; i++)
        {
            // Generate random point within distance range (horizontal only - no vertical!)
            Vector2 randomCircle = Random.insideUnitCircle.normalized;
            float randomDistance = Random.Range(modifierData.teleportMinDistance, modifierData.teleportMaxDistance);

            Vector3 randomOffset = new Vector3(randomCircle.x, 0f, randomCircle.y) * randomDistance;
            Vector3 candidatePosition = originalPosition + randomOffset;

            // Check if position is valid (not inside objects)
            if (IsPositionValid(candidatePosition))
            {
                validPosition = candidatePosition;
                Debug.Log($"[BulletModifier] Found valid position on attempt {i + 1}: {validPosition}");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if a position is valid (not inside walls/objects)
    /// </summary>
    private bool IsPositionValid(Vector3 position)
    {
        // Check sphere around position for collisions
        Collider[] colliders = Physics.OverlapSphere(position, modifierData.teleportCheckRadius);

        foreach (Collider col in colliders)
        {
            // Ignore valid targets (we want to check for walls/obstacles, not other targets)
            if (IsValidTarget(col.gameObject))
            {
                continue;
            }

            // If we hit something that's not a valid target, position is invalid
            Debug.Log($"[BulletModifier] Position {position} invalid - colliding with {col.gameObject.name} on layer {LayerMask.LayerToName(col.gameObject.layer)}");
            return false;
        }

        // Position is valid - no obstacles detected
        Debug.Log($"[BulletModifier] Position {position} is valid - clear of obstacles");
        return true;
    }

    /// <summary>
    /// Apply incendiary/fire effect to the hit target
    /// Burns target over time dealing damage
    /// Affects objects on BOTH "Enemies" AND "NPCs" layers
    /// </summary>
    private void ApplyIncendiary(Collision collision)
    {
        if (collision.collider == null) return;

        GameObject target = collision.collider.gameObject;

        // Check if target is on a valid layer (Enemies OR NPCs)
        if (!IsValidTarget(target))
        {
            Debug.Log($"[BulletModifier] Skipping incendiary on {target.name} - not on Enemies or NPCs layer");
            return;
        }

        // Get root transform
        Transform rootTransform = target.transform.root;

        Debug.Log($"[BulletModifier] Applying incendiary effect to {rootTransform.name}");

        // Check if target already has an incendiary effect component
        IncendiaryEffect existingEffect = rootTransform.GetComponent<IncendiaryEffect>();

        if (existingEffect != null)
        {
            // Refresh the existing effect
            existingEffect.RefreshEffect(
                modifierData.incendiaryDuration,
                modifierData.incendiaryDamagePerSecond,
                modifierData.incendiaryTickInterval,
                modifierData.incendiarySpreadEnabled,
                modifierData.incendiarySpreadRadius,
                modifierData.incendiarySpreadChance,
                modifierData.incendiaryFireEffectPrefab,
                modifierData.incendiaryTintColor,
                modifierData.incendiaryTintStrength,
                modifierData.incendiaryAddEmission,
                modifierData.incendiaryEmissionIntensity
            );
            Debug.Log($"[BulletModifier] Refreshed existing incendiary effect");
        }
        else
        {
            // Add new incendiary effect component to the target
            IncendiaryEffect effect = rootTransform.gameObject.AddComponent<IncendiaryEffect>();
            effect.Initialize(
                modifierData.incendiaryDuration,
                modifierData.incendiaryDamagePerSecond,
                modifierData.incendiaryTickInterval,
                modifierData.incendiarySpreadEnabled,
                modifierData.incendiarySpreadRadius,
                modifierData.incendiarySpreadChance,
                modifierData.incendiaryFireEffectPrefab,
                modifierData.incendiaryTintColor,
                modifierData.incendiaryTintStrength,
                modifierData.incendiaryAddEmission,
                modifierData.incendiaryEmissionIntensity
            );
            Debug.Log($"[BulletModifier] Added new incendiary effect");
        }
    }

    /// <summary>
    /// Apply disorientation effect to the hit target
    /// Makes target spin rapidly (360s) like a spinbot while maintaining normal movement
    /// Affects objects on BOTH "Enemies" AND "NPCs" layers
    /// </summary>
    private void ApplyDisorientation(Collision collision)
    {
        if (collision.collider == null) return;

        GameObject target = collision.collider.gameObject;

        // Check if target is on a valid layer (Enemies OR NPCs)
        if (!IsValidTarget(target))
        {
            Debug.Log($"[BulletModifier] Skipping disorientation on {target.name} - not on Enemies or NPCs layer");
            return;
        }

        // Get root transform
        Transform rootTransform = target.transform.root;

        Debug.Log($"[BulletModifier] Applying disorientation (SPINBOT) effect to {rootTransform.name}");

        // Check if target already has a disorientation effect component
        DisorientationEffect existingEffect = rootTransform.GetComponent<DisorientationEffect>();

        if (existingEffect != null)
        {
            // Refresh the existing effect
            existingEffect.RefreshEffect(
                modifierData.disorientationDuration,
                modifierData.disorientationSpinSpeed,
                modifierData.disorientationEffectPrefab,
                modifierData.showDisorientationDebug
            );
            Debug.Log($"[BulletModifier] Refreshed existing disorientation effect");
        }
        else
        {
            // Add new disorientation effect component to the target
            DisorientationEffect effect = rootTransform.gameObject.AddComponent<DisorientationEffect>();
            effect.Initialize(
                modifierData.disorientationDuration,
                modifierData.disorientationSpinSpeed,
                modifierData.disorientationEffectPrefab,
                modifierData.showDisorientationDebug
            );
            Debug.Log($"[BulletModifier] Added new disorientation effect");
        }
    }

    /// <summary>
    /// Apply stun effect to the hit target
    /// Stops all movement, animation, and AI behavior
    /// Affects objects on BOTH "Enemies" AND "NPCs" layers
    /// </summary>
    private void ApplyStun(Collision collision)
    {
        if (collision.collider == null) return;

        GameObject target = collision.collider.gameObject;

        // Check if target is on a valid layer (Enemies OR NPCs)
        if (!IsValidTarget(target))
        {
            Debug.Log($"[BulletModifier] Skipping stun on {target.name} - not on Enemies or NPCs layer");
            return;
        }

        // Get root transform
        Transform rootTransform = target.transform.root;

        Debug.Log($"[BulletModifier] Applying stun effect to {rootTransform.name}");

        // Check if target already has a stun effect component
        StunEffect existingEffect = rootTransform.GetComponent<StunEffect>();

        if (existingEffect != null)
        {
            // Refresh the existing effect
            existingEffect.RefreshEffect(
                modifierData.stunDuration,
                modifierData.stunFreezeAnimation,
                modifierData.stunDisableAI,
                modifierData.stunTintColor,
                modifierData.stunTintStrength,
                modifierData.stunEffectPrefab,
                modifierData.showStunDebug
            );
            Debug.Log($"[BulletModifier] Refreshed existing stun effect");
        }
        else
        {
            // Add new stun effect component to the target
            StunEffect effect = rootTransform.gameObject.AddComponent<StunEffect>();
            effect.Initialize(
                modifierData.stunDuration,
                modifierData.stunFreezeAnimation,
                modifierData.stunDisableAI,
                modifierData.stunTintColor,
                modifierData.stunTintStrength,
                modifierData.stunEffectPrefab,
                modifierData.showStunDebug
            );
            Debug.Log($"[BulletModifier] Added new stun effect");
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

    /// <summary>
    /// Apply phase effect to bullet - visual changes
    /// Makes bullet semi-transparent and glowy
    /// </summary>
    private void ApplyPhaseEffect()
    {
        Debug.Log($"[BulletModifier] Applying phase effect to bullet");

        // Make bullet semi-transparent
        if (modifierData.makeTransparent)
        {
            Renderer bulletRenderer = GetComponent<Renderer>();
            if (bulletRenderer != null && bulletRenderer.material != null)
            {
                Material mat = bulletRenderer.material;

                // Set material to transparent mode
                mat.SetFloat("_Mode", 3); // Transparent mode
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;

                // Set transparency
                Color color = mat.color;
                color.a = modifierData.phaseTransparency;
                mat.color = color;

                // Add phase glow
                mat.EnableKeyword("_EMISSION");
                Color emissionColor = modifierData.phaseGlowColor * 2f; // HDR glow
                mat.SetColor("_EmissionColor", emissionColor);

                Debug.Log($"[BulletModifier] Set bullet transparency to {modifierData.phaseTransparency}");
            }
        }

        // Add phase particle effect
        if (modifierData.phaseEffectPrefab != null)
        {
            GameObject phaseEffect = Instantiate(modifierData.phaseEffectPrefab, transform.position, Quaternion.identity, transform);
            Debug.Log($"[BulletModifier] Added phase particle effect");
        }
    }

    /// <summary>
    /// Setup collision to only hit valid targets (Enemies AND NPCs)
    /// Called in Start after Initialize
    /// </summary>
    private void SetupPhaseCollision()
    {
        // Get the bullet's collider
        Collider bulletCollider = GetComponent<Collider>();
        if (bulletCollider == null)
        {
            Debug.LogWarning("[BulletModifier] No collider found on bullet! Phase rounds need a collider.");
            return;
        }

        // Get layer indices
        int enemyLayer = LayerMask.NameToLayer("Enemies");
        int npcLayer = LayerMask.NameToLayer("NPCs");

        // Iterate through all layers (0-31)
        for (int i = 0; i < 32; i++)
        {
            // Ignore collision with all layers except Enemies and NPCs
            if (i != enemyLayer && i != npcLayer)
            {
                Physics.IgnoreLayerCollision(gameObject.layer, i, true);
            }
        }

        Debug.Log($"[BulletModifier] Phase collision setup - bullet will only collide with Enemies and NPCs layers");

        // Debug visualization
        if (modifierData.showPhaseDebug)
        {
            // Draw a trail behind the bullet
            StartCoroutine(DrawPhaseDebugTrail());
        }
    }

    /// <summary>
    /// Draw debug trail for phase bullets
    /// </summary>
    private IEnumerator DrawPhaseDebugTrail()
    {
        Vector3 lastPos = transform.position;

        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            if (transform != null)
            {
                Debug.DrawLine(lastPos, transform.position, Color.cyan, 2f);
                lastPos = transform.position;
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Apply ricochet effect - bounce bullet off surface
    /// </summary>
    private void ApplyRicochet(Collision collision)
    {
        if (collision.contacts.Length == 0) return;

        ContactPoint contact = collision.contacts[0];
        Vector3 incomingDirection = transform.forward;
        Vector3 normal = contact.normal;

        // Calculate reflection direction
        Vector3 reflectDirection = Vector3.Reflect(incomingDirection, normal);

        Debug.Log($"[BulletModifier] Ricochet! Bounces remaining: {bouncesRemaining}");

        // Spawn spark effect
        if (modifierData.ricochetSparkPrefab != null)
        {
            GameObject spark = Instantiate(modifierData.ricochetSparkPrefab, contact.point, Quaternion.LookRotation(normal));
            Destroy(spark, 2f);
            Debug.Log($"[BulletModifier] Spawned ricochet spark");
        }

        // Play ricochet sound
        if (modifierData.ricochetSound != null)
        {
            AudioSource.PlayClipAtPoint(modifierData.ricochetSound, contact.point);
            Debug.Log($"[BulletModifier] Played ricochet sound");
        }

        // Update bullet direction and velocity
        transform.forward = reflectDirection;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Apply speed multiplier
            float newSpeed = rb.velocity.magnitude * modifierData.bounceSpeedMultiplier;
            rb.velocity = reflectDirection * newSpeed;
            Debug.Log($"[BulletModifier] New velocity: {rb.velocity.magnitude}");
        }

        // Update damage multiplier
        currentDamageMultiplier *= modifierData.bounceDamageMultiplier;
        bouncesRemaining--;

        Debug.Log($"[BulletModifier] Damage multiplier now: {currentDamageMultiplier}x, Bounces remaining: {bouncesRemaining}");

        // Debug visualization
        if (modifierData.showRicochetDebug)
        {
            Debug.DrawRay(contact.point, normal * 2f, Color.yellow, 2f);
            Debug.DrawRay(contact.point, reflectDirection * 3f, Color.green, 2f);
            Debug.DrawRay(contact.point, -incomingDirection * 3f, Color.red, 2f);
        }
    }

    /// <summary>
    /// Get the current damage multiplier (for Bullet script to use)
    /// </summary>
    public float GetCurrentDamageMultiplier()
    {
        return currentDamageMultiplier;
    }

    /// <summary>
    /// Check if bullet should be destroyed on this hit (false if ricocheting)
    /// </summary>
    public bool ShouldDestroyOnHit(Collision collision)
    {
        // If ricochet is enabled and we have bounces remaining
        if (modifierData != null && modifierData.ricochetRounds && bouncesRemaining > 0)
        {
            // Check if we hit a valid target - if so, destroy (don't ricochet off targets)
            if (IsValidTarget(collision.collider.gameObject))
            {
                return true; // Destroy on target hit
            }

            // Hit a surface - don't destroy, we're going to ricochet
            return false;
        }

        // No ricochet or no bounces left - destroy normally
        return true;
    }
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

        // Check if knockback is active (Rigidbody present)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.Log($"[AntiGravityEffect] Found Rigidbody - will use physics-based anti-gravity on {gameObject.name}");
            // Don't disable NavMeshAgent - knockback already did it
            return;
        }

        // No Rigidbody - use transform-based movement
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

    /// <summary>
    /// Update the ground position (used when enemy is teleported while floating)
    /// </summary>
    public void UpdateGroundPosition(Vector3 newGroundPosition)
    {
        groundPosition = newGroundPosition;
        targetPosition = newGroundPosition + Vector3.up * liftHeight;

        Debug.Log($"[AntiGravityEffect] Updated ground position to {newGroundPosition}, new target: {targetPosition}");
    }

    private void Update()
    {
        // Check if there's an active Rigidbody (from knockback)
        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            Debug.Log($"[AntiGravityEffect] UPDATE: Found RB on {gameObject.name}, isKinematic: {rb.isKinematic}, useGravity: {rb.useGravity}");

            // Make sure it's active for anti-gravity
            if (rb.isKinematic)
            {
                rb.isKinematic = false;
                rb.useGravity = false; // Disable Unity's gravity, we'll control it
                Debug.Log($"[AntiGravityEffect] Re-enabled Rigidbody for anti-gravity on {gameObject.name}");
            }

            // Check if knockback is still active
            KnockbackHandler knockback = GetComponent<KnockbackHandler>();
            if (knockback != null && knockback.IsBeingKnockedBack)
            {
                Debug.Log($"[AntiGravityEffect] Knockback still active - WAITING");
                // Knockback is active - don't interfere! Just wait
                // Only disable gravity so they fly higher
                rb.useGravity = false;

                timeRemaining -= Time.deltaTime;
                return; // Let knockback handle movement
            }

            Debug.Log($"[AntiGravityEffect] Knockback finished - TAKING OVER, currentY: {transform.position.y}, targetY: {groundPosition.y + liftHeight}");

            // Knockback finished or not present - anti-gravity takes over
            rb.useGravity = false; // Keep gravity off

            // Use Rigidbody but mimic the transform-based behavior
            float currentHeight = transform.position.y;
            float targetHeight = groundPosition.y + liftHeight;

            // Damp velocity to stop it at target height
            rb.velocity = new Vector3(rb.velocity.x * 0.95f, rb.velocity.y * 0.9f, rb.velocity.z * 0.95f);

            if (isLifting)
            {
                // Still lifting - apply force toward target
                if (currentHeight < targetHeight - 0.5f)
                {
                    // Not at target yet - apply upward force
                    float forceNeeded = (targetHeight - currentHeight) * 50f; // Proportional force
                    forceNeeded = Mathf.Clamp(forceNeeded, 0f, 300f); // Cap it
                    rb.AddForce(Vector3.up * forceNeeded, ForceMode.Force);
                    Debug.Log($"[AntiGravityEffect] LIFTING - Applied {forceNeeded}N upward");
                }
                else
                {
                    // Reached target - start floating
                    isLifting = false;
                    rb.velocity = Vector3.zero; // Stop movement
                    Debug.Log($"[AntiGravityEffect] {gameObject.name} reached target height with Rigidbody, now floating");
                }
            }
            else
            {
                // Floating - apply bobbing force
                float bob = Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount;
                float bobTargetY = targetHeight + bob;
                float heightDiff = bobTargetY - currentHeight;

                // Apply gentle force to maintain bobbing
                rb.AddForce(Vector3.up * heightDiff * 20f, ForceMode.Force);
                Debug.Log($"[AntiGravityEffect] BOBBING - heightDiff: {heightDiff}");
            }

            // Reduce time
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                // Effect over - re-enable gravity and let them fall
                rb.useGravity = true;
                Debug.Log($"[AntiGravityEffect] Duration expired, re-enabling gravity");
                Destroy(this);
            }
            return;
        }

        Debug.LogWarning($"[AntiGravityEffect] NO RIGIDBODY FOUND on {gameObject.name} - using transform movement");

        // Original transform-based movement (when no knockback active)
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

/// <summary>
/// Component that creates a gravity well that pulls enemies AND NPCs toward a point
/// Does NOT require Rigidbody - uses transform-based movement
/// </summary>
public class GravityWellEffect : MonoBehaviour
{
    private Vector3 wellCenter;
    private float radius;
    private float duration;
    private float strength;
    private float damagePerSecond;
    private bool showDebug;

    private float timeRemaining;
    private GameObject visualEffect;
    private List<Transform> affectedTargets = new List<Transform>();

    public void Initialize(Vector3 center, float wellRadius, float wellDuration, float pullStrength, float dps, GameObject effectPrefab, bool debug)
    {
        wellCenter = center;
        radius = wellRadius;
        duration = wellDuration;
        strength = pullStrength;
        damagePerSecond = dps;
        showDebug = debug;
        timeRemaining = duration;

        Debug.Log($"[GravityWellEffect] Created at {wellCenter} - Radius: {radius}, Duration: {duration}s, Strength: {strength}");

        // Spawn visual effect
        if (effectPrefab != null)
        {
            visualEffect = Instantiate(effectPrefab, wellCenter, Quaternion.identity);
            visualEffect.transform.localScale = Vector3.one * radius * 2f; // Scale to match radius
            Debug.Log($"[GravityWellEffect] Spawned visual effect");
        }
    }

    private void Update()
    {
        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            EndEffect();
            return;
        }

        // Find all valid targets in radius (Enemies AND NPCs)
        Collider[] targets = Physics.OverlapSphere(wellCenter, radius, LayerMask.GetMask("Enemies", "NPCs"));

        affectedTargets.Clear();

        // Track which root transforms we've already processed
        HashSet<Transform> processedRoots = new HashSet<Transform>();

        foreach (Collider target in targets)
        {
            if (target == null) continue;

            // Get the root transform (top-level parent)
            Transform rootTransform = target.transform.root;

            // Skip if we've already processed this root
            if (processedRoots.Contains(rootTransform))
            {
                continue;
            }

            processedRoots.Add(rootTransform);
            affectedTargets.Add(rootTransform);

            // Calculate pull direction and distance (use root position)
            Vector3 directionToCenter = (wellCenter - rootTransform.position).normalized;
            float distanceToCenter = Vector3.Distance(rootTransform.position, wellCenter);

            // Calculate pull strength (stronger closer to center, weaker at edges)
            float distanceMultiplier = 1f - (distanceToCenter / radius);
            float pullForce = strength * distanceMultiplier * Time.deltaTime;

            // Pull ONLY the root transform (this moves the entire target hierarchy)
            rootTransform.position += directionToCenter * pullForce;

            // Apply damage if enabled (get health from the collider we hit, not the root)
            if (damagePerSecond > 0f)
            {
                EnemyHealth health = target.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    float damage = damagePerSecond * Time.deltaTime;
                    health.TakeDamage(damage);
                }
            }

            // Debug visualization
            if (showDebug)
            {
                Debug.DrawLine(rootTransform.position, wellCenter, Color.magenta, Time.deltaTime);
            }
        }

        // Debug visualization - draw sphere
        if (showDebug)
        {
            DrawDebugSphere(wellCenter, radius, Color.magenta);
        }
    }

    private void EndEffect()
    {
        Debug.Log($"[GravityWellEffect] Ended at {wellCenter}");

        if (visualEffect != null)
        {
            Destroy(visualEffect);
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (visualEffect != null)
        {
            Destroy(visualEffect);
        }
    }

    /// <summary>
    /// Draw a debug sphere using lines
    /// </summary>
    private void DrawDebugSphere(Vector3 center, float sphereRadius, Color color)
    {
        // Draw 3 circles (XY, XZ, YZ planes)
        int segments = 16;
        float angleStep = 360f / segments;

        // XY plane circle
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 p1 = center + new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0) * sphereRadius;
            Vector3 p2 = center + new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0) * sphereRadius;

            Debug.DrawLine(p1, p2, color, Time.deltaTime);
        }

        // XZ plane circle
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 p1 = center + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * sphereRadius;
            Vector3 p2 = center + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * sphereRadius;

            Debug.DrawLine(p1, p2, color, Time.deltaTime);
        }

        // YZ plane circle
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 p1 = center + new Vector3(0, Mathf.Cos(angle1), Mathf.Sin(angle1)) * sphereRadius;
            Vector3 p2 = center + new Vector3(0, Mathf.Cos(angle2), Mathf.Sin(angle2)) * sphereRadius;

            Debug.DrawLine(p1, p2, color, Time.deltaTime);
        }
    }
}

/// <summary>
/// Component that applies incendiary/fire effect to targets
/// Burns them over time dealing damage per second
/// Can spread to nearby enemies AND NPCs
/// </summary>
public class IncendiaryEffect : MonoBehaviour
{
    private float duration;
    private float timeRemaining;
    private float damagePerSecond;
    private float tickInterval;
    private float nextTickTime;

    // Fire spread
    private bool spreadEnabled;
    private float spreadRadius;
    private float spreadChance;
    private float nextSpreadCheckTime;
    private const float SPREAD_CHECK_INTERVAL = 1f; // Check for spread once per second

    // Visual effects
    private GameObject fireEffectInstance;
    private Color tintColor;
    private float tintStrength;
    private bool addEmission;
    private float emissionIntensity;

    private Renderer[] renderers;
    private Dictionary<Renderer, Color[]> originalColors = new Dictionary<Renderer, Color[]>();
    private Dictionary<Renderer, Color[]> originalEmissionColors = new Dictionary<Renderer, Color[]>();

    // Track which targets we've already spread to (prevent infinite spread loops)
    private static HashSet<Transform> globalBurningTargets = new HashSet<Transform>();

    public void Initialize(float burnDuration, float dps, float tickTime, bool enableSpread, float radius, float chance,
        GameObject fireEffect, Color tint, float tintAmount, bool emission, float emissionAmount)
    {
        duration = burnDuration;
        timeRemaining = duration;
        damagePerSecond = dps;
        tickInterval = tickTime;
        nextTickTime = Time.time + tickInterval;

        spreadEnabled = enableSpread;
        spreadRadius = radius;
        spreadChance = chance;
        nextSpreadCheckTime = Time.time + SPREAD_CHECK_INTERVAL;

        tintColor = tint;
        tintStrength = tintAmount;
        addEmission = emission;
        emissionIntensity = emissionAmount;

        // Add to global burning list
        globalBurningTargets.Add(transform);

        // Apply visual effects
        ApplyFireVisuals(fireEffect);

        Debug.Log($"[IncendiaryEffect] Started on {gameObject.name} for {duration}s (DPS: {damagePerSecond})");
    }

    public void RefreshEffect(float burnDuration, float dps, float tickTime, bool enableSpread, float radius, float chance,
        GameObject fireEffect, Color tint, float tintAmount, bool emission, float emissionAmount)
    {
        duration = burnDuration;
        timeRemaining = duration; // Reset timer
        damagePerSecond = dps;
        tickInterval = tickTime;

        spreadEnabled = enableSpread;
        spreadRadius = radius;
        spreadChance = chance;

        Debug.Log($"[IncendiaryEffect] Refreshed on {gameObject.name}");
    }

    private void ApplyFireVisuals(GameObject fireEffect)
    {
        // Spawn fire particle effect
        if (fireEffect != null)
        {
            // Try to find a better position - use the renderer bounds center
            Vector3 spawnPosition = transform.position;

            // Get all renderers to find the center of the visual mesh
            Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
            if (allRenderers.Length > 0)
            {
                // Calculate the center point of all renderers (visual center of character)
                Bounds combinedBounds = allRenderers[0].bounds;
                for (int i = 1; i < allRenderers.Length; i++)
                {
                    combinedBounds.Encapsulate(allRenderers[i].bounds);
                }
                spawnPosition = combinedBounds.center;
            }

            fireEffectInstance = Instantiate(fireEffect, spawnPosition, Quaternion.identity, transform);
            Debug.Log($"[IncendiaryEffect] Spawned fire effect on {gameObject.name} at {spawnPosition}");
        }

        // Apply fire tint and emission
        renderers = GetComponentsInChildren<Renderer>();
        originalColors.Clear();
        originalEmissionColors.Clear();

        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;

            // Get a fresh copy of materials
            Material[] materials = renderer.materials;
            Color[] colors = new Color[materials.Length];
            Color[] emissionColors = new Color[materials.Length];

            for (int i = 0; i < materials.Length; i++)
            {
                // Store and apply color tint
                if (materials[i].HasProperty("_Color"))
                {
                    colors[i] = materials[i].color;
                    Color tintedColor = Color.Lerp(colors[i], tintColor, tintStrength);
                    materials[i].color = tintedColor;
                }

                // Store and apply emission
                if (addEmission && materials[i].HasProperty("_EmissionColor"))
                {
                    // Store original emission
                    if (materials[i].IsKeywordEnabled("_EMISSION"))
                    {
                        emissionColors[i] = materials[i].GetColor("_EmissionColor");
                    }
                    else
                    {
                        emissionColors[i] = Color.black;
                    }

                    // Enable and apply fire emission
                    materials[i].EnableKeyword("_EMISSION");
                    Color fireEmission = tintColor * emissionIntensity;
                    materials[i].SetColor("_EmissionColor", fireEmission);
                }
            }

            originalColors[renderer] = colors;
            originalEmissionColors[renderer] = emissionColors;

            // CRITICAL: Apply the modified materials back
            renderer.materials = materials;
        }

        Debug.Log($"[IncendiaryEffect] Applied fire visuals to {renderers.Length} renderers");
    }

    private void RemoveFireVisuals()
    {
        if (renderers == null) return;

        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;

            // Important: Get fresh material array reference
            Material[] materials = renderer.materials;

            // Restore colors
            if (originalColors.ContainsKey(renderer))
            {
                Color[] colors = originalColors[renderer];
                for (int i = 0; i < materials.Length && i < colors.Length; i++)
                {
                    if (materials[i].HasProperty("_Color"))
                    {
                        materials[i].color = colors[i];
                    }
                }
            }

            // Restore emission
            if (addEmission && originalEmissionColors.ContainsKey(renderer))
            {
                Color[] emissionColors = originalEmissionColors[renderer];
                for (int i = 0; i < materials.Length && i < emissionColors.Length; i++)
                {
                    if (materials[i].HasProperty("_EmissionColor"))
                    {
                        // If original emission was black, disable emission
                        if (emissionColors[i] == Color.black)
                        {
                            materials[i].DisableKeyword("_EMISSION");
                        }
                        else
                        {
                            materials[i].SetColor("_EmissionColor", emissionColors[i]);
                        }
                    }
                }
            }

            // CRITICAL: Reassign the modified materials back to the renderer
            renderer.materials = materials;
        }

        Debug.Log($"[IncendiaryEffect] Removed fire visuals");
    }

    private void Update()
    {
        timeRemaining -= Time.deltaTime;

        // Apply damage tick
        if (Time.time >= nextTickTime)
        {
            ApplyBurnDamage();
            nextTickTime = Time.time + tickInterval;
        }

        // Check for fire spread
        if (spreadEnabled && Time.time >= nextSpreadCheckTime)
        {
            TrySpreadFire();
            nextSpreadCheckTime = Time.time + SPREAD_CHECK_INTERVAL;
        }

        // End effect when duration expires
        if (timeRemaining <= 0f)
        {
            EndEffect();
        }
    }

    private void ApplyBurnDamage()
    {
        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health != null)
        {
            float damage = damagePerSecond * tickInterval;
            health.TakeDamage(damage);
            Debug.Log($"[IncendiaryEffect] Burned {gameObject.name} for {damage} damage");
        }
    }

    private void TrySpreadFire()
    {
        // Find nearby valid targets (Enemies AND NPCs)
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, spreadRadius, LayerMask.GetMask("Enemies", "NPCs"));

        foreach (Collider col in nearbyColliders)
        {
            if (col == null) continue;

            Transform rootTransform = col.transform.root;

            // Skip self
            if (rootTransform == transform) continue;

            // Skip if already burning
            if (globalBurningTargets.Contains(rootTransform)) continue;

            // Check if target is already on fire
            if (rootTransform.GetComponent<IncendiaryEffect>() != null) continue;

            // Random chance to spread
            if (Random.value > spreadChance) continue;

            // Spread fire!
            Debug.Log($"[IncendiaryEffect] Fire spreading from {gameObject.name} to {rootTransform.name}!");

            IncendiaryEffect newFire = rootTransform.gameObject.AddComponent<IncendiaryEffect>();
            newFire.Initialize(
                duration * 0.75f, // Spread fires last 75% as long
                damagePerSecond,
                tickInterval,
                spreadEnabled, // Can continue spreading
                spreadRadius,
                spreadChance * 0.5f, // Spread chance reduces (prevents infinite spread)
                fireEffectInstance != null ? fireEffectInstance : null,
                tintColor,
                tintStrength,
                addEmission,
                emissionIntensity
            );
        }
    }

    private void EndEffect()
    {
        Debug.Log($"[IncendiaryEffect] Ended on {gameObject.name}");

        // Remove from global burning list
        globalBurningTargets.Remove(transform);

        // Remove visuals
        RemoveFireVisuals();

        if (fireEffectInstance != null)
        {
            Destroy(fireEffectInstance);
        }

        Destroy(this);
    }

    private void OnDestroy()
    {
        // Clean up
        globalBurningTargets.Remove(transform);
        RemoveFireVisuals();

        if (fireEffectInstance != null)
        {
            Destroy(fireEffectInstance);
        }
    }
}

/// <summary>
/// Component that applies disorientation effect to targets
/// Makes them spin rapidly (360s) like a spinbot while maintaining their normal movement/behavior
/// Works with anti-gravity, explosions, teleport, etc.
/// </summary>
public class DisorientationEffect : MonoBehaviour
{
    private float duration;
    private float timeRemaining;
    private float spinSpeed;
    private bool showDebug;

    // Visual effects
    private GameObject visualEffectInstance;

    // We DON'T disable NavMeshAgent or movement - we just spin the visual model
    // This creates the "spinbot" effect where they move normally but spin like crazy

    public void Initialize(float spinDuration, float rotationSpeed, GameObject effectPrefab, bool debug)
    {
        duration = spinDuration;
        timeRemaining = duration;
        spinSpeed = rotationSpeed;
        showDebug = debug;

        // Spawn visual effect
        if (effectPrefab != null)
        {
            visualEffectInstance = Instantiate(effectPrefab, transform.position, Quaternion.identity, transform);
            Debug.Log($"[DisorientationEffect] Spawned disorientation effect on {gameObject.name}");
        }

        Debug.Log($"[DisorientationEffect] Started SPINBOT on {gameObject.name} - Spin Speed: {spinSpeed}°/s (360s while moving!)");
    }

    public void RefreshEffect(float spinDuration, float rotationSpeed, GameObject effectPrefab, bool debug)
    {
        duration = spinDuration;
        timeRemaining = duration; // Reset timer
        spinSpeed = rotationSpeed;

        Debug.Log($"[DisorientationEffect] Refreshed SPINBOT on {gameObject.name}");
    }

    private void Update()
    {
        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            EndEffect();
            return;
        }

        // SPIN THE ENTIRE TRANSFORM AROUND Y-AXIS (horizontal 360s)
        // This spins the whole target (including model, NavMeshAgent, etc.) while they continue their normal behavior
        float rotationAmount = spinSpeed * Time.deltaTime;
        transform.Rotate(Vector3.up, rotationAmount, Space.World);

        // Debug visualization
        if (showDebug)
        {
            Debug.DrawRay(transform.position, Vector3.up * 3f, Color.yellow, Time.deltaTime);
            Debug.DrawRay(transform.position, transform.forward * 2f, Color.green, Time.deltaTime);
        }
    }

    private void EndEffect()
    {
        Debug.Log($"[DisorientationEffect] SPINBOT ended on {gameObject.name}");

        // Clean up visual effect
        if (visualEffectInstance != null)
        {
            Destroy(visualEffectInstance);
        }

        Destroy(this);
    }

    private void OnDestroy()
    {
        // Clean up visual effect
        if (visualEffectInstance != null)
        {
            Destroy(visualEffectInstance);
        }
    }
}

/// <summary>
/// UPDATED StunEffect - now integrates with NPCManager
/// Add this to replace the StunEffect class in BulletModifier.cs
/// </summary>
public class StunEffect : MonoBehaviour
{
    private float duration;
    private float timeRemaining;
    private bool freezeAnimation;
    private bool disableAI;
    private Color tintColor;
    private float tintStrength;
    private bool showDebug;

    // Components to stun
    private UnityEngine.AI.NavMeshAgent navAgent;
    private bool hadNavAgent = false;
    private bool wasNavAgentOnNavMesh = false;
    private CharacterController characterController;
    private bool hadCharacterController = false;
    private Animator animator;
    private bool hadAnimator = false;
    private float originalAnimatorSpeed;

    // NEW: NPCManager integration
    private NPCManager npcManager;
    private bool hasNPCManager = false;

    // AI components (store references to re-enable later)
    private MonoBehaviour[] aiScripts;
    private bool[] aiScriptWasEnabled;

    // Visual effects
    private GameObject stunEffectInstance;
    private Renderer[] renderers;
    private Dictionary<Renderer, Color[]> originalColors = new Dictionary<Renderer, Color[]>();

    public void Initialize(float stunDur, bool freezeAnim, bool disableAIBehavior, Color tint, float tintAmount, GameObject effectPrefab, bool debug)
    {
        duration = stunDur;
        timeRemaining = duration;
        freezeAnimation = freezeAnim;
        disableAI = disableAIBehavior;
        tintColor = tint;
        tintStrength = tintAmount;
        showDebug = debug;

        // NEW: Check if target has NPCManager
        npcManager = GetComponent<NPCManager>();
        if (npcManager != null)
        {
            hasNPCManager = true;
            npcManager.EnterStunnedState();
            Debug.Log($"[StunEffect] Using NPCManager stun system on {gameObject.name}");
        }
        else
        {
            // Fallback to old system for non-NPCManager enemies
            Debug.Log($"[StunEffect] No NPCManager found, using legacy stun system on {gameObject.name}");

            // Disable movement components (legacy)
            navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent != null)
            {
                hadNavAgent = true;
                wasNavAgentOnNavMesh = navAgent.isOnNavMesh;
                navAgent.isStopped = true;
                navAgent.velocity = Vector3.zero;
                Debug.Log($"[StunEffect] Stopped NavMeshAgent on {gameObject.name}");
            }

            characterController = GetComponent<CharacterController>();
            if (characterController != null)
            {
                hadCharacterController = true;
                characterController.enabled = false;
                Debug.Log($"[StunEffect] Disabled CharacterController on {gameObject.name}");
            }
        }

        // Freeze animation (works for both systems)
        if (freezeAnimation)
        {
            animator = GetComponent<Animator>();
            if (animator != null)
            {
                hadAnimator = true;
                originalAnimatorSpeed = animator.speed;
                animator.speed = 0f;
                Debug.Log($"[StunEffect] Froze Animator on {gameObject.name}");
            }
        }

        // Disable AI scripts if requested (legacy system only)
        if (disableAI && !hasNPCManager)
        {
            DisableAIScripts();
        }

        // Apply visual tint
        if (tintStrength > 0f)
        {
            ApplyStunTint();
        }

        // Spawn stun effect (stars, swirls around head)
        if (effectPrefab != null)
        {
            Vector3 spawnPosition = transform.position + Vector3.up * 2f;

            Transform headBone = transform.Find("Head");
            if (headBone == null)
                headBone = transform.Find("head");
            if (headBone != null)
                spawnPosition = headBone.position;

            stunEffectInstance = Instantiate(effectPrefab, spawnPosition, Quaternion.identity, transform);
            Debug.Log($"[StunEffect] Spawned stun effect on {gameObject.name} at {spawnPosition}");
        }

        Debug.Log($"[StunEffect] Started on {gameObject.name} for {duration}s");
    }

    public void RefreshEffect(float stunDur, bool freezeAnim, bool disableAIBehavior, Color tint, float tintAmount, GameObject effectPrefab, bool debug)
    {
        duration = stunDur;
        timeRemaining = duration; // Reset timer
        freezeAnimation = freezeAnim;
        disableAI = disableAIBehavior;
        tintColor = tint;
        tintStrength = tintAmount;
        showDebug = debug;

        Debug.Log($"[StunEffect] Refreshed on {gameObject.name}");
    }

    private void DisableAIScripts()
    {
        List<MonoBehaviour> foundAIScripts = new List<MonoBehaviour>();
        List<bool> foundAIScriptStates = new List<bool>();

        MonoBehaviour[] allScripts = GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour script in allScripts)
        {
            if (script == null) continue;
            if (script == this) continue;

            string scriptName = script.GetType().Name.ToLower();
            if (scriptName.Contains("ai") || scriptName.Contains("enemy") || scriptName.Contains("npc") ||
                scriptName.Contains("controller") || scriptName.Contains("behavior"))
            {
                foundAIScripts.Add(script);
                foundAIScriptStates.Add(script.enabled);

                if (script.enabled)
                {
                    script.enabled = false;
                    Debug.Log($"[StunEffect] Disabled AI script: {script.GetType().Name}");
                }
            }
        }

        aiScripts = foundAIScripts.ToArray();
        aiScriptWasEnabled = foundAIScriptStates.ToArray();

        if (aiScripts.Length > 0)
        {
            Debug.Log($"[StunEffect] Disabled {aiScripts.Length} AI scripts on {gameObject.name}");
        }
    }

    private void ReEnableAIScripts()
    {
        if (aiScripts == null) return;

        for (int i = 0; i < aiScripts.Length; i++)
        {
            if (aiScripts[i] != null && aiScriptWasEnabled[i])
            {
                aiScripts[i].enabled = true;
                Debug.Log($"[StunEffect] Re-enabled AI script: {aiScripts[i].GetType().Name}");
            }
        }
    }

    private void ApplyStunTint()
    {
        renderers = GetComponentsInChildren<Renderer>();

        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;

            Material[] materials = renderer.materials;
            Color[] colors = new Color[materials.Length];

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i].HasProperty("_Color"))
                {
                    colors[i] = materials[i].color;
                    Color tintedColor = Color.Lerp(colors[i], tintColor, tintStrength);
                    materials[i].color = tintedColor;
                }
            }

            originalColors[renderer] = colors;
            renderer.materials = materials;
        }

        Debug.Log($"[StunEffect] Applied stun tint to {renderers.Length} renderers");
    }

    private void RemoveStunTint()
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

        Debug.Log($"[StunEffect] Removed stun tint");
    }

    private void Update()
    {
        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            EndEffect();
        }

        // Debug visualization
        if (showDebug)
        {
            Debug.DrawRay(transform.position, Vector3.up * 3f, Color.yellow, Time.deltaTime);
        }
    }

    private void EndEffect()
    {
        Debug.Log($"[StunEffect] Ended on {gameObject.name}");

        // NEW: Exit stun state if using NPCManager
        if (hasNPCManager && npcManager != null)
        {
            npcManager.ExitStunnedState();
        }
        else
        {
            // Legacy system cleanup
            if (hadNavAgent && navAgent != null)
            {
                navAgent.isStopped = false;

                if (wasNavAgentOnNavMesh && navAgent.isOnNavMesh)
                {
                    Debug.Log($"[StunEffect] Resumed NavMeshAgent on {gameObject.name}");
                }
                else
                {
                    Debug.LogWarning($"[StunEffect] NavMeshAgent not on NavMesh for {gameObject.name}");
                }
            }

            if (hadCharacterController && characterController != null)
            {
                characterController.enabled = true;
                Debug.Log($"[StunEffect] Re-enabled CharacterController on {gameObject.name}");
            }

            if (disableAI)
            {
                ReEnableAIScripts();
            }
        }

        // Unfreeze animation (both systems)
        if (hadAnimator && animator != null)
        {
            animator.speed = originalAnimatorSpeed;
            Debug.Log($"[StunEffect] Unfroze Animator on {gameObject.name}");
        }

        // Remove visual effects
        RemoveStunTint();

        if (stunEffectInstance != null)
        {
            Destroy(stunEffectInstance);
        }

        Destroy(this);
    }

    private void OnDestroy()
    {
        // Restore components if destroyed prematurely
        if (hasNPCManager && npcManager != null)
        {
            npcManager.ExitStunnedState();
        }
        else
        {
            if (hadNavAgent && navAgent != null)
            {
                navAgent.isStopped = false;
            }

            if (hadCharacterController && characterController != null)
            {
                characterController.enabled = true;
            }

            if (disableAI)
            {
                ReEnableAIScripts();
            }
        }

        if (hadAnimator && animator != null)
        {
            animator.speed = originalAnimatorSpeed;
        }

        RemoveStunTint();

        if (stunEffectInstance != null)
        {
            Destroy(stunEffectInstance);
        }
    }
}
