using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Minigame for REMOVING magazine attachments - just pull out to complete
/// NOTE: No camera zoom for magazine removal (it's easy to see without zooming)
/// NOTE: Auto-completes after pulling out (no need to drag away)
/// </summary>
public class MagazineRemovalMinigame : AttachmentMinigameBase
{
    [Header("Removal Settings")]
    [SerializeField] private float pullDistance = 0.05f; // REDUCED - magazines don't need to be pulled far
    [SerializeField] private float pullSpeed = 2f;
    [SerializeField] private Vector3 pullDirection = Vector3.down; // Pull down

    [Header("Camera Zoom Settings")]
    [SerializeField] private float zoomAmount = 0.7f;
    [SerializeField] private float zoomSpeed = 2f;

    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private bool isZooming = false;
    private bool isZoomingOut = false;
    private Vector3 targetCameraPosition;

    private enum RemovalState { PullingOut, Complete }
    private RemovalState currentState = RemovalState.PullingOut;

    private bool isPulling = false;
    private float pullProgress = 0f;
    private Vector3 pullStartPosition;
    private Vector3 pullTargetPosition;

    private List<GameObject> weaponPartsToReEnable = new List<GameObject>();
    private Dictionary<GameObject, Vector3> partOriginalLocalPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Quaternion> partOriginalLocalRotations = new Dictionary<GameObject, Quaternion>();
    private Vector3 socketWorldPosition;

