using UnityEngine;

/// <summary>
/// Minigame for attaching silencers - drag to barrel, then screw in by dragging down
/// </summary>
public class SilencerMinigame : AttachmentMinigameBase
{
    [Header("Silencer Specific Settings")]
    [SerializeField] private float snapDistance = 0.3f;
    [SerializeField] private float screwDistance = 2f; // Total distance to drag down
    [SerializeField] private float rotationSpeed = 360f; // Degrees per unit dragged
    [SerializeField] private LayerMask raycastLayerMask = -1; // All layers by default

    [Header("Spawn Settings")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(2f, 0f, 0f); // Offset from weapon

    [Header("Camera Zoom Settings")]
    [SerializeField] private float zoomAmount = 0.7f;
    [SerializeField] private float zoomSpeed = 2f;

    private Camera mainCamera;
    private Vector3 dragStartMousePos;
    private Vector3 objectStartPos;
    private float screwProgress = 0f; // 0 to 1
    private bool isSnapped = false;
    private Vector3 lastMousePosition;
    private float totalDragDistance = 0f;
    private Quaternion startRotation;

    // Camera zoom
    private Vector3 originalCameraPosition;
    private bool isZooming = false;
    private bool isZoomingOut = false;
    private Vector3 targetCameraPosition;

    protected override void Awake()
    {
        base.Awake();
        startRotation = transform.rotation;

        // Check for collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = GetComponentInChildren<Collider>();
        }

        if (col == null)
        {
            Debug.LogWarning("SilencerMinigame: No collider found! Adding BoxCollider...");
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            // Try to auto-size it
            Renderer rend = GetComponent<Renderer>();
            if (rend == null) rend = GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                boxCol.size = rend.bounds.size;
                boxCol.center = rend.bounds.center - transform.position;
            }
        }
        else
        {
            Debug.Log($"SilencerMinigame: Collider found - {col.GetType().Name}");
        }
    }

    public override void StartMinigame()
    {
        base.StartMinigame();

        Debug.Log($"StartMinigame called. minigameCamera is: {(minigameCamera != null ? minigameCamera.name : "NULL")}");

        // Use the minigame camera if provided, otherwise fallback to main camera
        mainCamera = minigameCamera != null ? minigameCamera : Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("SilencerMinigame: No camera found! Make sure preview camera is assigned or main camera exists.");
            return;
        }

        // Store original camera position
        originalCameraPosition = mainCamera.transform.position;

        Debug.Log($"SilencerMinigame: Using camera: {mainCamera.name}");
        Debug.Log($"Original camera position: {originalCameraPosition}");

        // Position the silencer next to the weapon
        if (targetSocket != null)
        {
            transform.position = targetSocket.position + spawnOffset;
            transform.rotation = targetSocket.rotation;
            startRotation = transform.rotation;
        }

        SetColor(normalColor);
        Debug.Log("Silencer minigame started. Drag the silencer to the barrel, then drag down to screw it in.");
    }

    protected override void Update()
    {
        base.Update();

        if (isComplete) return;

        // Handle camera zooming
        if (isZooming || isZoomingOut)
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

        if (!isSnapped)
        {
            HandleDragging();
        }
        else
        {
            HandleScrewing();
        }
    }

    void HandleDragging()
    {
        // Start dragging
        if (Input.GetMouseButtonDown(0))
        {
            // Check if clicking on UI
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("Clicked on UI, ignoring");
                return;
            }

            if (mainCamera == null)
            {
                Debug.LogError("mainCamera is NULL!");
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);

            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, raycastLayerMask);

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                {
                    isDragging = true;
                    dragStartMousePos = Input.mousePosition;
                    objectStartPos = transform.position;
                    Debug.Log("Started dragging silencer!");
                    break;
                }
            }
        }

        // Continue dragging
        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 currentMousePos = Input.mousePosition;
            Vector3 mouseDelta = currentMousePos - dragStartMousePos;

            // Convert screen space delta to world space
            Vector3 worldDelta = mainCamera.ScreenToWorldPoint(new Vector3(mouseDelta.x, mouseDelta.y, mainCamera.WorldToScreenPoint(objectStartPos).z))
                                - mainCamera.ScreenToWorldPoint(new Vector3(0, 0, mainCamera.WorldToScreenPoint(objectStartPos).z));

            transform.position = objectStartPos + worldDelta;

            // Check if near socket
            if (IsNearSocket(snapDistance))
            {
                SetColor(validColor);
            }
            else
            {
                SetColor(normalColor);
            }
        }

        // Release drag
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;

            // Check if close enough to snap
            if (IsNearSocket(snapDistance))
            {
                SnapToSocket();
            }
            else
            {
                SetColor(normalColor);
                Debug.Log("Not close enough to socket. Try again.");
            }
        }
    }

    void SnapToSocket()
    {
        isSnapped = true;
        transform.position = targetSocket.position;
        transform.rotation = targetSocket.rotation;
        startRotation = transform.rotation;
        SetColor(validColor);
        lastMousePosition = Input.mousePosition;

        // Start zooming camera towards silencer
        if (mainCamera != null)
        {
            Vector3 directionToSilencer = (transform.position - mainCamera.transform.position).normalized;
            float distanceToSilencer = Vector3.Distance(mainCamera.transform.position, transform.position);
            targetCameraPosition = mainCamera.transform.position + (directionToSilencer * distanceToSilencer * zoomAmount);
            isZooming = true;

            Debug.Log($"Camera zooming from {mainCamera.transform.position} to {targetCameraPosition}");
        }

        Debug.Log("Silencer snapped! Now drag DOWN while holding left click to screw it in.");
    }

    void HandleScrewing()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 currentMousePos = Input.mousePosition;
            Vector3 mouseDelta = currentMousePos - lastMousePosition;

            // Only count downward movement
            float downwardMovement = -mouseDelta.y / Screen.height * 10f; // Scale factor for sensitivity

            if (downwardMovement > 0)
            {
                totalDragDistance += downwardMovement;
                screwProgress = Mathf.Clamp01(totalDragDistance / screwDistance);

                // Rotate as we screw in - rotate around the local forward axis (Z-axis)
                float rotationAmount = downwardMovement * rotationSpeed;
                transform.Rotate(Vector3.right, rotationAmount, Space.Self);

                // Visual feedback based on progress
                Color progressColor = Color.Lerp(validColor, Color.cyan, screwProgress);
                SetColor(progressColor);

                // Check if complete
                if (screwProgress >= 1f)
                {
                    Debug.Log("Silencer attached successfully!");
                    CompleteMinigame();
                }
            }

            lastMousePosition = currentMousePos;
        }
    }

    protected override void CompleteMinigame()
    {
        // Final position and rotation
        transform.position = targetSocket.position;
        transform.rotation = targetSocket.rotation;
        SetColor(Color.green);

        // Zoom camera back out
        if (mainCamera != null)
        {
            targetCameraPosition = originalCameraPosition;
            isZoomingOut = true;
            Debug.Log($"Camera zooming back to {originalCameraPosition}");
        }

        StartCoroutine(CompleteAfterZoomOut());
    }

    private System.Collections.IEnumerator CompleteAfterZoomOut()
    {
        // Wait for camera to finish zooming out
        while (isZoomingOut)
        {
            yield return null;
        }

        // Now actually complete the minigame
        base.CompleteMinigame();
    }

    void OnDrawGizmos()
    {
        if (targetSocket != null)
        {
            // Draw snap radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetSocket.position, snapDistance);
        }
    }
}