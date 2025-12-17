
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory System/Flashlight")]
public class FlashlightData : ScriptableObject
{
    [Header("Flashlight Info")]
    public string flashlightName = "Tactical Flashlight";
    public string flashlightId = "flashlight_tactical";

    [Header("Light Settings")]
    [Tooltip("Light intensity (brightness)")]
    public float intensity = 2f;

    [Tooltip("Light range in meters")]
    public float range = 50f;

    [Tooltip("Spotlight angle (cone angle)")]
    [Range(1f, 179f)]
    public float spotAngle = 45f;

    [Tooltip("Light color")]
    public Color lightColor = Color.white;

    [Header("Audio")]
    [Tooltip("Sound when toggling flashlight on/off")]
    public AudioClip toggleSound;

    [Header("Battery Settings (Optional)")]
    [Tooltip("Enable battery drain system")]
    public bool hasBatteryDrain = false;

    [Tooltip("Battery life in seconds (0 = infinite)")]
    public float batteryLife = 600f; // 10 minutes default

    [Header("Visual Effects")]
    [Tooltip("Light flare effect (optional)")]
    public Flare lightFlare;

    [Tooltip("Cookie texture for light pattern (optional)")]
    public Texture lightCookie;
}