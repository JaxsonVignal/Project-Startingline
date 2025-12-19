using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Minigame for attaching underbarrel attachments - drag to socket, then screw in front and back screws
/// </summary>
public class UnderbarrelMinigame : AttachmentMinigameBase
{
    [Header("Underbarrel Specific Settings")]
    [SerializeField] private float snapDistance = 0.3f;
    [SerializeField] private float rotationsRequired = 2f;
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField] private float circleDetectionRadius = 50f;

    [Header("Spawn Settings")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, -0.1f, 0f);

    [Header("Screw Prefab Settings")]
    [SerializeField] private GameObject screwPrefab; // Assign your screw prefab here
    [SerializeField] private float screwMoveInDistance = 0.05f; // How far screws move in when being screwed
    [SerializeField] private float screwMoveSpeed = 2f; // Speed of screw movement

    [Header("Screw Positions (fallback if no AttachmentData)")]
    [SerializeField] private Vector3 frontScrewLocalPos = new Vector3(-0.05f, 0.02f, 0f);
    [SerializeField] private Vector3 backScrewLocalPos = new Vector3(0.05f, 0.02f, 0f);
    [SerializeField] private float screwRadius = 0.015f;

    [Header("Camera Zoom Settings")]
    [SerializeField] private float zoomAmount = 0.7f;
    [SerializeField] private float zoomSpeed = 2f;

    private Camera mainCamera;
    private Vector3 dragStartMousePos;
    private Vector3 objectStartPos;
    private bool isSnapped = false;
    private Vector3 lastMousePosition;

    private enum ScrewState { None, FrontScrew, BackScrew, Complete }
    private ScrewState currentState = ScrewState.None;
    private float frontScrewProgress = 0f;
    private float backScrewProgress = 0f;

    private Vector2 screwCenterScreenPos;
    private float totalRotation = 0f;
    private Vector2 lastMouseAngle;
    private bool isRotating = false;

    private GameObject frontScrewVisual;
    private GameObject backScrewVisual;
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
            Debug.LogError("UnderbarrelMinigame: No camera found!");
            return;
        }

        originalCameraPosition = mainCamera.transform.position;
        Debug.Log($"UnderbarrelMinigame started. Camera: {mainCamera.name}, Position: {originalCameraPosition}");

        if (targetSocket != null)
        {
            Vector3 worldOffset = targetSocket.TransformDirection(spawnOffset);
            transform.position = targetSocket.position + worldOffset;
            transform.rotation = targetSocket.rotation;
        }

        CreateScrewVisuals();
        SetColor(normalColor);
    }

    void CreateScrewVisuals()
    {
        bool usingAttachmentData = attachmentData != null;
        actualFrontScrewPos = attachmentData != null ? attachmentData.frontScrewLocalPos : frontScrewLocalPos;
        actualBackScrewPos = attachmentData != null ? attachmentData.backScrewLocalPos : backScrewLocalPos;
        actualScrewRadius = attachmentData != null ? attachmentData.screwRadius : screwRadius;

        Debug.Log($"Using AttachmentData: {usingAttachmentData}");
        Debug.Log($"Creating screws at: Front={actualFrontScrewPos}, Back={actualBackScrewPos}, Radius={actualScrewRadius}");

        // Create front screw from prefab
        if (screwPrefab != null)
        {
            frontScrewVisual = Instantiate(screwPrefab, transform);
            frontScrewVisual.name = "FrontScrew";
        }
        else
        {
            // Fallback to primitive if no prefab assigned
            frontScrewVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            frontScrewVisual.transform.localScale = new Vector3(actualScrewRadius * 2, 0.01f, actualScrewRadius * 2);
            Debug.LogWarning("No screw prefab assigned! Using primitive cylinder as fallback.");
        }

        frontScrewVisual.transform.SetParent(transform);
        frontScrewVisual.transform.localPosition = actualFrontScrewPos;
        frontScrewVisual.transform.localRotation = Quaternion.Euler(90, 0, 90);

        // Store start and target positions for movement (move along X-axis)
        frontScrewStartPos = frontScrewVisual.transform.localPosition;
        frontScrewTargetPos = frontScrewStartPos + new Vector3(screwMoveInDistance, 0, 0);

        // Remove collider if it exists (we don't want screws to be physical objects)
        Collider frontCol = frontScrewVisual.GetComponent<Collider>();
        if (frontCol != null) Destroy(frontCol);

        // Create back screw from prefab
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

        // Store start and target positions for movement (move along X-axis)
        backScrewStartPos = backScrewVisual.transform.localPosition;
        backScrewTargetPos = backScrewStartPos + new Vector3(screwMoveInDistance, 0, 0);

        Collider backCol = backScrewVisual.GetComponent<Collider>();
        if (backCol != null) Destroy(backCol);

        // Set initial colors
        SetScrewColor(frontScrewVisual, Color.red);
        SetScrewColor(backScrewVisual, Color.red);
    }

    void SetScrewColor(GameObject screw, Color color)
    {
        if (screw == null) return;

        // Try to find renderer in the screw or its children
        Renderer rend = screw.GetComponent<Renderer>();
        if (rend == null)
            rend = screw.GetComponentInChildren<Renderer>();

        if (rend != null && rend.material != null)
        {
            rend.material.color = color;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (isComplete)
        {
            return;
        }

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

        // Animate screw movement based on progress
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
        transform.position = targetSocket.position;
        transform.rotation = targetSocket.rotation;
        SetColor(validColor);
        lastMousePosition = Input.mousePosition;
        currentState = ScrewState.FrontScrew;

        if (mainCamera != null)
        {
            Vector3 directionToUnderbarrel = (transform.position - mainCamera.transform.position).normalized;
            float distanceToUnderbarrel = Vector3.Distance(mainCamera.transform.position, transform.position);
            targetCameraPosition = mainCamera.transform.position + (directionToUnderbarrel * distanceToUnderbarrel * zoomAmount);
            isZooming = true;
        }

        Debug.Log("Underbarrel snapped! Screw in the FRONT screw by moving mouse in circles.");
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

        if (angleDiff < 0)
        {
            totalRotation += Mathf.Abs(angleDiff);

            if (currentState == ScrewState.FrontScrew)
            {
                float requiredRotation = rotationsRequired * 360f;
                frontScrewProgress = Mathf.Clamp01(totalRotation / requiredRotation);

                // Rotate the screw visually
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

                // Rotate the screw visually
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
        Debug.Log("=== UnderbarrelMinigame CompleteMinigame() called ===");
        transform.position = targetSocket.position;
        transform.rotation = targetSocket.rotation;
        SetColor(Color.green);
        SetScrewColor(frontScrewVisual, Color.green);
        SetScrewColor(backScrewVisual, Color.green);

        if (mainCamera != null)
        {
            targetCameraPosition = originalCameraPosition;
            isZoomingOut = true;
            Debug.Log($"Starting zoom out to: {originalCameraPosition}");
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

        // Destroy screws before cancelling
        DestroyScrews();

        base.CancelMinigame();
    }

    private System.Collections.IEnumerator CompleteAfterZoomOut()
    {
        while (isZoomingOut)
        {
            yield return null;
        }

        // Destroy screws after zoom out completes
        DestroyScrews();

        base.CompleteMinigame();
    }

    /// <summary>
    /// Destroys the screw visual GameObjects
    /// </summary>
    private void DestroyScrews()
    {
        if (frontScrewVisual != null)
        {
            Debug.Log("Destroying front screw visual");
            Destroy(frontScrewVisual);
            frontScrewVisual = null;
        }

        if (backScrewVisual != null)
        {
            Debug.Log("Destroying back screw visual");
            Destroy(backScrewVisual);
            backScrewVisual = null;
        }
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
            Gizmos.color = Color.red;
            Vector3 frontPos = transform.TransformPoint(actualFrontScrewPos);
            Vector3 backPos = transform.TransformPoint(actualBackScrewPos);
            Gizmos.DrawWireSphere(frontPos, actualScrewRadius);
            Gizmos.DrawWireSphere(backPos, actualScrewRadius);
        }
    }
}