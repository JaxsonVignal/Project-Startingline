using UnityEngine;

/// <summary>
/// Defines behavior modifiers for weapons
/// This data gets passed to bullets when they're fired
/// </summary>
[System.Serializable]
public class ModifierData
{
    [Header("Anti-Gravity Modifier")]
    [Tooltip("Bullets make targets levitate/float upwards")]
    public bool antiGravityRounds = false;

    [Tooltip("How high the target floats in units (10 = 10 units/feet high)")]
    public float antiGravityForce = 10f;

    [Tooltip("Duration of anti-gravity effect in seconds")]
    public float antiGravityDuration = 5f;

    [Tooltip("Not used for non-Rigidbody version (kept for compatibility)")]
    public bool disableGravity = true;

    [Tooltip("How fast the target lifts upward (units per second)")]
    public float initialUpwardImpulse = 5f;

    [Header("Anti-Gravity Visual Effects")]
    [Tooltip("Amount of bobbing/floating motion while suspended (0 = no bobbing, 0.5 = gentle, 2.0 = dramatic)")]
    [Range(0f, 5f)]
    public float bobbingAmount = 0.2f;

    [Tooltip("Speed of the bobbing motion (higher = faster bobbing)")]
    [Range(0.5f, 5f)]
    public float bobbingSpeed = 2f;

    [Header("Cryo Modifier")]
    [Tooltip("Bullets freeze targets in place")]
    public bool cryoRounds = false;

    [Tooltip("Duration target is frozen in seconds")]
    public float cryoDuration = 5f;

    [Tooltip("Freeze animation (if false, just stops movement)")]
    public bool freezeAnimation = true;

    [Header("Cryo Visual Effects")]
    [Tooltip("Tint color applied to frozen targets (light blue = icy look)")]
    public Color freezeTintColor = new Color(0.5f, 0.8f, 1f, 1f);

    [Tooltip("Strength of the color tint (0 = no tint, 1 = full tint)")]
    [Range(0f, 1f)]
    public float freezeTintStrength = 0.5f;

    [Tooltip("Optional ice effect prefab to spawn on frozen target")]
    public GameObject iceEffectPrefab;

    [Header("Explosive Modifier")]
    [Tooltip("Bullets explode on impact dealing area damage")]
    public bool explosiveRounds = false;

    [Tooltip("Radius of explosion in units")]
    public float explosionRadius = 5f;

    [Tooltip("Damage multiplier applied to weapon damage (1.0 = same as weapon, 2.0 = double)")]
    public float explosionDamageMultiplier = 1.5f;

    [Tooltip("Apply explosion force to rigidbodies")]
    public bool applyExplosionForce = true;

    [Tooltip("Force strength applied to physics objects")]
    public float explosionForceStrength = 1000f;

    [Header("Explosive Visual Effects")]
    [Tooltip("Explosion effect prefab to spawn at impact point")]
    public GameObject explosionEffectPrefab;

    [Tooltip("Show explosion radius in scene (debug)")]
    public bool showExplosionRadius = false;

    [Header("Tracer Modifier")]
    [Tooltip("Bullets glow/emit light as they fly")]
    public bool tracerRounds = false;

    [Tooltip("Color of the tracer glow")]
    public Color tracerColor = new Color(1f, 0.3f, 0f, 1f); // Orange/red by default

    [Tooltip("Intensity of the light (0-8, standard range is 0-2)")]
    [Range(0f, 8f)]
    public float tracerIntensity = 1.5f;

    [Tooltip("Range of the light in units")]
    [Range(1f, 20f)]
    public float tracerLightRange = 5f;

    [Header("Tracer Visual Effects")]
    [Tooltip("Add emission to bullet material (makes bullet itself glow)")]
    public bool addEmission = true;

    [Tooltip("Emission intensity multiplier")]
    [Range(0f, 10f)]
    public float emissionIntensity = 2f;

    [Tooltip("Optional trail renderer prefab for bullet trail")]
    public GameObject trailEffectPrefab;

    [Tooltip("Trail length in seconds")]
    [Range(0.1f, 2f)]
    public float trailDuration = 0.3f;

    [Header("Ricochet Modifier")]
    [Tooltip("Bullets bounce off surfaces once before applying effects")]
    public bool ricochetRounds = false;

    [Tooltip("Maximum number of bounces (1 = one bounce, 2 = two bounces, etc)")]
    [Range(1, 5)]
    public int maxBounces = 1;

    [Tooltip("Speed multiplier after bounce (1.0 = same speed, 0.8 = 80% speed)")]
    [Range(0.1f, 1.5f)]
    public float bounceSpeedMultiplier = 0.9f;

    [Tooltip("Damage multiplier after bounce (1.0 = same damage, 0.8 = 80% damage)")]
    [Range(0.1f, 1.5f)]
    public float bounceDamageMultiplier = 0.8f;

    [Header("Ricochet Visual Effects")]
    [Tooltip("Spark effect prefab to spawn at bounce point")]
    public GameObject ricochetSparkPrefab;

    [Tooltip("Sound to play on ricochet")]
    public AudioClip ricochetSound;

    [Tooltip("Show ricochet angle in scene (debug)")]
    public bool showRicochetDebug = false;

    // ADD MORE MODIFIERS HERE IN THE FUTURE
}