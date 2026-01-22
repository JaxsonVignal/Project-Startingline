using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Minigame for attaching siderail attachments - drag to socket, then slide into place
/// </summary>
public class SiderailMinigame : AttachmentMinigameBase
{
    [Header("Siderail Specific Settings")]
    [SerializeField] private float snapDistance = 0.3f;
    [SerializeField] private float slideDistance = 0.1f; // How far to slide the attachment
    [SerializeField] private float slideSpeed = 2f;
    [SerializeField] private Vector3 slideDirection = Vector3.right; // Default slide direction (local space)

    [Header("Spawn Settings")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 0, -0.2f); // Spawn to the left (negative Z in local space)

    [Header("Camera Zoom Settings")]
    [SerializeField] private float zoomAmount = 0.7f;
    [SerializeField] private float zoomSpeed = 2f;

    [Header("Old Siderail Removal")]
    [SerializeField] private float minDistanceToSlideAway = 0.15f;

    private Camera mainCamera;
    private Vector3 dragStartMousePos;
    private Vector3 objectStartPos;
    private bool isSnapped = false;
    private bool isSliding = false;
    private float slideProgress = 0f;

    private enum SiderailState { SlidingOldOff, MovingOldAway, Dragging, Sliding, Complete }
    private SiderailState replacementState = SiderailState.Dragging;

    private Vector3 originalCameraPosition;
    private bool isZooming = false;
    private bool isZoomingOut = false;
    private Vector3 targetCameraPosition;

    // Old siderail parts
    private List<GameObject> oldSiderailParts = new List<GameObject>();
    private List<Vector3> partOriginalPositions = new List<Vector3>();
    private GameObject grabbedPart;
    private Vector3 grabbedPartDragStart;
    private Vector3 grabbedPartStartPos;
    private bool isDraggingPart = false;
    private Vector3 socketWorldPosition;

    // Slide positions
    private Vector3 slideStartPosition;
    private Vector3 slideTargetPosition;
    private Vector3 oldPartSlideStartPos;
    private Vector3 oldPartSlideTargetPos;

    protected override void Awake()
    {
        base.Awake();

        Collider col = GetComponent<Collider>();
        if (col == null) col = GetComponentInChildren<Collider>();

        if (col == null)
        {
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
            Debug.LogError("SiderailMinigame: No camera found!");
            return;
        }

        originalCameraPosition = mainCamera.transform.position;

        if (targetSocket != null)
        {
            socketWorldPosition = targetSocket.position;
            Vector3 worldOffset = targetSocket.TransformDirection(spawnOffset);
            transform.position = targetSocket.position + worldOffset;

            // Apply socket rotation with 180 degree Y-axis offset for correct orientation
            transform.rotation = targetSocket.rotation * Quaternion.Euler(180, 180, 0);
        }

        // Check if we need to remove old parts first
        if (oldSiderailParts != null && oldSiderailParts.Count > 0)
        {
            replacementState = SiderailState.SlidingOldOff;

            // Hide new siderail
            SetRendererActive(false);

            // Add colliders to old parts
            AddCollidersToOldParts();

            // Setup old part slide positions
            SetupOldPartSlidePositions();

            // Zoom to socket
            ZoomToSocket();

            Debug.Log("Slide old siderail off (drag to slide)");
        }
        else
        {
            // No old parts, start normal minigame
            replacementState = SiderailState.Dragging;
            SetColor(normalColor);
            Debug.Log("Drag siderail to socket");
        }
    }

