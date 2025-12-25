using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Minigame for changing magazines - pull old magazine down, then push new magazine up
/// </summary>
public class MagazineMinigame : AttachmentMinigameBase
{
    [Header("Magazine Specific Settings")]
    [SerializeField] private float snapDistance = 0.3f;
    [SerializeField] private float insertDistance = .5f; // Total distance to drag up to insert
    [SerializeField] private float maxDragPerPush = 1f; // Max % of insertDistance per push (0.25 = 25%)
    [SerializeField] private LayerMask raycastLayerMask = -1; // All layers by default

    [Header("Spawn Settings")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, -0.3f, 0f); // Offset from weapon (below)

    [Header("Remove Settings")]
    [SerializeField] private float removeDistance = .5f; // Total distance to drag down to remove
    [SerializeField] private float maxRemovePerPull = 1f; // Max % of removeDistance per pull
    [SerializeField] private float removeMoveDistance = 0.1f; // How far the magazine moves down when removed
    [SerializeField] private Vector3 removeMoveDirection = Vector3.down; // Direction to move the magazine (in local space)
    [SerializeField] private float minDistanceToMoveAway = 0.1f; // Minimum distance magazine must be moved away before proceeding

    [Header("Insert Settings")]
    [SerializeField] private float insertStartDistance = 0.2f; // How far below socket the new magazine starts
    [SerializeField] private Vector3 insertStartDirection = Vector3.down; // Direction to offset the magazine (in local space)

    private Camera mainCamera;
    private Vector3 dragStartMousePos;
    private Vector3 objectStartPos;
    private float insertProgress = 0f; // 0 to 1
    private bool isSnapped = false;
    private Vector3 lastMousePosition;
    private float totalDragDistance = 0f;
    private float currentPushDistance = 0f; // Distance dragged in current push
    private Quaternion startRotation;

    // Old magazine parts
    private List<GameObject> oldMagazineParts = new List<GameObject>();
    private List<Vector3> partOriginalPositions = new List<Vector3>(); // Store original positions
    private List<Vector3> partTargetPositions = new List<Vector3>(); // Store target positions when removed

    // Magazine grabbing
    private GameObject grabbedMagazine;
    private Vector3 grabbedMagazineDragStart;
    private Vector3 grabbedMagazineStartPos;
    private bool isDraggingMagazine = false;
    private Vector3 socketWorldPosition; // Store socket position for distance checking

    // Insert positioning
    private Vector3 insertStartPosition; // Where the new magazine starts when inserting
    private Vector3 insertTargetPosition; // Final socket position

