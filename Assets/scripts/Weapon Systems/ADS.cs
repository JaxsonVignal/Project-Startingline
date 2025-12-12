using UnityEngine;
using UnityEngine.InputSystem;

public class ADS : MonoBehaviour
{
    [Header("References")]
    public Transform weaponRoot;
    public Transform hipPosition;
    public Transform adsPosition;
    public Transform scopeAdsPosition;
    public Camera playerCamera;

    [Header("Settings")]
    public float aimSpeed = 10f;
    public float adsFOV = 45f;
    public float scopeFOV = 20f;
    [Tooltip("If true, hide crosshair when aiming down sights without a scope")]
    public bool hideCrosshairWhenADS = true;

    private float defaultFOV;
    private bool isAiming;
    private GameInput input;
    private WeaponAttachmentSystem attachmentSystem;
    private WeaponData currentWeaponData;
    private bool hasScopeEquipped;
    private Vector3 originalScopePosition;
    private Quaternion originalScopeRotation;
    private string currentEquippedScopeId;
    private AttachmentData currentScope;

    private void Awake()
    {
        input = new GameInput();
        input.Player.Enable();
        input.Player.Aim.performed += ctx => StartAim();
        input.Player.Aim.canceled += ctx => StopAim();
    }

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
        defaultFOV = playerCamera.fieldOfView;

        attachmentSystem = GetComponent<WeaponAttachmentSystem>();

        // Store original scope position/rotation for resets
        if (scopeAdsPosition != null)
        {
            originalScopePosition = scopeAdsPosition.localPosition;
            originalScopeRotation = scopeAdsPosition.localRotation;
        }

