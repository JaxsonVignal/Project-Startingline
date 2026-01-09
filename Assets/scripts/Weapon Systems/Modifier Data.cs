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

    // ADD MORE MODIFIERS HERE IN THE FUTURE
    // Example:
    // [Header("Explosive Modifier")]
    // public bool explosiveRounds = false;
    // public float explosionRadius = 5f;
    // public float explosionDamage = 50f;
}