using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Minigame for REMOVING barrel attachments - unscrew and drag away
/// </summary>
public class BarrelRemovalMinigame : AttachmentMinigameBase
{
    [Header("Removal Settings")]
    [SerializeField] private float unscrewDistance = 2f;
    [SerializeField] private float maxUnscrewPerPull = 0.25f;
    [SerializeField] private float unscrewRotationSpeed = 180f;
    [SerializeField] private float unscrewMoveDistance = 0.05f;
    [SerializeField] private Vector3 unscrewMoveDirection = Vector3.forward;
    [SerializeField] private float minDistanceToMoveAway = 0.1f;

    [Header("Camera Zoom Settings")]
    [SerializeField] private float zoomAmount = 0.7f;
    [SerializeField] private float zoomSpeed = 2f;

    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private bool isZooming = false;
    private bool isZoomingOut = false;
    private Vector3 targetCameraPosition;

    private enum RemovalState { Unscrewing, MovingAway, Complete }
    private RemovalState currentState = RemovalState.Unscrewing;

    private float unscrewProgress = 0f;
    private float totalUnscrewDistance = 0f;
    private float currentUnscrewPullDistance = 0f;
    private Vector3 lastMousePosition;

    private List<GameObject> weaponPartsToReEnable = new List<GameObject>();
    private Vector3 socketWorldPosition;
    private bool isDraggingPart = false;
    private Vector3 grabbedPartDragStart;
    private Vector3 grabbedPartStartPos;

    protected override void Awake()
    {
        base.Awake();

        // Add collider if needed
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
            Debug.LogError("BarrelRemovalMinigame: No camera found!");
            return;
        }

        originalCameraPosition = mainCamera.transform.position;

        if (targetSocket != null)
        {
            socketWorldPosition = targetSocket.position;
        }

        currentState = RemovalState.Unscrewing;

        // Zoom camera to barrel
        ZoomToBarrel();

