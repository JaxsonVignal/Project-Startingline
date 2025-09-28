using UnityEngine;

public class WeaponFollow : MonoBehaviour
{
    [HideInInspector] public Transform cameraTransform;

    [Header("Follow Settings")]
    public float smoothSpeed = 10f;        // How fast weapon rotates to match camera
    public float swayAmount = 0.05f;       // How much the weapon sways with movement
    public float swaySmooth = 4f;          // Smoothing for sway

    private Quaternion initialLocalRotation;
    private Vector3 initialLocalPosition;

    private Vector3 swayVelocity = Vector3.zero;

    private Vector3 lastCameraEuler;

    private void Start()
    {
        if (cameraTransform == null) cameraTransform = Camera.main.transform;

        initialLocalRotation = transform.localRotation;
        initialLocalPosition = transform.localPosition;
        lastCameraEuler = cameraTransform.eulerAngles;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        // ---- 1. Weapon tilt with camera pitch ----
        float pitch = cameraTransform.eulerAngles.x;
        if (pitch > 180) pitch -= 360;

        Quaternion targetRotation = Quaternion.Euler(pitch, 0, 0) * initialLocalRotation;

        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * smoothSpeed);

        // ---- 2. Weapon sway based on camera movement ----
        Vector3 deltaEuler = cameraTransform.eulerAngles - lastCameraEuler;

        // Convert large jumps due to 0-360 wraparound
        if (deltaEuler.x > 180) deltaEuler.x -= 360;
        if (deltaEuler.y > 180) deltaEuler.y -= 360;
        if (deltaEuler.z > 180) deltaEuler.z -= 360;

        Vector3 swayTarget = new Vector3(-deltaEuler.x, -deltaEuler.y, 0) * swayAmount;
        transform.localPosition = Vector3.SmoothDamp(transform.localPosition, initialLocalPosition + swayTarget, ref swayVelocity, 1 / swaySmooth);

        lastCameraEuler = cameraTransform.eulerAngles;
    }
}
