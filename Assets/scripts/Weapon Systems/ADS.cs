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

        weaponRoot.position = Vector3.Lerp(weaponRoot.position, targetPos.position, Time.deltaTime * aimSpeed);
        weaponRoot.rotation = Quaternion.Lerp(weaponRoot.rotation, targetPos.rotation, Time.deltaTime * aimSpeed);

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
                    Debug.Log($"Scope equipped: {att.id}, offset: {att.scopePositionOffset}");
                    ApplyScopeModifiers(att);
                }
                break;
            }
        }

        if (!hasScopeEquipped && currentEquippedScopeId != null)
        {
            Debug.Log("No scope equipped, resetting");
            ResetScopeTransform();
            currentEquippedScopeId = null;
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

    public void SetAttachmentSystem(WeaponAttachmentSystem attachSys)
    {
        attachmentSystem = attachSys;
        UpdateScopeStatus();
    }

    private void StartAim() => isAiming = true;
    private void StopAim() => isAiming = false;

    private void OnDisable()
    {
        input.Player.Aim.performed -= ctx => StartAim();
        input.Player.Aim.canceled -= ctx => StopAim();
        input.Disable();
    }
}