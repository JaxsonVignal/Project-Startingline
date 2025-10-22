using UnityEngine;

/// <summary>
/// Minigame for attaching scopes - drag to sight socket, then screw in front and back screws
/// </summary>
public class ScopeMinigame : AttachmentMinigameBase
{
    [Header("Scope Specific Settings")]
    [SerializeField] private float snapDistance = 0.3f;
    [SerializeField] private float rotationsRequired = 2f; // Number of full circles needed per screw
    [SerializeField] private float rotationSpeed = 360f; // Visual rotation speed
    [SerializeField] private float circleDetectionRadius = 50f; // Screen space radius for circle detection

    [Header("Spawn Settings")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(1f, 0f, 0f);

    [Header("Screw Positions (relative to scope)")]
    [SerializeField] private Vector3 frontScrewLocalPos = new Vector3(-0.05f, 0f, -0.05f); // Swapped with back
    [SerializeField] private Vector3 backScrewLocalPos = new Vector3(0.05f, 0f, -0.05f);   // Swapped with front
    [SerializeField] private float screwRadius = 0.015f; // How close mouse needs to be to screw (halved from 0.03)

    private Camera mainCamera;
    private Vector3 dragStartMousePos;
    private Vector3 objectStartPos;
    private bool isSnapped = false;
    private Vector3 lastMousePosition;

    // Screw states
    private enum ScrewState { None, FrontScrew, BackScrew, Complete }
    private ScrewState currentState = ScrewState.None;
    private float frontScrewProgress = 0f; // 0 to 1
    private float backScrewProgress = 0f; // 0 to 1

    // Circular motion tracking
    private Vector2 screwCenterScreenPos;
    private float totalRotation = 0f; // Total degrees rotated
    private Vector2 lastMouseAngle;
    private bool isRotating = false;

    [Header("Camera Zoom Settings")]
    [SerializeField] private float zoomAmount = 0.5f; // How much closer to zoom (multiplier)
    [SerializeField] private float zoomSpeed = 2f; // Speed of zoom transition

    private Vector3 originalCameraPosition;
    private bool isZooming = false;
    private bool isZoomingOut = false;
    private Vector3 targetCameraPosition;

    protected override void Awake()
    {
        base.Awake();

        // Check for collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = GetComponentInChildren<Collider>();
        }

        if (col == null)
        {
            Debug.LogWarning("ScopeMinigame: No collider found! Adding BoxCollider...");
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            Renderer rend = GetComponent<Renderer>();
            if (rend == null) rend = GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                boxCol.size = rend.bounds.size;
                boxCol.center = rend.bounds.center - transform.position;
            }
        }
    }

    public override void StartMinigame()
    {
        base.StartMinigame();

        mainCamera = minigameCamera != null ? minigameCamera : Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("ScopeMinigame: No camera found!");
            return;
        }

        // Store the original camera position at the START of the minigame
        originalCameraPosition = mainCamera.transform.position;
        Debug.Log($"ScopeMinigame: Using camera: {mainCamera.name}");
        Debug.Log($"Original camera position saved: {originalCameraPosition}");

        // Position the scope next to the weapon (same as silencer)
        if (targetSocket != null)
        {
            // Use the socket's local right direction for offset (relative to weapon)
            Vector3 worldOffset = targetSocket.TransformDirection(spawnOffset);
            transform.position = targetSocket.position + worldOffset;
            transform.rotation = targetSocket.rotation;

            Debug.Log($"Scope spawned at: {transform.position}");
        }
        else
        {
            Debug.LogError("Target socket is null!");
        }

        CreateScrewVisuals();
        SetColor(normalColor);

        Debug.Log("Scope minigame started. Drag the scope to the sight rail.");
    }

    void CreateScrewVisuals()
    {
        // Create visual indicators for screws
        frontScrewVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        frontScrewVisual.transform.SetParent(transform);
        frontScrewVisual.transform.localPosition = frontScrewLocalPos;
        frontScrewVisual.transform.localRotation = Quaternion.Euler(90, 0, 0); // Rotate to stick out sideways
        frontScrewVisual.transform.localScale = new Vector3(screwRadius * 2, 0.01f, screwRadius * 2);
        Destroy(frontScrewVisual.GetComponent<Collider>()); // Remove collider

        backScrewVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        backScrewVisual.transform.SetParent(transform);
        backScrewVisual.transform.localPosition = backScrewLocalPos;
        backScrewVisual.transform.localRotation = Quaternion.Euler(90, 0, 0); // Rotate to stick out sideways
        backScrewVisual.transform.localScale = new Vector3(screwRadius * 2, 0.01f, screwRadius * 2);
        Destroy(backScrewVisual.GetComponent<Collider>());

        // Color them red initially
        SetScrewColor(frontScrewVisual, Color.red);
        SetScrewColor(backScrewVisual, Color.red);
    }

    void SetScrewColor(GameObject screw, Color color)
    {
        if (screw != null)
        {
            Renderer rend = screw.GetComponent<Renderer>();
            if (rend != null && rend.material != null)
            {
                rend.material.color = color;
            }
        }
    }

    // Visual screw objects
    private GameObject frontScrewVisual;
    private GameObject backScrewVisual;

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

            // Check if zoom is complete
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
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                {
                    isDragging = true;
                    dragStartMousePos = Input.mousePosition;
                    objectStartPos = transform.position;
                    Debug.Log("Started dragging scope");
                    break;
                }
            }
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 currentMousePos = Input.mousePosition;
            Vector3 mouseDelta = currentMousePos - dragStartMousePos;

            Vector3 worldDelta = mainCamera.ScreenToWorldPoint(new Vector3(mouseDelta.x, mouseDelta.y, mainCamera.WorldToScreenPoint(objectStartPos).z))
                                - mainCamera.ScreenToWorldPoint(new Vector3(0, 0, mainCamera.WorldToScreenPoint(objectStartPos).z));

            transform.position = objectStartPos + worldDelta;

            if (IsNearSocket(snapDistance))
            {
                SetColor(validColor);
            }
            else
            {
                SetColor(normalColor);
            }
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;

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
        SetColor(validColor);
        lastMousePosition = Input.mousePosition;
        currentState = ScrewState.FrontScrew;

        Debug.Log("Scope snapped! Now screw in the FRONT screw (red indicator) by dragging DOWN.");
    }

    void HandleScrewing()
    {
        // Check which screw the mouse is over
        Vector3 mousePos = Input.mousePosition;

        Vector3 frontScrewWorldPos = transform.TransformPoint(frontScrewLocalPos);
        Vector3 backScrewWorldPos = transform.TransformPoint(backScrewLocalPos);

        Vector3 frontScrewScreenPos = mainCamera.WorldToScreenPoint(frontScrewWorldPos);
        Vector3 backScrewScreenPos = mainCamera.WorldToScreenPoint(backScrewWorldPos);

        float distToFront = Vector2.Distance(new Vector2(mousePos.x, mousePos.y), new Vector2(frontScrewScreenPos.x, frontScrewScreenPos.y));
        float distToBack = Vector2.Distance(new Vector2(mousePos.x, mousePos.y), new Vector2(backScrewScreenPos.x, backScrewScreenPos.y));

        float screenScrewRadius = circleDetectionRadius;

        // Highlight screws based on mouse position
        if (frontScrewProgress < 1f && distToFront < screenScrewRadius)
        {
            SetScrewColor(frontScrewVisual, Color.yellow);
        }
        else if (frontScrewProgress < 1f)
        {
            SetScrewColor(frontScrewVisual, Color.red);
        }

        if (backScrewProgress < 1f && distToBack < screenScrewRadius)
        {
            SetScrewColor(backScrewVisual, Color.yellow);
        }
        else if (backScrewProgress < 1f)
        {
            SetScrewColor(backScrewVisual, Color.red);
        }

        // Handle screwing
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("CLICK DETECTED");
            Debug.Log("distToFront: " + distToFront + " distToBack: " + distToBack + " radius: " + screenScrewRadius);
            Debug.Log("frontProgress: " + frontScrewProgress + " backProgress: " + backScrewProgress);

            // Determine which screw to work on
            if (frontScrewProgress < 1f && distToFront < screenScrewRadius)
            {
                Debug.Log("STARTING FRONT SCREW");
                currentState = ScrewState.FrontScrew;
                screwCenterScreenPos = new Vector2(frontScrewScreenPos.x, frontScrewScreenPos.y);
                StartCircularMotion(mousePos);
            }
            else if (backScrewProgress < 1f && distToBack < screenScrewRadius)
            {
                Debug.Log("STARTING BACK SCREW");
                currentState = ScrewState.BackScrew;
                screwCenterScreenPos = new Vector2(backScrewScreenPos.x, backScrewScreenPos.y);
                StartCircularMotion(mousePos);
            }
            else
            {
                Debug.Log("Click not on any screw");
            }
        }

        if (Input.GetMouseButton(0) && isRotating && currentState != ScrewState.None && currentState != ScrewState.Complete)
        {
            UpdateCircularMotion(mousePos);
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (isRotating)
            {
                Debug.Log($"Released. Total rotation: {totalRotation:F0} degrees");
            }
            isRotating = false;
        }
    }

    void StartCircularMotion(Vector3 mousePos)
    {
        isRotating = true;
        totalRotation = 0f;

        Vector2 mouseVec = new Vector2(mousePos.x, mousePos.y) - screwCenterScreenPos;
        lastMouseAngle = mouseVec.normalized;

        Debug.Log($"StartCircularMotion - Mouse distance from screw center: {mouseVec.magnitude}");
    }

    void UpdateCircularMotion(Vector3 mousePos)
    {
        Debug.Log($"UpdateCircularMotion - currentState: {currentState}");

        Vector2 currentMouseVec = new Vector2(mousePos.x, mousePos.y) - screwCenterScreenPos;
        Vector2 currentMouseAngle = currentMouseVec.normalized;

        // Calculate angle between last position and current position
        float angleDiff = Vector2.SignedAngle(lastMouseAngle, currentMouseAngle);

        // Only count clockwise rotation (negative angle in Unity's 2D space)
        if (angleDiff < 0)
        {
            totalRotation += Mathf.Abs(angleDiff);

            Debug.Log($"Rotation detected: {Mathf.Abs(angleDiff):F1}°, Total: {totalRotation:F1}°, State: {currentState}");

            if (currentState == ScrewState.FrontScrew)
            {
                Debug.Log("Updating FRONT screw");
                float requiredRotation = rotationsRequired * 360f;
                frontScrewProgress = Mathf.Clamp01(totalRotation / requiredRotation);

                Debug.Log($"Front screw progress: {frontScrewProgress * 100f:F1}%");

                // Rotate the screw visual
                frontScrewVisual.transform.Rotate(Vector3.up, Mathf.Abs(angleDiff), Space.Self);

                Color progressColor = Color.Lerp(Color.red, Color.green, frontScrewProgress);
                SetScrewColor(frontScrewVisual, progressColor);

                if (frontScrewProgress >= 1f && !isComplete)
                {
                    Debug.Log("Front screw complete! Now screw in the BACK screw.");
                    currentState = ScrewState.None;
                    isRotating = false;
                }
            }
            else if (currentState == ScrewState.BackScrew)
            {
                Debug.Log("Updating BACK screw");
                float requiredRotation = rotationsRequired * 360f;
                backScrewProgress = Mathf.Clamp01(totalRotation / requiredRotation);

                Debug.Log($"Back screw progress: {backScrewProgress * 100f:F1}%");

                backScrewVisual.transform.Rotate(Vector3.up, Mathf.Abs(angleDiff), Space.Self);

                Color progressColor = Color.Lerp(Color.red, Color.green, backScrewProgress);
                SetScrewColor(backScrewVisual, progressColor);

                if (backScrewProgress >= 1f && !isComplete)
                {
                    Debug.Log("Back screw complete! Scope attached successfully!");
                    currentState = ScrewState.Complete;
                    isRotating = false;
                    CompleteMinigame();
                }
            }
            else
            {
                Debug.Log($"State is {currentState} - not updating any screw");
            }
        }

        lastMouseAngle = currentMouseAngle;
    }

    protected override void CompleteMinigame()
    {
        transform.position = targetSocket.position;
        transform.rotation = targetSocket.rotation;
        SetColor(Color.green);
        SetScrewColor(frontScrewVisual, Color.green);
        SetScrewColor(backScrewVisual, Color.green);

        // Zoom camera back out to original position
        if (mainCamera != null)
        {
            targetCameraPosition = originalCameraPosition;
            isZoomingOut = true;
            Debug.Log($"Camera zooming back to {originalCameraPosition}");
        }

        // Delay the actual completion until camera finishes zooming out
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
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetSocket.position, snapDistance);
        }

        if (isSnapped)
        {
            // Draw screw positions
            Gizmos.color = Color.red;
            Vector3 frontPos = transform.TransformPoint(frontScrewLocalPos);
            Vector3 backPos = transform.TransformPoint(backScrewLocalPos);
            Gizmos.DrawWireSphere(frontPos, screwRadius);
            Gizmos.DrawWireSphere(backPos, screwRadius);
        }
    }
}