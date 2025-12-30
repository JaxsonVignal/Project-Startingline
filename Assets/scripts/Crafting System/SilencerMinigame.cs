using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Minigame for attaching silencers - unscrew old barrel part, drag to barrel, then screw in by dragging down
/// </summary>
public class SilencerMinigame : AttachmentMinigameBase
{
    [Header("Silencer Specific Settings")]
    [SerializeField] private float snapDistance = 0.3f;
    [SerializeField] private float screwDistance = 2f; // Total distance to drag down
    [SerializeField] private float maxDragPerPull = 0.25f; // Max % of screwDistance per pull (0.25 = 25%)
    [SerializeField] private float rotationSpeed = 180f; // Degrees per unit dragged
    [SerializeField] private LayerMask raycastLayerMask = -1; // All layers by default

    [Header("Spawn Settings")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0f, 0.5f); // Offset from weapon

    [Header("Camera Zoom Settings")]
    [SerializeField] private float zoomAmount = 0.7f;
    [SerializeField] private float zoomSpeed = 2f;

    [Header("Unscrew Settings")]
    [SerializeField] private float unscrewDistance = 2f; // Total distance to drag up to unscrew
    [SerializeField] private float maxUnscrewPerPull = 0.25f; // Max % of unscrewDistance per pull
    [SerializeField] private float unscrewRotationSpeed = 180f; // Degrees per unit dragged
    [SerializeField] private float unscrewMoveDistance = 0.05f; // How far the part moves away from weapon when unscrewed
    [SerializeField] private Vector3 unscrewMoveDirection = Vector3.forward; // Direction to move the part (in local space)
    [SerializeField] private float minDistanceToMoveAway = 0.1f; // Minimum distance part must be moved away before proceeding

    [Header("Screw-In Settings")]
    [SerializeField] private float screwInStartDistance = 0.01f; // How far away from socket the silencer starts when screwing in
    [SerializeField] private Vector3 screwInStartDirection = Vector3.forward; // Direction to offset the silencer (in local space)

    private Camera mainCamera;
    private Vector3 dragStartMousePos;
    private Vector3 objectStartPos;
    private float screwProgress = 0f; // 0 to 1
    private bool isSnapped = false;
    private Vector3 lastMousePosition;
    private float totalDragDistance = 0f;
    private float currentPullDistance = 0f; // Distance dragged in current pull
    private Quaternion startRotation;

    // Camera zoom
    private Vector3 originalCameraPosition;
    private bool isZooming = false;
    private bool isZoomingOut = false;
    private Vector3 targetCameraPosition;

    // Weapon parts to disable
    private List<GameObject> weaponPartsToDisable = new List<GameObject>();
    private List<Vector3> partOriginalPositions = new List<Vector3>(); // Store original positions
    private List<Vector3> partTargetPositions = new List<Vector3>(); // Store target positions when unscrewed

    // Part grabbing
    private GameObject grabbedPart;
    private Vector3 grabbedPartDragStart;
    private Vector3 grabbedPartStartPos;
    private bool isDraggingPart = false;
    private Vector3 socketWorldPosition; // Store socket position for distance checking

    // Screw-in positioning
    private Vector3 screwInStartPosition; // Where the silencer starts when screwing in
    private Vector3 screwInTargetPosition; // Final socket position

    // Unscrew phase tracking
    private enum SilencerState { UnscrewingOldPart, MovingPartAway, Dragging, Screwing, Complete }
    private SilencerState currentState = SilencerState.UnscrewingOldPart;
    private float unscrewProgress = 0f;
    private float totalUnscrewDistance = 0f;
    private float currentUnscrewPullDistance = 0f;

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

        // Position the silencer next to the weapon (hidden initially if we need to unscrew first)
        if (targetSocket != null)
        {
            socketWorldPosition = targetSocket.position;
            transform.position = targetSocket.position + spawnOffset;
            transform.rotation = targetSocket.rotation;
            startRotation = transform.rotation;
        }