    // Magazine change phase tracking
    private enum MagazineState { RemovingOldMagazine, MovingMagazineAway, Dragging, Inserting, Complete }
    private MagazineState currentState = MagazineState.RemovingOldMagazine;
    private float removeProgress = 0f;
    private float totalRemoveDistance = 0f;
    private float currentRemovePullDistance = 0f;

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
            Debug.LogWarning("MagazineMinigame: No collider found! Adding BoxCollider...");
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
            Debug.Log($"MagazineMinigame: Collider found - {col.GetType().Name}");
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
            Debug.LogError("MagazineMinigame: No camera found! Make sure preview camera is assigned or main camera exists.");
            return;
        }

        Debug.Log($"MagazineMinigame: Using camera: {mainCamera.name}");

        // Position the new magazine below the weapon (hidden initially if we need to remove old one first)
        if (targetSocket != null)
        {
            socketWorldPosition = targetSocket.position;
            transform.position = targetSocket.position + spawnOffset;
            transform.rotation = targetSocket.rotation;
            startRotation = transform.rotation;
        }

        // Check if there are old magazines to remove first
        if (oldMagazineParts != null && oldMagazineParts.Count > 0)
        {
            currentState = MagazineState.RemovingOldMagazine;

            // Hide the new magazine while removing old one
            SetRendererActive(false);

            // Calculate target positions for removing (move magazine down)
            CalculateRemoveTargetPositions();

            // Add colliders to magazine parts so they can be grabbed
            AddCollidersToMagazineParts();

            Debug.Log("Magazine minigame started. First, drag DOWN to remove the old magazine.");
        }
        else
        {
            // No old magazine to remove, skip to dragging phase
            currentState = MagazineState.Dragging;
            SetColor(normalColor);
            Debug.Log("Magazine minigame started. Drag the magazine to the magazine well, then drag up to insert it.");
        }
    }

    public void SetOldMagazineParts(Transform weaponTransform, List<string> partPaths)
    {
        Debug.Log($"SetOldMagazineParts called with weaponTransform: {(weaponTransform != null ? weaponTransform.name : "NULL")}, partPaths count: {(partPaths != null ? partPaths.Count : 0)}");

        oldMagazineParts.Clear();
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
                oldMagazineParts.Add(partTransform.gameObject);
                partOriginalPositions.Add(partTransform.localPosition);
                Debug.Log($"SUCCESS: Magazine minigame will remove '{partTransform.gameObject.name}' at path: {GetFullPath(partTransform)}");
            }
            else
            {
                Debug.LogError($"FAILED: Could not find magazine part: '{partPath}'. Check the name and hierarchy.");
            }
        }

        Debug.Log($"Total old magazine parts: {oldMagazineParts.Count}");
    }

    private void AddCollidersToMagazineParts()
    {
        foreach (var part in oldMagazineParts)
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

                    Debug.Log($"Added collider to magazine part: {part.name}");
                }
            }
        }
    }

    private void CalculateRemoveTargetPositions()
    {
        partTargetPositions.Clear();

        for (int i = 0; i < oldMagazineParts.Count; i++)
        {
            if (oldMagazineParts[i] != null)
            {
                // Calculate the target position (move down in local space)
                Vector3 moveOffset = oldMagazineParts[i].transform.TransformDirection(removeMoveDirection) * removeMoveDistance;
                Vector3 targetWorldPos = oldMagazineParts[i].transform.position + moveOffset;

                // Convert back to local position
                Vector3 targetLocalPos;
                if (oldMagazineParts[i].transform.parent != null)
                {
                    targetLocalPos = oldMagazineParts[i].transform.parent.InverseTransformPoint(targetWorldPos);
                }
                else
                {
                    targetLocalPos = targetWorldPos;
                }

                partTargetPositions.Add(targetLocalPos);
                Debug.Log($"Magazine '{oldMagazineParts[i].name}' will move from {partOriginalPositions[i]} to {targetLocalPos}");
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

        // Update magazine positions during removal
        if (currentState == MagazineState.RemovingOldMagazine)
        {
            UpdateMagazinePositions();
        }

        // Update new magazine position during insertion
        if (currentState == MagazineState.Inserting)
        {
            UpdateMagazineInsertPosition();
        }

        switch (currentState)
        {
            case MagazineState.RemovingOldMagazine:
                HandleRemoving();
                break;
            case MagazineState.MovingMagazineAway:
                HandleMovingMagazineAway();
                break;
            case MagazineState.Dragging:
                HandleDragging();
                break;
            case MagazineState.Inserting:
                HandleInserting();
                break;
        }
    }

    void UpdateMagazinePositions()
    {
        for (int i = 0; i < oldMagazineParts.Count; i++)
        {
            if (oldMagazineParts[i] != null && i < partOriginalPositions.Count && i < partTargetPositions.Count)
            {
                // Lerp between original and target position based on remove progress
                Vector3 currentTargetPos = Vector3.Lerp(partOriginalPositions[i], partTargetPositions[i], removeProgress);
                oldMagazineParts[i].transform.localPosition = Vector3.Lerp(
                    oldMagazineParts[i].transform.localPosition,
                    currentTargetPos,
                    Time.deltaTime * 5f // Smooth movement
                );
            }
        }
    }

    void UpdateMagazineInsertPosition()
    {
        // Lerp between start position and target position based on insert progress
        Vector3 currentTargetPos = Vector3.Lerp(insertStartPosition, insertTargetPosition, insertProgress);
        transform.position = Vector3.Lerp(
            transform.position,
            currentTargetPos,
            Time.deltaTime * 5f // Smooth movement
        );
    }

    void HandleRemoving()
    {
        if (oldMagazineParts == null || oldMagazineParts.Count == 0)
        {
            // No magazine to remove, move to dragging
            TransitionToDragging();
            return;
        }

        // Start a new pull
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;
            currentRemovePullDistance = 0f;
            Debug.Log("Started new magazine remove pull");
        }

        // Continue pulling DOWN
        if (Input.GetMouseButton(0))
        {
            Vector3 currentMousePos = Input.mousePosition;
            Vector3 mouseDelta = currentMousePos - lastMousePosition;

            // Only count DOWNWARD movement
            float downwardMovement = -mouseDelta.y / Screen.height * 10f; // Scale factor for sensitivity

            if (downwardMovement > 0)
            {
                // Calculate max distance allowed for this pull
                float maxDistanceThisPull = removeDistance * maxRemovePerPull;

                // Check if we've reached the limit for this pull
                if (currentRemovePullDistance < maxDistanceThisPull)
                {
                    // Add to current pull distance, but cap it
                    float distanceToAdd = Mathf.Min(downwardMovement, maxDistanceThisPull - currentRemovePullDistance);
                    currentRemovePullDistance += distanceToAdd;
                    totalRemoveDistance += distanceToAdd;

                    removeProgress = Mathf.Clamp01(totalRemoveDistance / removeDistance);

                    Debug.Log($"Remove progress: {removeProgress * 100f:F0}%");
                }
                else
                {
                    Debug.Log($"Remove pull limit reached! ({currentRemovePullDistance}/{maxDistanceThisPull}) - Release and pull again");
                }

                // Check if complete
                if (removeProgress >= 1f)
                {
                    Debug.Log("Old magazine removed! Now grab and move the magazine away.");
                    TransitionToMovingMagazineAway();
                }
            }

            lastMousePosition = currentMousePos;
        }

        // Released mouse - reset for next pull
        if (Input.GetMouseButtonUp(0))
        {
            if (currentRemovePullDistance > 0)
            {
                Debug.Log($"Remove pull complete: {currentRemovePullDistance:F2} units. Total progress: {removeProgress * 100f:F0}%");
            }
            currentRemovePullDistance = 0f;
        }
    }

    void TransitionToMovingMagazineAway()
    {
        currentState = MagazineState.MovingMagazineAway;
        Debug.Log("Transition to moving magazine away. Click and drag the magazine to move it out of the way.");
    }

    void HandleMovingMagazineAway()
    {
        // Start dragging magazine
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

            // Check if we hit any magazine parts
            foreach (var hit in hits)
            {
                foreach (var part in oldMagazineParts)
                {
                    if (part != null && (hit.collider.gameObject == part || hit.collider.transform.IsChildOf(part.transform)))
                    {
                        grabbedMagazine = part;
                        isDraggingMagazine = true;
                        grabbedMagazineDragStart = Input.mousePosition;
                        grabbedMagazineStartPos = grabbedMagazine.transform.position;
                        Debug.Log($"Grabbed magazine: {grabbedMagazine.name}");
                        break;
                    }
                }
                if (grabbedMagazine != null) break;
            }
        }

        // Continue dragging magazine
        if (isDraggingMagazine && Input.GetMouseButton(0) && grabbedMagazine != null)
        {
            Vector3 currentMousePos = Input.mousePosition;
            Vector3 mouseDelta = currentMousePos - grabbedMagazineDragStart;

            // Convert screen space delta to world space
            Vector3 worldDelta = mainCamera.ScreenToWorldPoint(new Vector3(mouseDelta.x, mouseDelta.y, mainCamera.WorldToScreenPoint(grabbedMagazineStartPos).z))
                                - mainCamera.ScreenToWorldPoint(new Vector3(0, 0, mainCamera.WorldToScreenPoint(grabbedMagazineStartPos).z));

            grabbedMagazine.transform.position = grabbedMagazineStartPos + worldDelta;

            // Check distance from socket
            float distanceFromSocket = Vector3.Distance(grabbedMagazine.transform.position, socketWorldPosition);

            // Visual feedback
            Renderer[] renderers = grabbedMagazine.GetComponentsInChildren<Renderer>();
            Color feedbackColor = distanceFromSocket >= minDistanceToMoveAway ? Color.green : Color.yellow;
            foreach (var rend in renderers)
            {
                if (rend.material != null)
                {
                    rend.material.color = feedbackColor;
                }
            }
        }

        // Release magazine
        if (Input.GetMouseButtonUp(0) && isDraggingMagazine)
        {
            if (grabbedMagazine != null)
            {
                float distanceFromSocket = Vector3.Distance(grabbedMagazine.transform.position, socketWorldPosition);

                if (distanceFromSocket >= minDistanceToMoveAway)
                {
                    Debug.Log($"Magazine moved far enough away ({distanceFromSocket:F2}m). Proceeding to insert new magazine.");
                    TransitionToDragging();
                }
                else
                {
                    Debug.Log($"Magazine not far enough away ({distanceFromSocket:F2}m < {minDistanceToMoveAway}m). Move it further!");

                    // Reset color
                    Renderer[] renderers = grabbedMagazine.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        if (rend.material != null)
                        {
                            rend.material.color = Color.white;
                        }
                    }
                }
            }

            isDraggingMagazine = false;
            grabbedMagazine = null;
        }
    }

    void TransitionToDragging()
    {
        currentState = MagazineState.Dragging;

        // Show the new magazine
        SetRendererActive(true);
        SetColor(normalColor);

        Debug.Log("Transitioning to dragging phase");
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
                    Debug.Log("Started dragging new magazine!");
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
                Debug.Log("Not close enough to magazine well. Try again.");
            }
        }
    }

    void SnapToSocket()
    {
        isSnapped = true;
        currentState = MagazineState.Inserting;

        // Calculate the start position (offset below socket)
        Vector3 offsetDirection = targetSocket.TransformDirection(insertStartDirection);
        insertStartPosition = targetSocket.position + (offsetDirection * insertStartDistance);
        insertTargetPosition = targetSocket.position;

        // Position magazine at the start position
        transform.position = insertStartPosition;
        transform.rotation = targetSocket.rotation;
        startRotation = transform.rotation;

        SetColor(validColor);
        lastMousePosition = Input.mousePosition;

        Debug.Log($"Magazine snapped! Starting at {insertStartPosition}, will insert to {insertTargetPosition}. Drag UP to insert it.");
    }

    void HandleInserting()
    {
        // Start a new push
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;
            currentPushDistance = 0f;
            Debug.Log("Started new push");
        }

        // Continue pushing UP
        if (Input.GetMouseButton(0))
        {
            Vector3 currentMousePos = Input.mousePosition;
            Vector3 mouseDelta = currentMousePos - lastMousePosition;

            // Only count UPWARD movement
            float upwardMovement = mouseDelta.y / Screen.height * 10f; // Scale factor for sensitivity

            if (upwardMovement > 0)
            {
                // Calculate max distance allowed for this push
                float maxDistanceThisPush = insertDistance * maxDragPerPush;

                // Check if we've reached the limit for this push
                if (currentPushDistance < maxDistanceThisPush)
                {
                    // Add to current push distance, but cap it
                    float distanceToAdd = Mathf.Min(upwardMovement, maxDistanceThisPush - currentPushDistance);
                    currentPushDistance += distanceToAdd;
                    totalDragDistance += distanceToAdd;

                    insertProgress = Mathf.Clamp01(totalDragDistance / insertDistance);

                    // Visual feedback based on progress
                    Color progressColor = Color.Lerp(validColor, Color.cyan, insertProgress);
                    SetColor(progressColor);
                }
                else
                {
                    // Hit the limit for this push - can't go further until they release and push again
                    Debug.Log($"Push limit reached! ({currentPushDistance}/{maxDistanceThisPush}) - Release and push again");
                }

                // Check if complete
                if (insertProgress >= 1f)
                {
                    Debug.Log("Magazine inserted successfully!");
                    CompleteMinigame();
                }
            }

            lastMousePosition = currentMousePos;
        }

        // Released mouse - reset for next push
        if (Input.GetMouseButtonUp(0))
        {
            if (currentPushDistance > 0)
            {
                Debug.Log($"Push complete: {currentPushDistance:F2} units. Total progress: {insertProgress * 100f:F0}%");
            }
            currentPushDistance = 0f;
        }
    }

    protected override void CompleteMinigame()
    {
        // Final position and rotation
        transform.position = targetSocket.position;
        transform.rotation = targetSocket.rotation;
        SetColor(Color.green);

        // Start coroutine to disable parts after completion
        StartCoroutine(CompleteAndDisableParts());
    }

    private System.Collections.IEnumerator CompleteAndDisableParts()
    {
        // Disable old magazine parts first
        if (oldMagazineParts != null && oldMagazineParts.Count > 0)
        {
            Debug.Log($"Attempting to disable {oldMagazineParts.Count} old magazine part(s)");
            foreach (var part in oldMagazineParts)
            {
                if (part != null)
                {
                    Debug.Log($"  Disabling old magazine: {part.name}, currently active: {part.activeSelf}");
                    part.SetActive(false);
                    Debug.Log($"  Old magazine disabled. Now active: {part.activeSelf}");
                }
                else
                {
                    Debug.LogWarning("  Found NULL magazine part in list!");
                }
            }
        }

        // Wait one frame to ensure parts stay disabled
        yield return null;

        // Now complete the minigame
        base.CompleteMinigame();
    }

    protected override void CancelMinigame()
    {
        // Reset old magazine parts to original positions if minigame was cancelled
        if (oldMagazineParts != null && oldMagazineParts.Count > 0)
        {
            Debug.Log($"Resetting {oldMagazineParts.Count} magazine part(s) after cancel");
            for (int i = 0; i < oldMagazineParts.Count; i++)
            {
                if (oldMagazineParts[i] != null && i < partOriginalPositions.Count)
                {
                    oldMagazineParts[i].transform.localPosition = partOriginalPositions[i];

                    // Reset color
                    Renderer[] renderers = oldMagazineParts[i].GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        if (rend.material != null)
                        {
                            rend.material.color = Color.white;
                        }
                    }

                    Debug.Log($"  Reset magazine part: {oldMagazineParts[i].name} to original position");
                }
            }
        }

        base.CancelMinigame();
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

            // Draw insert start position
            Gizmos.color = Color.magenta;
            Vector3 startOffset = targetSocket.TransformDirection(insertStartDirection) * insertStartDistance;
            Gizmos.DrawLine(targetSocket.position, targetSocket.position + startOffset);
            Gizmos.DrawWireSphere(targetSocket.position + startOffset, 0.02f);
        }

        // Draw remove direction and distance
        if (oldMagazineParts != null && oldMagazineParts.Count > 0)
        {
            foreach (var part in oldMagazineParts)
            {
                if (part != null)
                {
                    Gizmos.color = Color.red;
                    Vector3 moveOffset = part.transform.TransformDirection(removeMoveDirection) * removeMoveDistance;
                    Gizmos.DrawLine(part.transform.position, part.transform.position + moveOffset);
                    Gizmos.DrawSphere(part.transform.position + moveOffset, 0.01f);
                }
            }
        }
    }
}