using UnityEngine;

public class SkyboxManager : MonoBehaviour
{
    [System.Serializable]
    public class SkyboxTimeSlot
    {
        public string name;
        public float startHour; // 0-24 hour format
        public Material skyboxMaterial;
    }

    [Header("Skybox Settings")]
    public SkyboxTimeSlot[] skyboxSlots;

    [Header("Transition Settings")]
    public bool useTransitions = true;
    public float transitionDurationInHours = 0.5f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private int currentSlotIndex = -1;
    private int nextSlotIndex = -1;
    private bool isTransitioning = false;
    private float transitionStartTime = 0f;
    private Material blendMaterial;

    void OnEnable()
    {
        DayNightCycleManager.OnTimeChanged += OnTimeChanged;
    }

    void OnDisable()
    {
        DayNightCycleManager.OnTimeChanged -= OnTimeChanged;
    }

    void Start()
    {
        // Validate setup
        if (skyboxSlots == null || skyboxSlots.Length == 0)
        {
            Debug.LogError("SkyboxManager: No skybox slots configured!");
            enabled = false;
            return;
        }

        // Sort skybox slots by start hour
        System.Array.Sort(skyboxSlots, (a, b) => a.startHour.CompareTo(b.startHour));

        if (showDebugLogs)
        {
            Debug.Log("=== Skybox Slots Configuration ===");
            for (int i = 0; i < skyboxSlots.Length; i++)
            {
                Debug.Log($"Slot {i}: {skyboxSlots[i].name} starts at {skyboxSlots[i].startHour:F1}h");
            }
        }

        // Create blend material if using transitions
        if (useTransitions)
        {
            CreateBlendMaterial();
        }

        // Set initial skybox
        if (DayNightCycleManager.Instance != null)
        {
            currentSlotIndex = GetCurrentSkyboxIndex(DayNightCycleManager.Instance.currentTimeOfDay);
            if (currentSlotIndex >= 0 && currentSlotIndex < skyboxSlots.Length)
            {
                SetSkybox(skyboxSlots[currentSlotIndex].skyboxMaterial);
                if (showDebugLogs)
                    Debug.Log($"Initial skybox set to slot {currentSlotIndex}: {skyboxSlots[currentSlotIndex].name}");
            }
        }
        else
        {
            Debug.LogError("SkyboxManager: DayNightCycleManager.Instance not found!");
        }
    }

    void Update()
    {
        // Continue updating transition every frame
        if (isTransitioning && DayNightCycleManager.Instance != null)
        {
            UpdateTransition(DayNightCycleManager.Instance.currentTimeOfDay);
        }
    }

    void CreateBlendMaterial()
    {
        Shader blendShader = Shader.Find("Skybox/PanoramicBlend");
        if (blendShader != null)
        {
            blendMaterial = new Material(blendShader);
            blendMaterial.SetFloat("_Exposure", 1.0f);
            blendMaterial.SetColor("_Tint", Color.white);

            if (showDebugLogs)
                Debug.Log("SkyboxManager: Blend material created successfully");
        }
        else
        {
            Debug.LogWarning("SkyboxManager: Skybox/PanoramicBlend shader not found. Using instant transitions.");
            useTransitions = false;
        }
    }

    void OnTimeChanged(float currentTime)
    {
        if (skyboxSlots == null || skyboxSlots.Length == 0) return;

        int targetSlotIndex = GetCurrentSkyboxIndex(currentTime);

        Debug.Log($"[OnTimeChanged] Time: {currentTime:F2}h | CurrentSlot: {currentSlotIndex} ({(currentSlotIndex >= 0 ? skyboxSlots[currentSlotIndex].name : "none")}) | TargetSlot: {targetSlotIndex} ({skyboxSlots[targetSlotIndex].name}) | Transitioning: {isTransitioning}");

        // Check if we need to switch skyboxes
        if (targetSlotIndex != currentSlotIndex && targetSlotIndex >= 0)
        {
            Debug.Log($"[SWITCH TRIGGERED] From slot {currentSlotIndex} to slot {targetSlotIndex}");

            if (useTransitions && blendMaterial != null && currentSlotIndex >= 0)
            {
                Debug.Log($"[STARTING TRANSITION] {skyboxSlots[currentSlotIndex].name} -> {skyboxSlots[targetSlotIndex].name}");
                int fromIndex = currentSlotIndex;
                currentSlotIndex = targetSlotIndex;  // <-- UPDATE THIS IMMEDIATELY
                StartTransition(fromIndex, targetSlotIndex, currentTime);
            }
            else
            {
                Debug.Log($"[INSTANT SWITCH] Setting skybox to: {skyboxSlots[targetSlotIndex].name}");
                SetSkybox(skyboxSlots[targetSlotIndex].skyboxMaterial);
                currentSlotIndex = targetSlotIndex;
            }
        }
    }
    void StartTransition(int fromIndex, int toIndex, float startTime)
    {
        Debug.Log($"[StartTransition] Called with fromIndex={fromIndex}, toIndex={toIndex}, startTime={startTime:F2}");

        if (fromIndex < 0 || toIndex < 0 || fromIndex >= skyboxSlots.Length || toIndex >= skyboxSlots.Length)
        {
            Debug.LogWarning($"[StartTransition] Invalid indices, doing instant switch to {toIndex}");
            currentSlotIndex = toIndex;
            SetSkybox(skyboxSlots[toIndex].skyboxMaterial);
            return;
        }

        Material fromMat = skyboxSlots[fromIndex].skyboxMaterial;
        Material toMat = skyboxSlots[toIndex].skyboxMaterial;

        Debug.Log($"[StartTransition] From material: {fromMat.name}, To material: {toMat.name}");

        // Get textures from panoramic materials
        Texture fromTex = GetPanoramicTexture(fromMat);
        Texture toTex = GetPanoramicTexture(toMat);

        Debug.Log($"[StartTransition] From texture: {fromTex?.name}, To texture: {toTex?.name}");

        if (fromTex == null || toTex == null)
        {
            Debug.LogWarning($"[StartTransition] Missing textures, doing instant switch");
            currentSlotIndex = toIndex;
            SetSkybox(toMat);
            return;
        }

        // Setup blend material
        blendMaterial.SetTexture("_MainTex1", fromTex);
        blendMaterial.SetTexture("_MainTex2", toTex);
        blendMaterial.SetFloat("_Blend", 0f);

        Debug.Log($"[StartTransition] Blend material textures set, blend=0");

        // Copy rotation values if they exist
        float rotation1 = fromMat.HasProperty("_Rotation") ? fromMat.GetFloat("_Rotation") : 0f;
        float rotation2 = toMat.HasProperty("_Rotation") ? toMat.GetFloat("_Rotation") : 0f;
        blendMaterial.SetFloat("_Rotation1", rotation1);
        blendMaterial.SetFloat("_Rotation2", rotation2);

        // Copy exposure and tint if they exist
        if (fromMat.HasProperty("_Exposure"))
            blendMaterial.SetFloat("_Exposure", fromMat.GetFloat("_Exposure"));
        if (fromMat.HasProperty("_Tint"))
            blendMaterial.SetColor("_Tint", fromMat.GetColor("_Tint"));

        isTransitioning = true;
        nextSlotIndex = toIndex;
        transitionStartTime = startTime;

        RenderSettings.skybox = blendMaterial;
        DynamicGI.UpdateEnvironment();

        Debug.Log($"[StartTransition] RenderSettings.skybox set to blend material. isTransitioning={isTransitioning}, nextSlotIndex={nextSlotIndex}");
    }

