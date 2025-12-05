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
    private bool hasScopeEquipped;
    private Vector3 originalScopePosition;
    private Quaternion originalScopeRotation;
    private string currentEquippedScopeId;
    private Vector3 scopePositionModifier = Vector3.zero;
    private Quaternion scopeRotationModifier = Quaternion.identity;
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
                    Debug.Log($"Scope equipped: {att.id}, offset: {att.scopePositionOffset}");
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
        if (scope == null || scopeAdsPosition == null) return;

        // Apply offsets relative to the original position stored at Start
        scopeAdsPosition.localPosition = originalScopePosition + scope.scopePositionOffset;
        scopeAdsPosition.localRotation = originalScopeRotation * Quaternion.Euler(scope.scopeRotationOffset);

        Debug.Log($"Scope modifiers applied. Position: {scopeAdsPosition.localPosition}, Offset was: {scope.scopePositionOffset}");
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
        attachmentSystem = attachSys;
        UpdateScopeStatus();
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