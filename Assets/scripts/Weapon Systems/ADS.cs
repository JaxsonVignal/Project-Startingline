using UnityEngine;
using UnityEngine.InputSystem;

public class ADS : MonoBehaviour
{
    [Header("References")]
    public Transform weaponRoot;
    public Transform hipPosition;
    public Transform adsPosition;
    public Camera playerCamera;

    [Header("Settings")]
    public float aimSpeed = 10f;
    public float adsFOV = 45f;
    private float defaultFOV;

    private bool isAiming;
    private GameInput input;

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
    }

    private void Update()
    {
        // Smoothly move the weapon
        Transform targetPos = isAiming ? adsPosition : hipPosition;
        weaponRoot.position = Vector3.Lerp(weaponRoot.position, targetPos.position, Time.deltaTime * aimSpeed);
        weaponRoot.rotation = Quaternion.Lerp(weaponRoot.rotation, targetPos.rotation, Time.deltaTime * aimSpeed);

        // Smooth FOV transition
        float targetFOV = isAiming ? adsFOV : defaultFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * aimSpeed);
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
