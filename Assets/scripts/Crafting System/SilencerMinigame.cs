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

    private Camera mainCamera;
    private Vector3 dragStartMousePos;
    private Vector3 objectStartPos;
    private float screwProgress = 0f; // 0 to 1
    private bool isSnapped = false;
    private Vector3 lastMousePosition;
    private float totalDragDistance = 0f;
    private Quaternion startRotation;

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

        Debug.Log($"SilencerMinigame: Using camera: {mainCamera.name}");
        Debug.Log($"mainCamera is now: {(mainCamera != null ? mainCamera.name : "NULL")}");

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

        // Debug input detection
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"LEFT CLICK DETECTED - isSnapped: {isSnapped}, isDragging: {isDragging}");
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
        // Debug to see if this method is even being called
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("HandleDragging: Left click detected!");
        }

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

            Debug.Log("About to do raycast...");

            if (mainCamera == null)
            {
                Debug.LogError("mainCamera is NULL!");
                return;
            }

            Debug.Log($"Using camera: {mainCamera.name}");
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);
            Debug.Log($"Ray origin: {ray.origin}, direction: {ray.direction}");

            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, raycastLayerMask);
            Debug.Log($"Raycast found {hits.Length} hits");

            foreach (var hit in hits)
            {
                Debug.Log($"Raycast hit: {hit.collider.gameObject.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");

                if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                {
                    isDragging = true;
                    dragStartMousePos = Input.mousePosition;
                    objectStartPos = transform.position;
                    Debug.Log("Started dragging silencer!");
                    break;
                }
            }

            if (!isDragging && hits.Length > 0)
            {
                Debug.Log($"Clicked something, but not the silencer. Clicked: {hits[0].collider.gameObject.name}");
            }
            else if (!isDragging)
            {
                Debug.Log("Raycast hit nothing");
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

                Debug.Log($"Screwing progress: {screwProgress * 100f:F0}%");

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