using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightController : MonoBehaviour
{
    public static FlashlightController Instance;

    private FlashlightData currentFlashlight;
    private Light flashlightLight;
    private AudioSource audioSource;
    private bool isFlashlightOn = false;
    private float currentBatteryLife;

    private GameObject flashlightObject; // The actual attachment prefab instance
    private Transform flashlightSocket; // Where the light component should be

    private float lastToggleTime = 0f;
    private const float toggleCooldown = 0.2f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void Update()
    {
        // Battery drain
        if (isFlashlightOn && currentFlashlight != null && currentFlashlight.hasBatteryDrain)
        {
            currentBatteryLife -= Time.deltaTime;

            if (currentBatteryLife <= 0f)
            {
                currentBatteryLife = 0f;
                TurnOffFlashlight();
                Debug.Log("Flashlight battery depleted!");
            }
        }
    }

    /// <summary>
    /// Called when a flashlight attachment is equipped
    /// </summary>
    public void EquipFlashlight(FlashlightData flashlightData, GameObject attachmentPrefabInstance)
    {
        Debug.Log($"[FlashlightController] Equipping flashlight: {flashlightData.flashlightName}");

        currentFlashlight = flashlightData;
        flashlightObject = attachmentPrefabInstance;
        currentBatteryLife = flashlightData.batteryLife;

        // Find or create the light component
        SetupFlashlightLight();

        // Start with flashlight off
        isFlashlightOn = false;
        if (flashlightLight != null)
        {
            flashlightLight.enabled = false;
        }

        Debug.Log($"[FlashlightController] Flashlight equipped successfully");
    }

    /// <summary>
    /// Called when flashlight attachment is removed
    /// </summary>
    public void UnequipFlashlight()
    {
        Debug.Log("[FlashlightController] Unequipping flashlight");

        if (flashlightLight != null)
        {
            flashlightLight.enabled = false;
        }

        currentFlashlight = null;
        flashlightLight = null;
        flashlightObject = null;
        isFlashlightOn = false;
    }

    /// <summary>
    /// Setup the Light component on the flashlight attachment
    /// </summary>
    private void SetupFlashlightLight()
    {
        if (flashlightObject == null || currentFlashlight == null)
        {
            Debug.LogError("[FlashlightController] Cannot setup light - missing flashlight object or data");
            return;
        }

        // Look for existing Light component
        flashlightLight = flashlightObject.GetComponentInChildren<Light>();

        // If no light exists, create one
        if (flashlightLight == null)
        {
            // Try to find LightStartPoint first (preferred location)
            Transform lightPoint = flashlightObject.transform.Find("LightStartPoint");

            // Fallback to old naming convention
            if (lightPoint == null)
            {
                lightPoint = flashlightObject.transform.Find("LightPoint");
            }

            if (lightPoint == null)
            {
                // Create a new child object for the light
                GameObject lightObj = new GameObject("FlashlightLight");
                lightObj.transform.SetParent(flashlightObject.transform);
                lightObj.transform.localPosition = Vector3.forward * 0.1f; // Slightly forward
                lightObj.transform.localRotation = Quaternion.identity;
                lightPoint = lightObj.transform;
                Debug.Log("[FlashlightController] Created new light point at default position");
            }
            else
            {
                Debug.Log($"[FlashlightController] Found light point: {lightPoint.name}");
            }

            flashlightLight = lightPoint.gameObject.AddComponent<Light>();
        }
        else
        {
            Debug.Log("[FlashlightController] Using existing Light component");
        }

        // Configure the light
        flashlightLight.type = LightType.Spot;
        flashlightLight.intensity = currentFlashlight.intensity;
        flashlightLight.range = currentFlashlight.range;
        flashlightLight.spotAngle = currentFlashlight.spotAngle;
        flashlightLight.color = currentFlashlight.lightColor;
        flashlightLight.shadows = LightShadows.Soft;
        flashlightLight.renderMode = LightRenderMode.ForcePixel;

        // Optional: Apply flare
        if (currentFlashlight.lightFlare != null)
        {
            flashlightLight.flare = currentFlashlight.lightFlare;
        }

        // Optional: Apply cookie (light pattern)
        if (currentFlashlight.lightCookie != null)
        {
            flashlightLight.cookie = currentFlashlight.lightCookie;
        }

        // Start disabled
        flashlightLight.enabled = false;

        Debug.Log($"[FlashlightController] Light component configured at {flashlightLight.transform.name}");
    }

    /// <summary>
    /// Toggle flashlight on/off (called by input)
    /// </summary>
    public void ToggleFlashlight(InputAction.CallbackContext context)
    {
        // Cooldown to prevent rapid toggling
        if (Time.time - lastToggleTime < toggleCooldown)
        {
            return;
        }

        if (currentFlashlight == null)
        {
            Debug.Log("No flashlight attached!");
            return;
        }

        lastToggleTime = Time.time;

        if (isFlashlightOn)
        {
            TurnOffFlashlight();
        }
        else
        {
            TurnOnFlashlight();
        }
    }

    private void TurnOnFlashlight()
    {
        if (currentFlashlight == null || flashlightLight == null)
        {
            Debug.LogWarning("Cannot turn on flashlight - not equipped properly");
            return;
        }

        // Check battery
        if (currentFlashlight.hasBatteryDrain && currentBatteryLife <= 0f)
        {
            Debug.Log("Flashlight battery is dead!");
            return;
        }

        isFlashlightOn = true;
        flashlightLight.enabled = true;

        // Play sound
        PlayToggleSound();

        Debug.Log($"Flashlight ON - Battery: {currentBatteryLife:F1}s");
    }

    private void TurnOffFlashlight()
    {
        if (flashlightLight == null)
        {
            return;
        }

        isFlashlightOn = false;
        flashlightLight.enabled = false;

        // Play sound
        PlayToggleSound();

        Debug.Log("Flashlight OFF");
    }

    private void PlayToggleSound()
    {
        if (currentFlashlight == null || currentFlashlight.toggleSound == null)
            return;

        // Create temporary audio source if needed
        if (audioSource == null && flashlightObject != null)
        {
            audioSource = flashlightObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = flashlightObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.volume = 0.5f;
            }
        }

        if (audioSource != null)
        {
            audioSource.PlayOneShot(currentFlashlight.toggleSound);
        }
    }

    // Public getters
    public bool IsFlashlightOn() => isFlashlightOn;
    public bool HasFlashlight() => currentFlashlight != null;
    public float GetBatteryLife() => currentBatteryLife;
    public float GetMaxBatteryLife() => currentFlashlight?.batteryLife ?? 0f;
}