        Debug.Log("Barrel removal started. Drag UP to unscrew the barrel.");
    }

    /// <summary>
    /// Set weapon parts that will be re-enabled when barrel is removed
    /// </summary>
    public void SetWeaponPartsToReEnable(Transform weaponTransform, List<string> partPaths)
    {
        weaponPartsToReEnable.Clear();

        if (partPaths == null || partPaths.Count == 0 || weaponTransform == null)
            return;

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
                Debug.Log($"Will re-enable: {partTransform.name}");
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

    private void ZoomToBarrel()
    {
        if (mainCamera == null || targetSocket == null) return;

        Vector3 directionToBarrel = (targetSocket.position - mainCamera.transform.position).normalized;
        float distanceToBarrel = Vector3.Distance(mainCamera.transform.position, targetSocket.position);
        targetCameraPosition = mainCamera.transform.position + (directionToBarrel * distanceToBarrel * zoomAmount);
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

        // Animate barrel moving out as it's unscrewed
        if (currentState == RemovalState.Unscrewing && unscrewProgress > 0f)
        {
            Vector3 moveOffset = transform.TransformDirection(unscrewMoveDirection) * unscrewMoveDistance * unscrewProgress;
            transform.position = Vector3.Lerp(transform.position, targetSocket.position + moveOffset, Time.deltaTime * 5f);
        }

        switch (currentState)
        {
            case RemovalState.Unscrewing:
                HandleUnscrewing();
                break;
            case RemovalState.MovingAway:
                HandleMovingAway();
                break;
        }
    }

    void HandleUnscrewing()
    {
        // Start a new pull
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;
            currentUnscrewPullDistance = 0f;
            Debug.Log("Started new unscrew pull");
        }

        // Continue pulling UP
        if (Input.GetMouseButton(0))
        {
            Vector3 currentMousePos = Input.mousePosition;
            Vector3 mouseDelta = currentMousePos - lastMousePosition;

            // Only count UPWARD movement
            float upwardMovement = mouseDelta.y / Screen.height * 10f;

            if (upwardMovement > 0)
            {
                float maxDistanceThisPull = unscrewDistance * maxUnscrewPerPull;

                if (currentUnscrewPullDistance < maxDistanceThisPull)
                {
                    float distanceToAdd = Mathf.Min(upwardMovement, maxDistanceThisPull - currentUnscrewPullDistance);
                    currentUnscrewPullDistance += distanceToAdd;
                    totalUnscrewDistance += distanceToAdd;

                    unscrewProgress = Mathf.Clamp01(totalUnscrewDistance / unscrewDistance);

                    // Rotate as we unscrew
                    float rotationAmount = distanceToAdd * unscrewRotationSpeed;
                    transform.Rotate(Vector3.forward, -rotationAmount, Space.Self);

                    // Visual feedback
                    Color progressColor = Color.Lerp(Color.red, Color.green, unscrewProgress);
                    SetColor(progressColor);

                    Debug.Log($"Unscrew progress: {unscrewProgress * 100f:F0}%");
                }
                else
                {
                    Debug.Log($"Unscrew pull limit reached! Release and pull again");
                }

                // Check if complete
                if (unscrewProgress >= 1f)
                {
                    Debug.Log("Barrel unscrewed! Now drag it away from the weapon.");
                    currentState = RemovalState.MovingAway;
                    SetColor(Color.yellow);
                }
            }

            lastMousePosition = currentMousePos;
        }

        // Released mouse - reset for next pull
        if (Input.GetMouseButtonUp(0))
        {
            if (currentUnscrewPullDistance > 0)
            {
                Debug.Log($"Unscrew pull complete: {currentUnscrewPullDistance:F2} units. Total progress: {unscrewProgress * 100f:F0}%");
            }
            currentUnscrewPullDistance = 0f;
        }
    }

    void HandleMovingAway()
    {
        // Start dragging
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                {
                    isDraggingPart = true;
                    grabbedPartDragStart = Input.mousePosition;
                    grabbedPartStartPos = transform.position;
                    Debug.Log($"Grabbed barrel: {gameObject.name}");
                    break;
                }
            }
        }

        // Continue dragging
        if (isDraggingPart && Input.GetMouseButton(0))
        {
            Vector3 currentMousePos = Input.mousePosition;
            Vector3 mouseDelta = currentMousePos - grabbedPartDragStart;

            Vector3 worldDelta = mainCamera.ScreenToWorldPoint(new Vector3(mouseDelta.x, mouseDelta.y, mainCamera.WorldToScreenPoint(grabbedPartStartPos).z))
                                - mainCamera.ScreenToWorldPoint(new Vector3(0, 0, mainCamera.WorldToScreenPoint(grabbedPartStartPos).z));

            transform.position = grabbedPartStartPos + worldDelta;

            // Check distance from socket
            float distanceFromSocket = Vector3.Distance(transform.position, socketWorldPosition);

            // Visual feedback
            Color feedbackColor = distanceFromSocket >= minDistanceToMoveAway ? Color.green : Color.yellow;
            SetColor(feedbackColor);
        }

        // Release
        if (Input.GetMouseButtonUp(0) && isDraggingPart)
        {
            float distanceFromSocket = Vector3.Distance(transform.position, socketWorldPosition);

            if (distanceFromSocket >= minDistanceToMoveAway)
            {
                Debug.Log($"Barrel moved far enough away. Removal complete!");
                CompleteMinigame();
            }
            else
            {
                Debug.Log($"Not far enough away. Move it further!");
            }

            isDraggingPart = false;
        }
    }

    protected override void CompleteMinigame()
    {
        SetColor(Color.green);

        // Re-enable weapon parts (default barrel)
        if (weaponPartsToReEnable != null && weaponPartsToReEnable.Count > 0)
        {
            Debug.Log($"Re-enabling {weaponPartsToReEnable.Count} weapon part(s)");
            foreach (var part in weaponPartsToReEnable)
            {
                if (part != null)
                {
                    Debug.Log($"  Re-enabling: {part.name}");
                    part.SetActive(true);
                }
            }
        }

        // Zoom camera back
        if (mainCamera != null)
        {
            targetCameraPosition = originalCameraPosition;
            isZoomingOut = true;
        }

        StartCoroutine(CompleteAfterZoomOut());
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

    private System.Collections.IEnumerator CompleteAfterZoomOut()
    {
        while (isZoomingOut)
        {
            yield return null;
        }

        base.CompleteMinigame();
    }

    void OnDrawGizmos()
    {
        if (targetSocket != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetSocket.position, minDistanceToMoveAway);

            Gizmos.color = Color.red;
            Vector3 moveOffset = transform.TransformDirection(unscrewMoveDirection) * unscrewMoveDistance;
            Gizmos.DrawLine(transform.position, transform.position + moveOffset);
        }
    }
}
