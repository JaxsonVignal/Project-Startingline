using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [Tooltip("Multiplier for shake intensity (0 = no shake, 1 = normal, >1 = more intense)")]
    [Range(0f, 2f)]
    public float shakeMultiplier = 1f;

    private Vector3 originalPosition;
    private bool isShaking = false;

    public void Shake(float duration, float magnitude)
    {
        if (!isShaking)
            StartCoroutine(ShakeCoroutine(duration, magnitude * shakeMultiplier));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        isShaking = true;
        originalPosition = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = originalPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
        isShaking = false;
    }
}