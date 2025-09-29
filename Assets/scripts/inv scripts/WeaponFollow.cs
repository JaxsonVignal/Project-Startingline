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

       
        Quaternion targetRotation = cameraTransform.rotation * Quaternion.Inverse(cameraTransform.parent.rotation) * initialLocalRotation;

        
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            Time.deltaTime * smoothSpeed
        );

        
        Vector3 deltaEuler = cameraTransform.eulerAngles - lastCameraEuler;

        
        if (deltaEuler.x > 180) deltaEuler.x -= 360;
        if (deltaEuler.y > 180) deltaEuler.y -= 360;
        if (deltaEuler.z > 180) deltaEuler.z -= 360;

        Vector3 swayTarget = new Vector3(-deltaEuler.x, -deltaEuler.y, 0) * swayAmount;
        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition,
            initialLocalPosition + swayTarget,
            ref swayVelocity,
            1 / swaySmooth
        );

        lastCameraEuler = cameraTransform.eulerAngles;
    }
}
