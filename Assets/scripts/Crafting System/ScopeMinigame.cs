using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Minigame for attaching scopes - drag to sight socket, then screw in front and back screws
/// Now supports removing old scope first if one exists
/// </summary>
public class ScopeMinigame : AttachmentMinigameBase
{
    [Header("Scope Specific Settings")]
    [SerializeField] private float snapDistance = 0.3f;
    [SerializeField] private float rotationsRequired = 2f;
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField] private float circleDetectionRadius = 50f;

    [Header("Spawn Settings")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 0.1f, 0f);

    [Header("Screw Prefab Settings")]
    [SerializeField] private GameObject screwPrefab;
    [SerializeField] private float screwMoveInDistance = 0.05f;
    [SerializeField] private float screwMoveSpeed = 2f;

    [Header("Screw Positions (fallback if no AttachmentData)")]
    [SerializeField] private Vector3 frontScrewLocalPos = new Vector3(-0.05f, 0.05f, -0.05f);
    [SerializeField] private Vector3 backScrewLocalPos = new Vector3(0.05f, 0.05f, -0.05f);
    [SerializeField] private float screwRadius = 0.015f;

    [Header("Camera Zoom Settings")]
    [SerializeField] private float zoomAmount = 0.7f;
    [SerializeField] private float zoomSpeed = 2f;

    [Header("Old Scope Removal")]
    [SerializeField] private float minDistanceToMoveAway = 0.15f;

    private Camera mainCamera;
    private Vector3 dragStartMousePos;
    private Vector3 objectStartPos;
    private bool isSnapped = false;
    private Vector3 lastMousePosition;

    private enum ScrewState { None, FrontScrew, BackScrew, Complete }
    private ScrewState currentState = ScrewState.None;

    private enum ScopeState { UnscrewingOldFront, UnscrewingOldBack, MovingOldAway, Dragging, Screwing, Complete }
    private ScopeState replacementState = ScopeState.Dragging;

    private float frontScrewProgress = 0f;
    private float backScrewProgress = 0f;
    private float oldFrontScrewProgress = 0f;
    private float oldBackScrewProgress = 0f;

    private Vector2 screwCenterScreenPos;
    private float totalRotation = 0f;
    private Vector2 lastMouseAngle;
    private bool isRotating = false;

    private GameObject frontScrewVisual;
    private GameObject backScrewVisual;
    private GameObject oldFrontScrewVisual;
    private GameObject oldBackScrewVisual;

    private Vector3 frontScrewStartPos;
    private Vector3 backScrewStartPos;
    private Vector3 frontScrewTargetPos;
    private Vector3 backScrewTargetPos;

    private Vector3 actualFrontScrewPos;
    private Vector3 actualBackScrewPos;
    private float actualScrewRadius;

    private Vector3 originalCameraPosition;
    private bool isZooming = false;
    private bool isZoomingOut = false;
    private Vector3 targetCameraPosition;

    private List<GameObject> weaponPartsToDisable = new List<GameObject>();

    // Old scope parts
    private List<GameObject> oldScopeParts = new List<GameObject>();
    private List<Vector3> partOriginalPositions = new List<Vector3>();
    private GameObject grabbedPart;
    private Vector3 grabbedPartDragStart;
    private Vector3 grabbedPartStartPos;
    private bool isDraggingPart = false;
    private Vector3 socketWorldPosition;
    private AttachmentData oldAttachmentData;

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
            Debug.LogError("ScopeMinigame: No camera found!");
            return;
        }

        originalCameraPosition = mainCamera.transform.position;

        if (targetSocket != null)
        {
            socketWorldPosition = targetSocket.position;
            Vector3 worldOffset = targetSocket.TransformDirection(spawnOffset);
            transform.position = targetSocket.position + worldOffset;
            transform.rotation = targetSocket.rotation;
        }

        // Check if we need to remove old scope first
        if (oldScopeParts != null && oldScopeParts.Count > 0)
        {
            replacementState = ScopeState.UnscrewingOldFront;

            // Hide new scope
            SetRendererActive(false);

            // Add colliders to old parts
            AddCollidersToOldParts();

            // Create screws on old scope
            CreateOldScrewVisuals();

            // Zoom to socket
            ZoomToSocket();

            Debug.Log("Unscrew old scope - FRONT screw first (clockwise circles)");
        }
        else
        {
            // No old scope, start normal minigame
            replacementState = ScopeState.Dragging;
            CreateScrewVisuals();
            SetColor(normalColor);
            Debug.Log("Drag scope to socket");
        }
    }

    /// <summary>
    /// Set the old scope parts to unscrew and remove
    /// </summary>
    public void SetOldScopeParts(Transform weaponTransform, List<string> partPaths, AttachmentData oldAttachment = null)
    {
        oldScopeParts.Clear();
        partOriginalPositions.Clear();
        oldAttachmentData = oldAttachment;

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
                oldScopeParts.Add(partTransform.gameObject);
                partOriginalPositions.Add(partTransform.localPosition);
                Debug.Log($"Will unscrew: {partTransform.name}");
            }
        }
    }

    public void SetWeaponPartsToDisable(Transform weaponTransform, List<string> partPaths)
    {
        Debug.Log($"SetWeaponPartsToDisable called with weaponTransform: {(weaponTransform != null ? weaponTransform.name : "NULL")}, partPaths count: {(partPaths != null ? partPaths.Count : 0)}");

        weaponPartsToDisable.Clear();

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
                Debug.Log($"SUCCESS: Scope will disable '{partTransform.gameObject.name}' at path: {GetFullPath(partTransform)}");
            }
            else
            {
                Debug.LogError($"FAILED: Could not find weapon part to disable: '{partPath}'. Check the name and hierarchy.");
            }
        }

        Debug.Log($"Total weapon parts to disable: {weaponPartsToDisable.Count}");
    }

    public void SetWeaponPartToDisable(Transform weaponTransform, string partPath)
    {
        Debug.Log($"SetWeaponPartToDisable (legacy) called with weaponTransform: {(weaponTransform != null ? weaponTransform.name : "NULL")}, partPath: '{partPath}'");

        if (string.IsNullOrEmpty(partPath))
        {
            Debug.LogWarning("Part path is empty");
            return;
        }

        var paths = new List<string> { partPath };
        SetWeaponPartsToDisable(weaponTransform, paths);
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

    private void AddCollidersToOldParts()
    {
        foreach (var part in oldScopeParts)
        {
            if (part != null && part.GetComponent<Collider>() == null)
                part.AddComponent<BoxCollider>();
        }
    }

    private void CreateOldScrewVisuals()
    {
        if (oldScopeParts.Count == 0) return;

        GameObject oldScope = oldScopeParts[0];

        // Use OLD attachment data for screw positions
        Vector3 oldFrontScrewPos = oldAttachmentData != null ? oldAttachmentData.frontScrewLocalPos : frontScrewLocalPos;
        Vector3 oldBackScrewPos = oldAttachmentData != null ? oldAttachmentData.backScrewLocalPos : backScrewLocalPos;
        float oldScrewRadius = oldAttachmentData != null ? oldAttachmentData.screwRadius : screwRadius;

        Debug.Log($"Creating OLD screws: Front={oldFrontScrewPos}, Back={oldBackScrewPos}");

        if (screwPrefab != null)
            oldFrontScrewVisual = Instantiate(screwPrefab, oldScope.transform);
        else
            oldFrontScrewVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        oldFrontScrewVisual.transform.SetParent(oldScope.transform);
        // Start screws IN position
        oldFrontScrewVisual.transform.localPosition = oldFrontScrewPos + new Vector3(screwMoveInDistance, 0, 0);
        oldFrontScrewVisual.transform.localRotation = Quaternion.Euler(90, 0, 90);
        if (!screwPrefab) oldFrontScrewVisual.transform.localScale = new Vector3(oldScrewRadius * 2, 0.01f, oldScrewRadius * 2);

        Collider col = oldFrontScrewVisual.GetComponent<Collider>();
        if (col) Destroy(col);

        if (screwPrefab != null)
            oldBackScrewVisual = Instantiate(screwPrefab, oldScope.transform);
        else
            oldBackScrewVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        oldBackScrewVisual.transform.SetParent(oldScope.transform);
        // Start screws IN position
        oldBackScrewVisual.transform.localPosition = oldBackScrewPos + new Vector3(screwMoveInDistance, 0, 0);
        oldBackScrewVisual.transform.localRotation = Quaternion.Euler(90, 0, 90);
        if (!screwPrefab) oldBackScrewVisual.transform.localScale = new Vector3(oldScrewRadius * 2, 0.01f, oldScrewRadius * 2);

        col = oldBackScrewVisual.GetComponent<Collider>();
        if (col) Destroy(col);

        SetScrewColor(oldFrontScrewVisual, Color.red);
        SetScrewColor(oldBackScrewVisual, Color.red);
    }

    void CreateScrewVisuals()
    {
        bool usingAttachmentData = attachmentData != null;
        actualFrontScrewPos = attachmentData != null ? attachmentData.frontScrewLocalPos : frontScrewLocalPos;
        actualBackScrewPos = attachmentData != null ? attachmentData.backScrewLocalPos : backScrewLocalPos;
        actualScrewRadius = attachmentData != null ? attachmentData.screwRadius : screwRadius;

        Debug.Log($"Using AttachmentData: {usingAttachmentData}");
        Debug.Log($"Creating screws at: Front={actualFrontScrewPos}, Back={actualBackScrewPos}, Radius={actualScrewRadius}");

        if (screwPrefab != null)
        {
            frontScrewVisual = Instantiate(screwPrefab, transform);
            frontScrewVisual.name = "FrontScrew";
        }
        else
        {
            frontScrewVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            frontScrewVisual.transform.localScale = new Vector3(actualScrewRadius * 2, 0.01f, actualScrewRadius * 2);
            Debug.LogWarning("No screw prefab assigned! Using primitive cylinder as fallback.");
        }

        frontScrewVisual.transform.SetParent(transform);
        frontScrewVisual.transform.localPosition = actualFrontScrewPos;
        frontScrewVisual.transform.localRotation = Quaternion.Euler(90, 0, 90);

        frontScrewStartPos = frontScrewVisual.transform.localPosition;
        frontScrewTargetPos = frontScrewStartPos + new Vector3(screwMoveInDistance, 0, 0);

        Collider frontCol = frontScrewVisual.GetComponent<Collider>();
        if (frontCol != null) Destroy(frontCol);

        if (screwPrefab != null)
        {
            backScrewVisual = Instantiate(screwPrefab, transform);
            backScrewVisual.name = "BackScrew";
        }
        else
        {
            backScrewVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            backScrewVisual.transform.localScale = new Vector3(actualScrewRadius * 2, 0.01f, actualScrewRadius * 2);
        }

        backScrewVisual.transform.SetParent(transform);
        backScrewVisual.transform.localPosition = actualBackScrewPos;
        backScrewVisual.transform.localRotation = Quaternion.Euler(90, 0, 90);

        backScrewStartPos = backScrewVisual.transform.localPosition;
        backScrewTargetPos = backScrewStartPos + new Vector3(screwMoveInDistance, 0, 0);

        Collider backCol = backScrewVisual.GetComponent<Collider>();
        if (backCol != null) Destroy(backCol);

        SetScrewColor(frontScrewVisual, Color.red);
        SetScrewColor(backScrewVisual, Color.red);
    }

    void SetScrewColor(GameObject screw, Color color)
    {
        if (screw == null) return;

        Renderer rend = screw.GetComponent<Renderer>();
        if (rend == null)
            rend = screw.GetComponentInChildren<Renderer>();

        if (rend != null && rend.material != null)
        {
            rend.material.color = color;
        }
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

        // Animate OLD screws moving OUT
        if (replacementState == ScopeState.UnscrewingOldFront || replacementState == ScopeState.UnscrewingOldBack)
        {
            if (oldScopeParts.Count > 0)
            {
                Vector3 oldFrontScrewPos = oldAttachmentData != null ? oldAttachmentData.frontScrewLocalPos : frontScrewLocalPos;
                Vector3 oldBackScrewPos = oldAttachmentData != null ? oldAttachmentData.backScrewLocalPos : backScrewLocalPos;

                if (oldFrontScrewVisual != null && oldFrontScrewProgress > 0f)
                {
                    Vector3 startPos = oldFrontScrewPos + new Vector3(screwMoveInDistance, 0, 0);
                    Vector3 targetPos = oldFrontScrewPos;
                    oldFrontScrewVisual.transform.localPosition = Vector3.Lerp(
                        startPos,
                        targetPos,
                        oldFrontScrewProgress
                    );
                }

                if (oldBackScrewVisual != null && oldBackScrewProgress > 0f)
                {
                    Vector3 startPos = oldBackScrewPos + new Vector3(screwMoveInDistance, 0, 0);
                    Vector3 targetPos = oldBackScrewPos;
                    oldBackScrewVisual.transform.localPosition = Vector3.Lerp(
                        startPos,
                        targetPos,
                        oldBackScrewProgress
                    );
                }
            }
        }

        // Animate NEW screws moving IN
        if (frontScrewVisual != null && frontScrewProgress > 0f)
        {
            Vector3 targetPos = Vector3.Lerp(frontScrewStartPos, frontScrewTargetPos, frontScrewProgress);
            frontScrewVisual.transform.localPosition = Vector3.Lerp(
                frontScrewVisual.transform.localPosition,
                targetPos,
                Time.deltaTime * screwMoveSpeed
            );
        }

        if (backScrewVisual != null && backScrewProgress > 0f)
        {
            Vector3 targetPos = Vector3.Lerp(backScrewStartPos, backScrewTargetPos, backScrewProgress);
            backScrewVisual.transform.localPosition = Vector3.Lerp(
                backScrewVisual.transform.localPosition,
                targetPos,
                Time.deltaTime * screwMoveSpeed
            );
        }

        // Main state machine
        switch (replacementState)
        {
            case ScopeState.UnscrewingOldFront:
            case ScopeState.UnscrewingOldBack:
                HandleUnscrewingOld();
                break;
            case ScopeState.MovingOldAway:
                HandleMovingOldAway();
                break;
            case ScopeState.Dragging:
                if (!isSnapped)
                    HandleDragging();
                break;
            case ScopeState.Screwing:
                if (isSnapped)
                    HandleScrewing();
                break;
        }
    }

    void HandleUnscrewingOld()
    {
        if (oldScopeParts.Count == 0)
        {
            TransitionToDragging();
            return;
        }

        GameObject oldScope = oldScopeParts[0];
        Vector3 mousePos = Input.mousePosition;

        // Use OLD attachment data for screw positions
        Vector3 oldFrontScrewPos = oldAttachmentData != null ? oldAttachmentData.frontScrewLocalPos : frontScrewLocalPos;
        Vector3 oldBackScrewPos = oldAttachmentData != null ? oldAttachmentData.backScrewLocalPos : backScrewLocalPos;

        Vector3 frontScrewWorldPos = oldScope.transform.TransformPoint(oldFrontScrewPos);
        Vector3 backScrewWorldPos = oldScope.transform.TransformPoint(oldBackScrewPos);

        Vector3 frontScrewScreenPos = mainCamera.WorldToScreenPoint(frontScrewWorldPos);
        Vector3 backScrewScreenPos = mainCamera.WorldToScreenPoint(backScrewWorldPos);

        float distToFront = Vector2.Distance(new Vector2(mousePos.x, mousePos.y), new Vector2(frontScrewScreenPos.x, frontScrewScreenPos.y));
        float distToBack = Vector2.Distance(new Vector2(mousePos.x, mousePos.y), new Vector2(backScrewScreenPos.x, backScrewScreenPos.y));

        if (replacementState == ScopeState.UnscrewingOldFront)
        {
            SetScrewColor(oldFrontScrewVisual, distToFront < circleDetectionRadius ? Color.yellow : Color.red);

            if (Input.GetMouseButtonDown(0) && distToFront < circleDetectionRadius)
            {
                screwCenterScreenPos = new Vector2(frontScrewScreenPos.x, frontScrewScreenPos.y);
                StartCircularMotion(mousePos);
            }
        }
        else if (replacementState == ScopeState.UnscrewingOldBack)
        {
            SetScrewColor(oldBackScrewVisual, distToBack < circleDetectionRadius ? Color.yellow : Color.red);

            if (Input.GetMouseButtonDown(0) && distToBack < circleDetectionRadius)
            {
                screwCenterScreenPos = new Vector2(backScrewScreenPos.x, backScrewScreenPos.y);
                StartCircularMotion(mousePos);
            }
        }

        if (Input.GetMouseButton(0) && isRotating)
        {
            UpdateCircularMotionUnscrew(mousePos);
        }

        if (Input.GetMouseButtonUp(0))
        {
            isRotating = false;
        }
    }

    void UpdateCircularMotionUnscrew(Vector3 mousePos)
    {
        Vector2 currentMouseVec = new Vector2(mousePos.x, mousePos.y) - screwCenterScreenPos;
        Vector2 currentMouseAngle = currentMouseVec.normalized;
        float angleDiff = Vector2.SignedAngle(lastMouseAngle, currentMouseAngle);

        // Unscrewing = CLOCKWISE mouse movement
        if (angleDiff > 0)
        {
            totalRotation += Mathf.Abs(angleDiff);
            float requiredRotation = rotationsRequired * 360f;

            if (replacementState == ScopeState.UnscrewingOldFront)
            {
                oldFrontScrewProgress = Mathf.Clamp01(totalRotation / requiredRotation);

                if (oldFrontScrewVisual != null)
                    oldFrontScrewVisual.transform.Rotate(Vector3.up, -Mathf.Abs(angleDiff), Space.Self);

                SetScrewColor(oldFrontScrewVisual, Color.Lerp(Color.red, Color.green, oldFrontScrewProgress));

                if (oldFrontScrewProgress >= 1f)
                {
                    Debug.Log("Old front screw done! Now back screw");
                    replacementState = ScopeState.UnscrewingOldBack;
                    isRotating = false;
                }
            }
            else if (replacementState == ScopeState.UnscrewingOldBack)
            {
                oldBackScrewProgress = Mathf.Clamp01(totalRotation / requiredRotation);

                if (oldBackScrewVisual != null)
                    oldBackScrewVisual.transform.Rotate(Vector3.up, -Mathf.Abs(angleDiff), Space.Self);

                SetScrewColor(oldBackScrewVisual, Color.Lerp(Color.red, Color.green, oldBackScrewProgress));

                if (oldBackScrewProgress >= 1f)
                {
                    Debug.Log("Old back screw done! Move it away");
                    replacementState = ScopeState.MovingOldAway;
                    isRotating = false;

                    if (oldFrontScrewVisual) Destroy(oldFrontScrewVisual);
                    if (oldBackScrewVisual) Destroy(oldBackScrewVisual);
                }
            }
        }

        lastMouseAngle = currentMouseAngle;
    }

    void HandleMovingOldAway()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);

            foreach (var hit in hits)
            {
                foreach (var part in oldScopeParts)
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
            Color feedbackColor = dist >= minDistanceToMoveAway ? Color.green : Color.yellow;

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

                if (dist >= minDistanceToMoveAway)
                {
                    Debug.Log("Old scope moved away!");
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
        replacementState = ScopeState.Dragging;

        SetRendererActive(true);
        SetColor(normalColor);
        CreateScrewVisuals();

        if (mainCamera != null)
        {
            targetCameraPosition = originalCameraPosition;
            isZoomingOut = true;
        }

        Debug.Log("Drag NEW scope to socket");
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
        replacementState = ScopeState.Screwing;
        transform.position = targetSocket.position;
        transform.rotation = targetSocket.rotation;
        SetColor(validColor);
        lastMousePosition = Input.mousePosition;
        currentState = ScrewState.None;

        if (mainCamera != null)
        {
            Vector3 directionToScope = (transform.position - mainCamera.transform.position).normalized;
            float distanceToScope = Vector3.Distance(mainCamera.transform.position, transform.position);
            targetCameraPosition = mainCamera.transform.position + (directionToScope * distanceToScope * zoomAmount);
            isZooming = true;
        }

        Debug.Log("Scope snapped! Screw in FRONT screw (counter-clockwise circles)");
    }

    void HandleScrewing()
    {
        Vector3 mousePos = Input.mousePosition;

        Vector3 frontScrewWorldPos = transform.TransformPoint(actualFrontScrewPos);
        Vector3 backScrewWorldPos = transform.TransformPoint(actualBackScrewPos);

        Vector3 frontScrewScreenPos = mainCamera.WorldToScreenPoint(frontScrewWorldPos);
        Vector3 backScrewScreenPos = mainCamera.WorldToScreenPoint(backScrewWorldPos);

        float distToFront = Vector2.Distance(new Vector2(mousePos.x, mousePos.y), new Vector2(frontScrewScreenPos.x, frontScrewScreenPos.y));
        float distToBack = Vector2.Distance(new Vector2(mousePos.x, mousePos.y), new Vector2(backScrewScreenPos.x, backScrewScreenPos.y));

        float screenScrewRadius = circleDetectionRadius;

        if (frontScrewProgress < 1f && distToFront < screenScrewRadius)
            SetScrewColor(frontScrewVisual, Color.yellow);
        else if (frontScrewProgress < 1f)
            SetScrewColor(frontScrewVisual, Color.red);

        if (backScrewProgress < 1f && distToBack < screenScrewRadius)
            SetScrewColor(backScrewVisual, Color.yellow);
        else if (backScrewProgress < 1f)
            SetScrewColor(backScrewVisual, Color.red);

        if (Input.GetMouseButtonDown(0))
        {
            if (frontScrewProgress < 1f && distToFront < screenScrewRadius)
            {
                currentState = ScrewState.FrontScrew;
                screwCenterScreenPos = new Vector2(frontScrewScreenPos.x, frontScrewScreenPos.y);
                StartCircularMotion(mousePos);
            }
            else if (backScrewProgress < 1f && distToBack < screenScrewRadius)
            {
                currentState = ScrewState.BackScrew;
                screwCenterScreenPos = new Vector2(backScrewScreenPos.x, backScrewScreenPos.y);
                StartCircularMotion(mousePos);
            }
        }

        if (Input.GetMouseButton(0) && isRotating && currentState != ScrewState.None && currentState != ScrewState.Complete)
        {
            UpdateCircularMotion(mousePos);
        }

        if (Input.GetMouseButtonUp(0))
        {
            isRotating = false;
        }
    }

    void StartCircularMotion(Vector3 mousePos)
    {
        isRotating = true;
        totalRotation = 0f;

        Vector2 mouseVec = new Vector2(mousePos.x, mousePos.y) - screwCenterScreenPos;
        lastMouseAngle = mouseVec.normalized;
    }

    void UpdateCircularMotion(Vector3 mousePos)
    {
        Vector2 currentMouseVec = new Vector2(mousePos.x, mousePos.y) - screwCenterScreenPos;
        Vector2 currentMouseAngle = currentMouseVec.normalized;
        float angleDiff = Vector2.SignedAngle(lastMouseAngle, currentMouseAngle);

        // Screwing IN = counter-clockwise
        if (angleDiff < 0)
        {
            totalRotation += Mathf.Abs(angleDiff);

            if (currentState == ScrewState.FrontScrew)
            {
                float requiredRotation = rotationsRequired * 360f;
                frontScrewProgress = Mathf.Clamp01(totalRotation / requiredRotation);

                if (frontScrewVisual != null)
                {
                    frontScrewVisual.transform.Rotate(Vector3.up, Mathf.Abs(angleDiff), Space.Self);
                }

                Color progressColor = Color.Lerp(Color.red, Color.green, frontScrewProgress);
                SetScrewColor(frontScrewVisual, progressColor);

                if (frontScrewProgress >= 1f)
                {
                    Debug.Log($"Front screw complete! Checking if back screw is also done: backScrewProgress={backScrewProgress}");
                    currentState = ScrewState.None;
                    isRotating = false;

                    if (backScrewProgress >= 1f && !isComplete)
                    {
                        Debug.Log("Both screws complete!");
                        currentState = ScrewState.Complete;
                        CompleteMinigame();
                    }
                }
            }
            else if (currentState == ScrewState.BackScrew)
            {
                float requiredRotation = rotationsRequired * 360f;
                backScrewProgress = Mathf.Clamp01(totalRotation / requiredRotation);

                if (backScrewVisual != null)
                {
                    backScrewVisual.transform.Rotate(Vector3.up, Mathf.Abs(angleDiff), Space.Self);
                }

                Color progressColor = Color.Lerp(Color.red, Color.green, backScrewProgress);
                SetScrewColor(backScrewVisual, progressColor);

                if (backScrewProgress >= 1f)
                {
                    Debug.Log($"Back screw complete! Checking if front screw is also done: frontScrewProgress={frontScrewProgress}");
                    currentState = ScrewState.None;
                    isRotating = false;

                    if (frontScrewProgress >= 1f && !isComplete)
                    {
                        Debug.Log("Both screws complete!");
                        currentState = ScrewState.Complete;
                        CompleteMinigame();
                    }
                }
            }
        }

        lastMouseAngle = currentMouseAngle;
    }

    protected override void CompleteMinigame()
    {
        Debug.Log("=== CompleteMinigame() called ===");
        transform.position = targetSocket.position;
        transform.rotation = targetSocket.rotation;
        SetColor(Color.green);
        SetScrewColor(frontScrewVisual, Color.green);
        SetScrewColor(backScrewVisual, Color.green);

        // Disable weapon parts (iron sights)
        if (weaponPartsToDisable != null && weaponPartsToDisable.Count > 0)
        {
            Debug.Log($"Attempting to disable {weaponPartsToDisable.Count} weapon part(s)");
            foreach (var part in weaponPartsToDisable)
            {
                if (part != null)
                {
                    Debug.Log($"  Disabling weapon part: {part.name}");
                    part.SetActive(false);
                }
            }
        }

        // Disable old scope parts
        if (oldScopeParts != null && oldScopeParts.Count > 0)
        {
            foreach (var part in oldScopeParts)
            {
                if (part != null)
                {
                    Debug.Log($"Disabling old scope: {part.name}");
                    part.SetActive(false);
                }
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

        if (weaponPartsToDisable != null && weaponPartsToDisable.Count > 0)
        {
            foreach (var part in weaponPartsToDisable)
            {
                if (part != null && !part.activeSelf)
                    part.SetActive(true);
            }
        }

        DestroyScrews();
        base.CancelMinigame();
    }

    private System.Collections.IEnumerator CompleteAfterZoomOut()
    {
        while (isZoomingOut)
        {
            yield return null;
        }

        DestroyScrews();
        base.CompleteMinigame();
    }

    private void DestroyScrews()
    {
        if (frontScrewVisual != null) Destroy(frontScrewVisual);
        if (backScrewVisual != null) Destroy(backScrewVisual);
        if (oldFrontScrewVisual != null) Destroy(oldFrontScrewVisual);
        if (oldBackScrewVisual != null) Destroy(oldBackScrewVisual);
    }

    void OnDrawGizmos()
    {
        if (targetSocket != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetSocket.position, snapDistance);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetSocket.position, minDistanceToMoveAway);
        }

        if (isSnapped)
        {
            Gizmos.color = Color.red;
            Vector3 frontPos = transform.TransformPoint(actualFrontScrewPos);
            Vector3 backPos = transform.TransformPoint(actualBackScrewPos);
            Gizmos.DrawWireSphere(frontPos, actualScrewRadius);
            Gizmos.DrawWireSphere(backPos, actualScrewRadius);
        }
    }
}