using UnityEngine;

public class GunFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Vector3 offset = new Vector3(0f, -0.2f, 0.5f); // tweak to position gun in view

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Position gun relative to camera
        transform.position = cameraTransform.position + cameraTransform.rotation * offset;

        // Keep gun rotation fixed (optional: match player yaw only)
        Vector3 euler = transform.eulerAngles;
        euler.x = 0f;
        euler.y = cameraTransform.eulerAngles.y; // optional: follow yaw
        euler.z = 0f;
        transform.eulerAngles = euler;
    }
}