    void UpdateTransition(float currentTime)
    {
        float timeSinceStart = currentTime - transitionStartTime;

        // Handle time wrapping around midnight
        if (timeSinceStart < 0)
        {
            timeSinceStart += 24f;
        }

        float progress = Mathf.Clamp01(timeSinceStart / transitionDurationInHours);

        // Update blend value
        if (blendMaterial != null)
        {
            blendMaterial.SetFloat("_Blend", progress);
            DynamicGI.UpdateEnvironment();
        }

        // Log every frame during transition
        Debug.Log($"[UpdateTransition] Time: {currentTime:F2}h | Start: {transitionStartTime:F2}h | TimeSince: {timeSinceStart:F3}h | Progress: {progress:F3} | Blend: {blendMaterial?.GetFloat("_Blend"):F3}");

        // Transition complete
        if (progress >= 1f)
        {
            isTransitioning = false;

            Debug.Log($"[UpdateTransition] TRANSITION COMPLETE - Setting to slot {nextSlotIndex}: {skyboxSlots[nextSlotIndex].name}");

            // IMPORTANT: Switch back to the actual skybox material, not the blend material
            if (nextSlotIndex >= 0 && nextSlotIndex < skyboxSlots.Length)
            {
                currentSlotIndex = nextSlotIndex;
                RenderSettings.skybox = skyboxSlots[currentSlotIndex].skyboxMaterial;
                DynamicGI.UpdateEnvironment();

                Debug.Log($"[UpdateTransition] RenderSettings.skybox = {RenderSettings.skybox.name}");
            }
        }
    }

    Texture GetPanoramicTexture(Material mat)
    {
        if (mat == null) return null;

        // Try common panoramic texture property names
        string[] possibleNames = { "_MainTex", "_Tex", "_FrontTex" };

        foreach (string texName in possibleNames)
        {
            if (mat.HasProperty(texName))
            {
                Texture tex = mat.GetTexture(texName);
                if (tex != null)
                {
                    return tex;
                }
            }
        }

        if (showDebugLogs)
            Debug.LogWarning($"Could not find texture in material '{mat.name}' with shader '{mat.shader.name}'");
        return null;
    }

    int GetCurrentSkyboxIndex(float currentTime)
    {
        if (skyboxSlots == null || skyboxSlots.Length == 0) return -1;

        // Find the slot that should be active for the current time
        for (int i = skyboxSlots.Length - 1; i >= 0; i--)
        {
            if (currentTime >= skyboxSlots[i].startHour)
            {
                return i;
            }
        }

        // If no match found, return the last slot (handles wrap-around from midnight)
        return skyboxSlots.Length - 1;
    }

    void SetSkybox(Material newSkybox)
    {
        if (newSkybox != null)
        {
            RenderSettings.skybox = newSkybox;

            // Reset any modified properties on the original material
            if (newSkybox.HasProperty("_Exposure"))
            {
                // Restore original exposure if it was in the material
                if (!newSkybox.shader.name.Contains("Blend"))
                    newSkybox.SetFloat("_Exposure", 1.0f);
            }

            DynamicGI.UpdateEnvironment();

            if (showDebugLogs)
                Debug.Log($"Skybox set to: {newSkybox.name}");
        }
        else
        {
            Debug.LogWarning("SkyboxManager: Attempted to set null skybox material");
        }
    }

    // Public helper methods
    public void ForceSetSkybox(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < skyboxSlots.Length)
        {
            currentSlotIndex = slotIndex;
            isTransitioning = false;
            SetSkybox(skyboxSlots[slotIndex].skyboxMaterial);
            Debug.Log($"Forced skybox to slot {slotIndex}: {skyboxSlots[slotIndex].name}");
        }
    }

    public string GetCurrentSkyboxName()
    {
        if (currentSlotIndex >= 0 && currentSlotIndex < skyboxSlots.Length)
            return skyboxSlots[currentSlotIndex].name;
        return "None";
    }
}