        // Initialize crosshair state
        UpdateCrosshairVisibility();
    }

    private void Update()
    {
        UpdateScopeStatus();

        Transform targetPos;
        if (isAiming && hasScopeEquipped && scopeAdsPosition != null)
        {
            targetPos = scopeAdsPosition;
            if (Time.frameCount % 60 == 0) // Log every 60 frames
            {
                Debug.Log($"[ADS Update] Using scopeAdsPosition: {scopeAdsPosition.localPosition}");
            }
        }
        else if (isAiming)
        {
            targetPos = adsPosition;
        }
        else
        {
            targetPos = hipPosition;
        }

        // Use LOCAL position and rotation instead of world
        weaponRoot.localPosition = Vector3.Lerp(weaponRoot.localPosition, targetPos.localPosition, Time.deltaTime * aimSpeed);
        weaponRoot.localRotation = Quaternion.Lerp(weaponRoot.localRotation, targetPos.localRotation, Time.deltaTime * aimSpeed);

        float targetFOV = GetCurrentFOV();
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * aimSpeed);
    }

    private void UpdateScopeStatus()
    {
        if (attachmentSystem == null)
        {
            hasScopeEquipped = false;
            if (currentEquippedScopeId != null)
            {
                ResetScopeTransform();
                currentEquippedScopeId = null;
                currentScope = null;
                UpdateCrosshairVisibility();
            }
            return;
        }

        hasScopeEquipped = false;
        foreach (var att in attachmentSystem.equippedAttachments)
        {
            if (att.type == AttachmentType.Sight)
            {
                hasScopeEquipped = true;

                // Only apply modifiers if scope changed
                if (currentEquippedScopeId != att.id)
                {
                    currentEquippedScopeId = att.id;
                    currentScope = att;
                    Debug.Log($"Scope equipped: {att.id}");
                    ApplyScopeModifiers(att);
                    UpdateCrosshairVisibility();
                }
                break;
            }
        }

        if (!hasScopeEquipped && currentEquippedScopeId != null)
        {
            Debug.Log("No scope equipped, resetting");
            ResetScopeTransform();
            currentEquippedScopeId = null;
            currentScope = null;
            UpdateCrosshairVisibility();
        }
    }

    private void ApplyScopeModifiers(AttachmentData scope)
    {
        Debug.Log($"[ADS] ====== ApplyScopeModifiers START ======");
        Debug.Log($"[ADS] Scope: {(scope != null ? scope.id : "NULL")}");
        Debug.Log($"[ADS] scopeAdsPosition: {(scopeAdsPosition != null ? "EXISTS" : "NULL")}");

        if (scope == null || scopeAdsPosition == null)
        {
            Debug.LogWarning("[ADS] Scope or scopeAdsPosition is null, returning early");
            return;
        }

        Debug.Log($"[ADS] originalScopePosition: {originalScopePosition}");
        Debug.Log($"[ADS] Current weapon data: {(currentWeaponData != null ? currentWeaponData.weaponId : "NULL")}");

        // Get the weapon-specific offset for this scope from the weapon data
        ScopeOffsetData offsetData = null;
        if (currentWeaponData != null)
        {
            offsetData = currentWeaponData.GetScopeOffset(scope);
            Debug.Log($"[ADS] Offset data found: {(offsetData != null ? "YES" : "NO")}");

            if (offsetData != null)
            {
                Debug.Log($"[ADS] Found offset - Pos: {offsetData.positionOffset}, Rot: {offsetData.rotationOffset}");
            }
        }
        else
        {
            Debug.LogError("[ADS] currentWeaponData is NULL! You need to call SetWeaponData() when equipping the weapon!");
        }

        if (offsetData != null)
        {
            Vector3 newPos = originalScopePosition + offsetData.positionOffset;
            Quaternion newRot = originalScopeRotation * Quaternion.Euler(offsetData.rotationOffset);

            Debug.Log($"[ADS] APPLYING OFFSET!");
            Debug.Log($"[ADS] Original position: {originalScopePosition}");
            Debug.Log($"[ADS] Offset: {offsetData.positionOffset}");
            Debug.Log($"[ADS] New position: {newPos}");
            Debug.Log($"[ADS] Setting scopeAdsPosition.localPosition to: {newPos}");

            scopeAdsPosition.localPosition = newPos;
            scopeAdsPosition.localRotation = newRot;

            Debug.Log($"[ADS] After setting - scopeAdsPosition.localPosition is: {scopeAdsPosition.localPosition}");
        }
        else
        {
            // No weapon-specific offset found, use default position
            scopeAdsPosition.localPosition = originalScopePosition;
            scopeAdsPosition.localRotation = originalScopeRotation;
            Debug.LogWarning($"[ADS] No offset found for scope {scope.id} on weapon {(currentWeaponData != null ? currentWeaponData.weaponId : "unknown")}. Using default position: {originalScopePosition}");
        }

        Debug.Log($"[ADS] ====== ApplyScopeModifiers END ======");
    }

    private void ResetScopeTransform()
    {
        if (scopeAdsPosition != null)
        {
            scopeAdsPosition.localPosition = originalScopePosition;
            scopeAdsPosition.localRotation = originalScopeRotation;
        }
    }

    private float GetCurrentFOV()
    {
        if (isAiming && hasScopeEquipped)
        {
            if (attachmentSystem != null)
            {
                foreach (var att in attachmentSystem.equippedAttachments)
                {
                    if (att.type == AttachmentType.Sight && att.scopeFOVOverride > 0)
                    {
                        return att.scopeFOVOverride;
                    }
                }
            }
            return scopeFOV;
        }
        else if (isAiming)
        {
            return adsFOV;
        }
        else
        {
            return defaultFOV;
        }
    }

    private void UpdateCrosshairVisibility()
    {
        if (CrosshairManager.Instance == null)
        {
            Debug.LogWarning("CrosshairManager not found in scene!");
            return;
        }

        // If aiming with a scope that has a custom reticle, show it
        if (isAiming && hasScopeEquipped && currentScope != null && currentScope.scopeReticle != null)
        {
            CrosshairManager.Instance.ShowScopeReticle(
                currentScope.scopeReticle,
                currentScope.reticleScale,
                currentScope.reticleColor
            );
        }
        // If aiming without a scope and hideCrosshairWhenADS is enabled, hide all crosshairs
        else if (isAiming && !hasScopeEquipped && hideCrosshairWhenADS)
        {
            CrosshairManager.Instance.HideAllCrosshairs();
        }
        // Otherwise show default crosshair (hip fire or not aiming)
        else
        {
            CrosshairManager.Instance.ShowDefaultCrosshair();
        }
    }

    public void SetAttachmentSystem(WeaponAttachmentSystem attachSys)
    {
        Debug.Log($"[ADS] SetAttachmentSystem called");
        attachmentSystem = attachSys;
        UpdateScopeStatus();
    }

    /// <summary>
    /// Set the current weapon data to retrieve weapon-specific scope offsets
    /// Call this when equipping a new weapon
    /// </summary>
    public void SetWeaponData(WeaponData weaponData)
    {
        currentWeaponData = weaponData;
        Debug.Log($"[ADS] SetWeaponData called with: {(weaponData != null ? weaponData.weaponId : "NULL")}");

        if (weaponData != null && weaponData.scopeOffsets != null)
        {
            Debug.Log($"[ADS] Weapon has {weaponData.scopeOffsets.Count} scope offset entries");
            foreach (var offset in weaponData.scopeOffsets)
            {
                Debug.Log($"[ADS]   - Scope: {(offset.scope != null ? offset.scope.id : "NULL")}, Pos Offset: {offset.positionOffset}, Rot Offset: {offset.rotationOffset}");
            }
        }

        // Reapply scope modifiers if a scope is currently equipped
        if (hasScopeEquipped && currentScope != null)
        {
            Debug.Log($"[ADS] Reapplying scope modifiers for currently equipped scope: {currentScope.id}");
            ApplyScopeModifiers(currentScope);
        }
    }

    private void StartAim()
    {
        isAiming = true;
        UpdateCrosshairVisibility();
    }

    private void StopAim()
    {
        isAiming = false;
        UpdateCrosshairVisibility();
    }

    private void OnDisable()
    {
        input.Player.Aim.performed -= ctx => StartAim();
        input.Player.Aim.canceled -= ctx => StopAim();
        input.Disable();

        // Reset to default crosshair when weapon is disabled
        if (CrosshairManager.Instance != null)
        {
            CrosshairManager.Instance.ShowDefaultCrosshair();
        }
    }
}