        // Check if there are parts to unscrew first
        if (weaponPartsToDisable != null && weaponPartsToDisable.Count > 0)
        {
            currentState = SilencerState.UnscrewingOldPart;

            // Hide the new silencer while unscrewing old part
            SetRendererActive(false);

            // Calculate target positions for unscrewing (move parts away)
            CalculateUnscrewTargetPositions();

            // Add colliders to parts so they can be grabbed
            AddCollidersToWeaponParts();

            // Zoom camera to the old barrel part
            ZoomToBarrelPart();

            Debug.Log("Silencer minigame started. First, drag UP to unscrew the old barrel part.");
        }
        else
        {
            // No parts to unscrew, skip to dragging phase
            currentState = SilencerState.Dragging;
            SetColor(normalColor);
            Debug.Log("Silencer minigame started. Drag the silencer to the barrel, then drag down to screw it in.");
        }
    }

    public void SetWeaponPartsToDisable(Transform weaponTransform, List<string> partPaths)
    {
        Debug.Log($"SetWeaponPartsToDisable called with weaponTransform: {(weaponTransform != null ? weaponTransform.name : "NULL")}, partPaths count: {(partPaths != null ? partPaths.Count : 0)}");

        weaponPartsToDisable.Clear();
        partOriginalPositions.Clear();
        partTargetPositions.Clear();

        if (partPaths == null || partPaths.Count == 0 || weaponTransform == null)
        {
            Debug.LogWarning("Part paths list is empty or weapon transform is null");
            return;
        }

        foreach (var partPath in partPaths)
        {
            if (string.IsNullOrEmpty(partPath))
                continue;

            Transform partTransform = weaponTransform.Find(partPath);

            if (partTransform == null)
            {
                Debug.Log($"Could not find '{partPath}' using Find(), searching recursively...");
                partTransform = FindChildByName(weaponTransform, partPath);
            }
            else
            {
                Debug.Log($"Found '{partPath}' using Find()");
            }

            if (partTransform != null)
            {
                weaponPartsToDisable.Add(partTransform.gameObject);
                partOriginalPositions.Add(partTransform.localPosition);
                Debug.Log($"SUCCESS: Silencer will disable '{partTransform.gameObject.name}' at path: {GetFullPath(partTransform)}");
            }
            else
            {
                Debug.LogError($"FAILED: Could not find weapon part to disable: '{partPath}'. Check the name and hierarchy.");
            }
        }

        Debug.Log($"Total weapon parts to disable: {weaponPartsToDisable.Count}");
    }

    private void AddCollidersToWeaponParts()
    {
        foreach (var part in weaponPartsToDisable)
        {
            if (part != null)
            {
                // Check if part already has a collider
                Collider existingCollider = part.GetComponent<Collider>();
                if (existingCollider == null)
                {
                    // Add a box collider for grabbing
                    BoxCollider col = part.AddComponent<BoxCollider>();

                    // Try to size it based on renderer
                    Renderer rend = part.GetComponent<Renderer>();
                    if (rend == null) rend = part.GetComponentInChildren<Renderer>();

                    if (rend != null)
                    {
                        col.size = rend.bounds.size;
                        col.center = rend.bounds.center - part.transform.position;
                    }

                    Debug.Log($"Added collider to weapon part: {part.name}");
                }
            }
        }
    }

    private void CalculateUnscrewTargetPositions()
    {
        partTargetPositions.Clear();

        for (int i = 0; i < weaponPartsToDisable.Count; i++)
        {
            if (weaponPartsToDisable[i] != null)
            {
                // Calculate the target position (move forward in local space)
                Vector3 moveOffset = weaponPartsToDisable[i].transform.TransformDirection(unscrewMoveDirection) * unscrewMoveDistance;
                Vector3 targetWorldPos = weaponPartsToDisable[i].transform.position + moveOffset;

                // Convert back to local position
                Vector3 targetLocalPos;
                if (weaponPartsToDisable[i].transform.parent != null)
                {
                    targetLocalPos = weaponPartsToDisable[i].transform.parent.InverseTransformPoint(targetWorldPos);
                }
                else
                {
                    targetLocalPos = targetWorldPos;
                }

                partTargetPositions.Add(targetLocalPos);
                Debug.Log($"Part '{weaponPartsToDisable[i].name}' will move from {partOriginalPositions[i]} to {targetLocalPos}");
            }
        }
    }

    private Transform FindChildByName(Transform parent, string name)
    {
        Debug.Log($"Searching children of '{parent.name}' for '{name}'");

        foreach (Transform child in parent)
        {
            Debug.Log($"  Checking child: {child.name}");
            if (child.name == name)
            {
                Debug.Log($"  MATCH FOUND: {child.name}");
                return child;
            }

            Transform result = FindChildByName(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    private string GetFullPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }

    private void ZoomToBarrelPart()
    {
        if (mainCamera == null || targetSocket == null) return;

        Vector3 directionToBarrel = (targetSocket.position - mainCamera.transform.position).normalized;
        float distanceToBarrel = Vector3.Distance(mainCamera.transform.position, targetSocket.position);
        targetCameraPosition = mainCamera.transform.position + (directionToBarrel * distanceToBarrel * zoomAmount);
        isZooming = true;

        Debug.Log($"Camera zooming to barrel part: from {mainCamera.transform.position} to {targetCameraPosition}");
    }

    private void SetRendererActive(bool active)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            rend.enabled = active;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (isComplete) return;

        // Handle camera zooming - only if we have valid camera and minigame is active
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

        // Update part positions during unscrewing
        if (currentState == SilencerState.UnscrewingOldPart)
        {
            UpdatePartPositions();
        }

        // Update silencer position during screwing
        if (currentState == SilencerState.Screwing)
        {
            UpdateSilencerScrewInPosition();
        }

        switch (currentState)
        {
            case SilencerState.UnscrewingOldPart:
                HandleUnscrewing();
                break;
            case SilencerState.MovingPartAway:
                HandleMovingPartAway();
                break;
            case SilencerState.Dragging:
                HandleDragging();
                break;
            case SilencerState.Screwing:
                HandleScrewing();
                break;
        }
    }

    void UpdatePartPositions()
    {
        for (int i = 0; i < weaponPartsToDisable.Count; i++)
        {
            if (weaponPartsToDisable[i] != null && i < partOriginalPositions.Count && i < partTargetPositions.Count)
            {
                // Lerp between original and target position based on unscrew progress
                Vector3 currentTargetPos = Vector3.Lerp(partOriginalPositions[i], partTargetPositions[i], unscrewProgress);
                weaponPartsToDisable[i].transform.localPosition = Vector3.Lerp(
                    weaponPartsToDisable[i].transform.localPosition,
                    currentTargetPos,
                    Time.deltaTime * 5f // Smooth movement
                );
            }
        }
    }

    void UpdateSilencerScrewInPosition()
    {
        // Lerp between start position and target position based on screw progress
        Vector3 currentTargetPos = Vector3.Lerp(screwInStartPosition, screwInTargetPosition, screwProgress);
        transform.position = Vector3.Lerp(
            transform.position,
            currentTargetPos,
            Time.deltaTime * 5f // Smooth movement
        );
    }

    void HandleUnscrewing()
    {
        if (weaponPartsToDisable == null || weaponPartsToDisable.Count == 0)
        {
            // No parts to unscrew, move to dragging
            TransitionToDragging();
            return;
        }

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
            float upwardMovement = mouseDelta.y / Screen.height * 10f; // Scale factor for sensitivity

            if (upwardMovement > 0)
            {
                // Calculate max distance allowed for this pull
                float maxDistanceThisPull = unscrewDistance * maxUnscrewPerPull;

                // Check if we've reached the limit for this pull
                if (currentUnscrewPullDistance < maxDistanceThisPull)
                {
                    // Add to current pull distance, but cap it
                    float distanceToAdd = Mathf.Min(upwardMovement, maxDistanceThisPull - currentUnscrewPullDistance);
                    currentUnscrewPullDistance += distanceToAdd;
                    totalUnscrewDistance += distanceToAdd;

                    unscrewProgress = Mathf.Clamp01(totalUnscrewDistance / unscrewDistance);

                    // Rotate the weapon parts as we unscrew
                    foreach (var part in weaponPartsToDisable)
                    {
                        if (part != null)
                        {
                            float rotationAmount = distanceToAdd * unscrewRotationSpeed;
                            part.transform.Rotate(Vector3.forward, -rotationAmount, Space.Self);
                        }
                    }

                    Debug.Log($"Unscrew progress: {unscrewProgress * 100f:F0}%");
                }
                else
                {
                    Debug.Log($"Unscrew pull limit reached! ({currentUnscrewPullDistance}/{maxDistanceThisPull}) - Release and pull again");
                }

                // Check if complete
                if (unscrewProgress >= 1f)
                {
                    Debug.Log("Old barrel part unscrewed! Now grab and move the part away from the weapon.");
                    TransitionToMovingPartAway();
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

    void TransitionToMovingPartAway()
    {
        currentState = SilencerState.MovingPartAway;
        Debug.Log("Transition to moving part away. Click and drag the part to move it out of the way.");
    }

    void HandleMovingPartAway()
    {
        // Start dragging part
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
            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, raycastLayerMask);

            // Check if we hit any weapon parts
            foreach (var hit in hits)
            {
                foreach (var part in weaponPartsToDisable)
                {
                    if (part != null && (hit.collider.gameObject == part || hit.collider.transform.IsChildOf(part.transform)))
                    {
                        grabbedPart = part;
                        isDraggingPart = true;
                        grabbedPartDragStart = Input.mousePosition;
                        grabbedPartStartPos = grabbedPart.transform.position;
                        Debug.Log($"Grabbed weapon part: {grabbedPart.name}");
                        break;
                    }
                }
                if (grabbedPart != null) break;
            }
        }

        // Continue dragging part
        if (isDraggingPart && Input.GetMouseButton(0) && grabbedPart != null)
        {
            Vector3 currentMousePos = Input.mousePosition;
            Vector3 mouseDelta = currentMousePos - grabbedPartDragStart;

            // Convert screen space delta to world space
            Vector3 worldDelta = mainCamera.ScreenToWorldPoint(new Vector3(mouseDelta.x, mouseDelta.y, mainCamera.WorldToScreenPoint(grabbedPartStartPos).z))
                                - mainCamera.ScreenToWorldPoint(new Vector3(0, 0, mainCamera.WorldToScreenPoint(grabbedPartStartPos).z));

            grabbedPart.transform.position = grabbedPartStartPos + worldDelta;

            // Check distance from socket
            float distanceFromSocket = Vector3.Distance(grabbedPart.transform.position, socketWorldPosition);

            // Visual feedback
            Renderer[] renderers = grabbedPart.GetComponentsInChildren<Renderer>();
            Color feedbackColor = distanceFromSocket >= minDistanceToMoveAway ? Color.green : Color.yellow;
            foreach (var rend in renderers)
            {
                if (rend.material != null)
                {
                    rend.material.color = feedbackColor;
                }
            }
        }

        // Release part
        if (Input.GetMouseButtonUp(0) && isDraggingPart)
        {
            if (grabbedPart != null)
            {
                float distanceFromSocket = Vector3.Distance(grabbedPart.transform.position, socketWorldPosition);

                if (distanceFromSocket >= minDistanceToMoveAway)
                {
                    Debug.Log($"Part moved far enough away ({distanceFromSocket:F2}m). Proceeding to attach silencer.");
                    TransitionToDragging();
                }
                else
                {
                    Debug.Log($"Part not far enough away ({distanceFromSocket:F2}m < {minDistanceToMoveAway}m). Move it further!");

                    // Reset color
                    Renderer[] renderers = grabbedPart.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        if (rend.material != null)
                        {
                            rend.material.color = Color.white;
                        }
                    }
                }
            }

            isDraggingPart = false;
            grabbedPart = null;
        }
    }

    void TransitionToDragging()
    {
        currentState = SilencerState.Dragging;

        // Show the new silencer
        SetRendererActive(true);
        SetColor(normalColor);

        // Zoom out a bit so player can see both the barrel and the silencer
        if (mainCamera != null)
        {
            targetCameraPosition = originalCameraPosition;
            isZoomingOut = true;
            Debug.Log("Transitioning to dragging phase");
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
        currentState = SilencerState.Screwing;

        // Calculate the start position (offset away from socket)
        Vector3 offsetDirection = targetSocket.TransformDirection(screwInStartDirection);
        screwInStartPosition = targetSocket.position + (offsetDirection * screwInStartDistance);
        screwInTargetPosition = targetSocket.position;

        // Position silencer at the start position
        transform.position = screwInStartPosition;
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

        Debug.Log($"Silencer snapped! Starting at {screwInStartPosition}, will screw in to {screwInTargetPosition}. Drag DOWN to screw it in.");
    }

    void HandleScrewing()
    {
        // Start a new pull
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;
            currentPullDistance = 0f;
            Debug.Log("Started new pull");
        }

        // Continue pulling
        if (Input.GetMouseButton(0))
        {
            Vector3 currentMousePos = Input.mousePosition;
            Vector3 mouseDelta = currentMousePos - lastMousePosition;

            // Only count downward movement
            float downwardMovement = -mouseDelta.y / Screen.height * 10f; // Scale factor for sensitivity

            if (downwardMovement > 0)
            {
                // Calculate max distance allowed for this pull
                float maxDistanceThisPull = screwDistance * maxDragPerPull;

                // Check if we've reached the limit for this pull
                if (currentPullDistance < maxDistanceThisPull)
                {
                    // Add to current pull distance, but cap it
                    float distanceToAdd = Mathf.Min(downwardMovement, maxDistanceThisPull - currentPullDistance);
                    currentPullDistance += distanceToAdd;
                    totalDragDistance += distanceToAdd;

                    screwProgress = Mathf.Clamp01(totalDragDistance / screwDistance);

                    // Rotate as we screw in
                    float rotationAmount = distanceToAdd * rotationSpeed;
                    transform.Rotate(Vector3.forward, rotationAmount, Space.Self);

                    // Visual feedback based on progress
                    Color progressColor = Color.Lerp(validColor, Color.cyan, screwProgress);
                    SetColor(progressColor);
                }
                else
                {
                    // Hit the limit for this pull - can't go further until they release and pull again
                    Debug.Log($"Pull limit reached! ({currentPullDistance}/{maxDistanceThisPull}) - Release and pull again");
                }

                // Check if complete
                if (screwProgress >= 1f)
                {
                    Debug.Log("Silencer attached successfully!");
                    CompleteMinigame();
                }
            }

            lastMousePosition = currentMousePos;
        }

        // Released mouse - reset for next pull
        if (Input.GetMouseButtonUp(0))
        {
            if (currentPullDistance > 0)
            {
                Debug.Log($"Pull complete: {currentPullDistance:F2} units. Total progress: {screwProgress * 100f:F0}%");
            }
            currentPullDistance = 0f;
        }
    }

    protected override void CompleteMinigame()
    {
        // Final position and rotation
        transform.position = targetSocket.position;
        transform.rotation = targetSocket.rotation;
        SetColor(Color.green);

        // Disable all weapon parts (old barrel parts) now that silencer is attached
        if (weaponPartsToDisable != null && weaponPartsToDisable.Count > 0)
        {
            Debug.Log($"Attempting to disable {weaponPartsToDisable.Count} weapon part(s)");
            foreach (var part in weaponPartsToDisable)
            {
                if (part != null)
                {
                    Debug.Log($"  Disabling weapon part: {part.name}, currently active: {part.activeSelf}");
                    part.SetActive(false);
                    Debug.Log($"  Weapon part disabled. Now active: {part.activeSelf}");
                }
                else
                {
                    Debug.LogWarning("  Found NULL weapon part in list!");
                }
            }
        }

        // Zoom camera back out
        if (mainCamera != null)
        {
            targetCameraPosition = originalCameraPosition;
            isZoomingOut = true;
            Debug.Log($"Camera zooming back to {originalCameraPosition}");
        }

        StartCoroutine(CompleteAfterZoomOut());
    }

    protected override void CancelMinigame()
    {
        // Reset camera immediately on cancel
        if (mainCamera != null)
        {
            mainCamera.transform.position = originalCameraPosition;
            isZooming = false;
            isZoomingOut = false;
            Debug.Log("Camera reset on cancel");
        }

        // Reset weapon parts to original positions if minigame was cancelled
        if (weaponPartsToDisable != null && weaponPartsToDisable.Count > 0)
        {
            Debug.Log($"Resetting {weaponPartsToDisable.Count} weapon part(s) after cancel");
            for (int i = 0; i < weaponPartsToDisable.Count; i++)
            {
                if (weaponPartsToDisable[i] != null && i < partOriginalPositions.Count)
                {
                    weaponPartsToDisable[i].transform.localPosition = partOriginalPositions[i];

                    // Reset color
                    Renderer[] renderers = weaponPartsToDisable[i].GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        if (rend.material != null)
                        {
                            rend.material.color = Color.white;
                        }
                    }

                    Debug.Log($"  Reset weapon part: {weaponPartsToDisable[i].name} to original position");
                }
            }
        }

        base.CancelMinigame();
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

            // Draw minimum distance to move away
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetSocket.position, minDistanceToMoveAway);

            // Draw screw-in start position
            Gizmos.color = Color.magenta;
            Vector3 startOffset = targetSocket.TransformDirection(screwInStartDirection) * screwInStartDistance;
            Gizmos.DrawLine(targetSocket.position, targetSocket.position + startOffset);
            Gizmos.DrawWireSphere(targetSocket.position + startOffset, 0.02f);
        }

        // Draw unscrew direction and distance
        if (weaponPartsToDisable != null && weaponPartsToDisable.Count > 0)
        {
            foreach (var part in weaponPartsToDisable)
            {
                if (part != null)
                {
                    Gizmos.color = Color.red;
                    Vector3 moveOffset = part.transform.TransformDirection(unscrewMoveDirection) * unscrewMoveDistance;
                    Gizmos.DrawLine(part.transform.position, part.transform.position + moveOffset);
                    Gizmos.DrawSphere(part.transform.position + moveOffset, 0.01f);
                }
            }
        }
    }
}