    protected override void Awake()
    {
        base.Awake();

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
        }
    }

    public override void StartMinigame()
    {
        base.StartMinigame();

        mainCamera = minigameCamera != null ? minigameCamera : Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("MagazineRemovalMinigame: No camera found!");
            return;
        }

        originalCameraPosition = mainCamera.transform.position;

        if (targetSocket != null)
        {
            socketWorldPosition = targetSocket.position;
        }

        currentState = RemovalState.PullingOut;

        // Calculate pull positions
        Vector3 worldPullDirection = transform.TransformDirection(pullDirection);
        pullStartPosition = transform.position;
        pullTargetPosition = pullStartPosition + (worldPullDirection * pullDistance);

        // No camera zoom for magazine removal - it's easy to see without zooming
        // ZoomToMagazine();

        Debug.Log("Magazine removal started. Click to pull out the magazine.");
    }

    /// <summary>
    /// Set weapon parts that will be re-enabled when magazine is removed
    /// </summary>
    public void SetWeaponPartsToReEnable(Transform weaponTransform, List<string> partPaths)
    {
        weaponPartsToReEnable.Clear();
        partOriginalLocalPositions.Clear();
        partOriginalLocalRotations.Clear();

        if (partPaths == null || partPaths.Count == 0 || weaponTransform == null)
            return;

        Debug.Log($"=== SetWeaponPartsToReEnable - Weapon Root: {weaponTransform.name} ===");

        foreach (var partPath in partPaths)
        {
            if (string.IsNullOrEmpty(partPath))
                continue;

            Transform partTransform = weaponTransform.Find(partPath);
            if (partTransform == null)
                partTransform = FindChildByName(weaponTransform, partPath);

            if (partTransform != null)
            {
                weaponPartsToReEnable.Add(partTransform.gameObject);

                // Store original local position and rotation
                partOriginalLocalPositions[partTransform.gameObject] = partTransform.localPosition;
                partOriginalLocalRotations[partTransform.gameObject] = partTransform.localRotation;

                Debug.Log($"  Part: {partTransform.name}");
                Debug.Log($"    Parent: {partTransform.parent.name}");
                Debug.Log($"    Local Pos: {partTransform.localPosition}");
                Debug.Log($"    World Pos: {partTransform.position}");
                Debug.Log($"    Active: {partTransform.gameObject.activeSelf}");
            }
        }
    }

    private Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            Transform result = FindChildByName(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    private void ZoomToMagazine()
    {
        if (mainCamera == null || targetSocket == null) return;

        Vector3 directionToMagazine = (transform.position - mainCamera.transform.position).normalized;
        float distanceToMagazine = Vector3.Distance(mainCamera.transform.position, transform.position);
        targetCameraPosition = mainCamera.transform.position + (directionToMagazine * distanceToMagazine * zoomAmount);
        isZooming = true;
    }

    protected override void Update()
    {
        base.Update();

        if (isComplete) return;

        // Handle camera zoom
        if ((isZooming || isZoomingOut) && mainCamera != null)
        {
            mainCamera.transform.position = Vector3.Lerp(
                mainCamera.transform.position,
                targetCameraPosition,
                Time.deltaTime * zoomSpeed
            );

            if (Vector3.Distance(mainCamera.transform.position, targetCameraPosition) < 0.01f)
            {
                mainCamera.transform.position = targetCameraPosition;
                isZooming = false;
                isZoomingOut = false;
            }
        }

        // Animate pulling
        if (currentState == RemovalState.PullingOut && isPulling)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                pullTargetPosition,
                Time.deltaTime * pullSpeed
            );

            pullProgress = 1f - (Vector3.Distance(transform.position, pullTargetPosition) / pullDistance);
            Color pullColor = Color.Lerp(Color.yellow, Color.green, pullProgress);
            SetColor(pullColor);

            if (Vector3.Distance(transform.position, pullTargetPosition) < 0.01f)
            {
                transform.position = pullTargetPosition;
                isPulling = false;

                // Magazine removal is complete after pulling out - no need to drag away
                Debug.Log("Magazine pulled out! Removal complete!");
                CompleteMinigame();
            }
        }

        switch (currentState)
        {
            case RemovalState.PullingOut:
                HandlePullingOut();
                break;
        }
    }

    void HandlePullingOut()
    {
        if (Input.GetMouseButtonDown(0) && !isPulling)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                {
                    Debug.Log("Starting pull out animation");
                    isPulling = true;
                    pullProgress = 0f;
                    break;
                }
            }
        }
    }

    protected override void CompleteMinigame()
    {
        SetColor(Color.green);

        Debug.Log("=== MagazineRemovalMinigame.CompleteMinigame ===");
        Debug.Log($"Attachment GameObject: {gameObject.name} at position: {transform.position}");
        Debug.Log($"Attachment parent: {(transform.parent != null ? transform.parent.name : "NULL")}");

        // Re-enable weapon parts (default magazine) and restore original positions
        if (weaponPartsToReEnable != null && weaponPartsToReEnable.Count > 0)
        {
            Debug.Log($"Re-enabling {weaponPartsToReEnable.Count} weapon part(s)");
            foreach (var part in weaponPartsToReEnable)
            {
                if (part != null)
                {
                    Debug.Log($"  Part: {part.name}");
                    Debug.Log($"    Current Parent: {(part.transform.parent != null ? part.transform.parent.name : "NULL")}");
                    Debug.Log($"    Current Local Pos: {part.transform.localPosition}");
                    Debug.Log($"    Current World Pos: {part.transform.position}");

                    // Restore original local position and rotation BEFORE re-enabling
                    if (partOriginalLocalPositions.ContainsKey(part))
                    {
                        Debug.Log($"    Restoring to Local Pos: {partOriginalLocalPositions[part]}");
                        part.transform.localPosition = partOriginalLocalPositions[part];
                        Debug.Log($"    After restore Local Pos: {part.transform.localPosition}");
                    }
                    else
                    {
                        Debug.LogWarning($"    NO ORIGINAL POSITION STORED!");
                    }

                    if (partOriginalLocalRotations.ContainsKey(part))
                    {
                        part.transform.localRotation = partOriginalLocalRotations[part];
                    }

                    // Now re-enable the part
                    part.SetActive(true);
                    Debug.Log($"    Re-enabled! Final World Pos: {part.transform.position}");
                }
                else
                {
                    Debug.LogError("  Part is NULL!");
                }
            }
        }
        else
        {
            Debug.LogWarning("No weapon parts to re-enable!");
        }

        // No camera zoom for magazine removal - complete immediately
        base.CompleteMinigame();
    }

    public override void CancelMinigame()
    {
        if (mainCamera != null)
        {
            mainCamera.transform.position = originalCameraPosition;
            isZooming = false;
            isZoomingOut = false;
        }

        base.CancelMinigame();
    }

    void OnDrawGizmos()
    {
        if (targetSocket != null)
        {
            // Draw pull direction
            Gizmos.color = Color.blue;
            Vector3 worldPullDir = transform.TransformDirection(pullDirection);
            Gizmos.DrawRay(transform.position, worldPullDir * pullDistance);
        }
    }
}