    /// <summary>
    /// Set the old siderail parts that need to be removed
    /// </summary>
    public void SetOldSiderailParts(Transform weaponTransform, List<string> partPaths)
    {
        oldSiderailParts.Clear();
        partOriginalPositions.Clear();

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
                oldSiderailParts.Add(partTransform.gameObject);
                partOriginalPositions.Add(partTransform.localPosition);
                Debug.Log($"Will slide off: {partTransform.name}");
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

    private void AddCollidersToOldParts()
    {
        foreach (var part in oldSiderailParts)
        {
            if (part != null && part.GetComponent<Collider>() == null)
                part.AddComponent<BoxCollider>();
        }
    }

    private void SetupOldPartSlidePositions()
    {
        if (oldSiderailParts.Count == 0) return;

        GameObject oldSiderail = oldSiderailParts[0];

        // Calculate slide direction in world space
        // Old part should slide BACKWARDS (from right to left, negative Z)
        Vector3 worldSlideDirection = oldSiderail.transform.TransformDirection(-Vector3.forward);

        oldPartSlideStartPos = oldSiderail.transform.position;
        oldPartSlideTargetPos = oldPartSlideStartPos + (worldSlideDirection * slideDistance);
    }

    void SetRendererActive(bool active)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            rend.enabled = active;
        }
    }

    void ZoomToSocket()
    {
        if (mainCamera == null || targetSocket == null) return;

        Vector3 directionToSocket = (targetSocket.position - mainCamera.transform.position).normalized;
        float distanceToSocket = Vector3.Distance(mainCamera.transform.position, targetSocket.position);
        targetCameraPosition = mainCamera.transform.position + (directionToSocket * distanceToSocket * zoomAmount);
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

        // Animate old part sliding
        if (replacementState == SiderailState.SlidingOldOff && isSliding && oldSiderailParts.Count > 0)
        {
            GameObject oldSiderail = oldSiderailParts[0];
            oldSiderail.transform.position = Vector3.Lerp(
                oldSiderail.transform.position,
                oldPartSlideTargetPos,
                Time.deltaTime * slideSpeed
            );

            // Visual feedback
            Color slideColor = Color.Lerp(Color.yellow, Color.green, slideProgress);
            Renderer[] renderers = oldSiderail.GetComponentsInChildren<Renderer>();
            foreach (var rend in renderers)
            {
                if (rend.material != null)
                    rend.material.color = slideColor;
            }

            // Check if slide is complete
            if (Vector3.Distance(oldSiderail.transform.position, oldPartSlideTargetPos) < 0.01f)
            {
                isSliding = false;
                Debug.Log("Old siderail slid off! Move it away");
                replacementState = SiderailState.MovingOldAway;
            }
        }

        // Animate new attachment sliding in
        if (replacementState == SiderailState.Sliding && isSliding)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                slideTargetPosition,
                Time.deltaTime * slideSpeed
            );

            slideProgress = 1f - (Vector3.Distance(transform.position, slideTargetPosition) / slideDistance);
            Color slideColor = Color.Lerp(Color.yellow, Color.green, slideProgress);
            SetColor(slideColor);

            if (Vector3.Distance(transform.position, slideTargetPosition) < 0.01f)
            {
                transform.position = slideTargetPosition;
                isSliding = false;
                CompleteMinigame();
            }
        }

        // Main state machine
        switch (replacementState)
        {
            case SiderailState.SlidingOldOff:
                HandleSlidingOldOff();
                break;
            case SiderailState.MovingOldAway:
                HandleMovingOldAway();
                break;
            case SiderailState.Dragging:
                if (!isSnapped)
                    HandleDragging();
                break;
            case SiderailState.Sliding:
                if (isSnapped && !isSliding)
                    HandleSliding();
                break;
        }
    }

    void HandleSlidingOldOff()
    {
        if (oldSiderailParts.Count == 0)
        {
            TransitionToDragging();
            return;
        }

        GameObject oldSiderail = oldSiderailParts[0];

        if (Input.GetMouseButtonDown(0) && !isSliding)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == oldSiderail || hit.collider.transform.IsChildOf(oldSiderail.transform))
                {
                    Debug.Log("Starting slide off animation");
                    isSliding = true;
                    slideProgress = 0f;
                    break;
                }
            }
        }
    }

    void HandleMovingOldAway()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);

            foreach (var hit in hits)
            {
                foreach (var part in oldSiderailParts)
                {
                    if (part != null && (hit.collider.gameObject == part || hit.collider.transform.IsChildOf(part.transform)))
                    {
                        grabbedPart = part;
                        isDraggingPart = true;
                        grabbedPartDragStart = Input.mousePosition;
                        grabbedPartStartPos = grabbedPart.transform.position;
                        break;
                    }
                }
                if (grabbedPart != null) break;
            }
        }

        if (isDraggingPart && Input.GetMouseButton(0) && grabbedPart != null)
        {
            Vector3 currentMousePos = Input.mousePosition;
            Vector3 mouseDelta = currentMousePos - grabbedPartDragStart;

            Vector3 worldDelta = mainCamera.ScreenToWorldPoint(new Vector3(mouseDelta.x, mouseDelta.y, mainCamera.WorldToScreenPoint(grabbedPartStartPos).z))
                                - mainCamera.ScreenToWorldPoint(new Vector3(0, 0, mainCamera.WorldToScreenPoint(grabbedPartStartPos).z));

            grabbedPart.transform.position = grabbedPartStartPos + worldDelta;

            float dist = Vector3.Distance(grabbedPart.transform.position, socketWorldPosition);
            Color feedbackColor = dist >= minDistanceToSlideAway ? Color.green : Color.yellow;

            Renderer[] renderers = grabbedPart.GetComponentsInChildren<Renderer>();
            foreach (var rend in renderers)
            {
                if (rend.material != null)
                    rend.material.color = feedbackColor;
            }
        }

        if (Input.GetMouseButtonUp(0) && isDraggingPart)
        {
            if (grabbedPart != null)
            {
                float dist = Vector3.Distance(grabbedPart.transform.position, socketWorldPosition);

                if (dist >= minDistanceToSlideAway)
                {
                    Debug.Log("Old siderail moved away!");
                    TransitionToDragging();
                }
                else
                {
                    Debug.Log("Not far enough!");
                }
            }

            isDraggingPart = false;
            grabbedPart = null;
        }
    }

    void TransitionToDragging()
    {
        replacementState = SiderailState.Dragging;

        SetRendererActive(true);
        SetColor(normalColor);

        if (mainCamera != null)
        {
            targetCameraPosition = originalCameraPosition;
            isZoomingOut = true;
        }

        Debug.Log("Drag NEW siderail to socket");
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
                SetColor(validColor);
            else
                SetColor(normalColor);
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;

            if (IsNearSocket(snapDistance))
                SnapToSocket();
            else
                SetColor(normalColor);
        }
    }

    void SnapToSocket()
    {
        isSnapped = true;
        replacementState = SiderailState.Sliding;

        // Position the attachment at the "pre-slide" position (to the LEFT of socket)
        // The attachment should START to the left and SLIDE right into the socket
        Vector3 worldSlideDirection = transform.TransformDirection(Vector3.forward); // Slide in +Z direction (right on the rail)
        slideStartPosition = targetSocket.position - (worldSlideDirection * slideDistance); // Start BACK (left)
        slideTargetPosition = targetSocket.position; // End at socket (right)

        transform.position = slideStartPosition;
        // Apply socket rotation with 180 degree Y-axis offset for correct orientation
        transform.rotation = targetSocket.rotation * Quaternion.Euler(180, 180, 0);

        SetColor(Color.yellow);

        if (mainCamera != null)
        {
            Vector3 directionToSiderail = (transform.position - mainCamera.transform.position).normalized;
            float distanceToSiderail = Vector3.Distance(mainCamera.transform.position, transform.position);
            targetCameraPosition = mainCamera.transform.position + (directionToSiderail * distanceToSiderail * zoomAmount);
            isZooming = true;
        }

        Debug.Log("Snapped! Click to slide into place");
    }

    void HandleSliding()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                {
                    Debug.Log("Starting slide animation");
                    isSliding = true;
                    slideProgress = 0f;
                    break;
                }
            }
        }
    }

    protected override void CompleteMinigame()
    {
        transform.position = targetSocket.position;
        // Apply socket rotation with 180 degree Y-axis offset for correct orientation
        transform.rotation = targetSocket.rotation * Quaternion.Euler(180, 180, 0);
        SetColor(Color.green);

        // Disable old parts
        if (oldSiderailParts != null && oldSiderailParts.Count > 0)
        {
            foreach (var part in oldSiderailParts)
            {
                if (part != null)
                    part.SetActive(false);
            }
        }

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
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetSocket.position, snapDistance);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetSocket.position, minDistanceToSlideAway);

            // Draw slide direction - attachment slides FROM left TO right (positive Z direction)
            if (isSnapped)
            {
                Gizmos.color = Color.blue;
                Vector3 worldSlideDir = transform.TransformDirection(Vector3.forward);
                Gizmos.DrawRay(slideStartPosition, worldSlideDir * slideDistance);
            }
            else
            {
                // Show where the slide will happen when not yet snapped
                Gizmos.color = Color.gray;
                Vector3 worldSlideDir = targetSocket.TransformDirection(Vector3.forward);
                Vector3 slideStart = targetSocket.position - (worldSlideDir * slideDistance);
                Gizmos.DrawRay(slideStart, worldSlideDir * slideDistance);
            }
        }
    }
}