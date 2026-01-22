using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Minigame for REMOVING underbarrel attachments - unscrew and drag away
/// </summary>
public class UnderbarrelRemovalMinigame : AttachmentMinigameBase
{
    [Header("Removal Settings")]
    [SerializeField] private float rotationsRequired = 2f;
    [SerializeField] private float circleDetectionRadius = 50f;
    [SerializeField] private float minDistanceToMoveAway = 0.15f;

    [Header("Screw Prefab")]
    [SerializeField] private GameObject screwPrefab;
    [SerializeField] private float screwMoveOutDistance = 0.05f;

    [Header("Camera Zoom Settings")]
    [SerializeField] private float zoomAmount = 0.7f;
    [SerializeField] private float zoomSpeed = 2f;

    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private bool isZooming = false;
    private bool isZoomingOut = false;
    private Vector3 targetCameraPosition;

    private enum RemovalState { UnscrewingFront, UnscrewingBack, MovingAway, Complete }
    private RemovalState currentState = RemovalState.UnscrewingFront;

    private float frontScrewProgress = 0f;
    private float backScrewProgress = 0f;
    private GameObject frontScrewVisual;
    private GameObject backScrewVisual;

    private Vector2 screwCenterScreenPos;
    private float totalRotation = 0f;
    private Vector2 lastMouseAngle;
    private bool isRotating = false;

    private Vector3 socketWorldPosition;
    private bool isDraggingPart = false;
    private Vector3 grabbedPartDragStart;
    private Vector3 grabbedPartStartPos;

    private Vector3 frontScrewPos;
    private Vector3 backScrewPos;
    private float screwRadius;

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
            Debug.LogError("UnderbarrelRemovalMinigame: No camera found!");
            return;
        }

        originalCameraPosition = mainCamera.transform.position;

        if (targetSocket != null)
        {
            socketWorldPosition = targetSocket.position;
        }

        // Get screw positions from attachment data
        frontScrewPos = attachmentData != null ? attachmentData.frontScrewLocalPos : new Vector3(-0.05f, 0.05f, -0.05f);
        backScrewPos = attachmentData != null ? attachmentData.backScrewLocalPos : new Vector3(0.05f, 0.05f, -0.05f);
        screwRadius = attachmentData != null ? attachmentData.screwRadius : 0.015f;

        currentState = RemovalState.UnscrewingFront;

        CreateScrewVisuals();
        ZoomToUnderbarrel();

        Debug.Log("Underbarrel removal started. Unscrew FRONT screw first (clockwise circles).");
    }

    private void CreateScrewVisuals()
    {
        if (screwPrefab != null)
            frontScrewVisual = Instantiate(screwPrefab, transform);
        else
            frontScrewVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        frontScrewVisual.transform.SetParent(transform);
        frontScrewVisual.transform.localPosition = frontScrewPos + new Vector3(screwMoveOutDistance, 0, 0);
        frontScrewVisual.transform.localRotation = Quaternion.Euler(90, 0, 90);
        if (!screwPrefab) frontScrewVisual.transform.localScale = new Vector3(screwRadius * 2, 0.01f, screwRadius * 2);

        Collider col = frontScrewVisual.GetComponent<Collider>();
        if (col) Destroy(col);

        if (screwPrefab != null)
            backScrewVisual = Instantiate(screwPrefab, transform);
        else
            backScrewVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        backScrewVisual.transform.SetParent(transform);
        backScrewVisual.transform.localPosition = backScrewPos + new Vector3(screwMoveOutDistance, 0, 0);
        backScrewVisual.transform.localRotation = Quaternion.Euler(90, 0, 90);
        if (!screwPrefab) backScrewVisual.transform.localScale = new Vector3(screwRadius * 2, 0.01f, screwRadius * 2);

        col = backScrewVisual.GetComponent<Collider>();
        if (col) Destroy(col);

        SetScrewColor(frontScrewVisual, Color.red);
        SetScrewColor(backScrewVisual, Color.red);
    }

    private void SetScrewColor(GameObject screw, Color color)
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

    private void ZoomToUnderbarrel()
    {
        if (mainCamera == null || targetSocket == null) return;

        Vector3 directionToUnderbarrel = (transform.position - mainCamera.transform.position).normalized;
        float distanceToUnderbarrel = Vector3.Distance(mainCamera.transform.position, transform.position);
        targetCameraPosition = mainCamera.transform.position + (directionToUnderbarrel * distanceToUnderbarrel * zoomAmount);
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

        // Animate screws moving OUT
        if (frontScrewVisual != null && frontScrewProgress > 0f)
        {
            Vector3 startPos = frontScrewPos + new Vector3(screwMoveOutDistance, 0, 0);
            Vector3 targetPos = frontScrewPos;
            frontScrewVisual.transform.localPosition = Vector3.Lerp(startPos, targetPos, frontScrewProgress);
        }

        if (backScrewVisual != null && backScrewProgress > 0f)
        {
            Vector3 startPos = backScrewPos + new Vector3(screwMoveOutDistance, 0, 0);
            Vector3 targetPos = backScrewPos;
            backScrewVisual.transform.localPosition = Vector3.Lerp(startPos, targetPos, backScrewProgress);
        }

        switch (currentState)
        {
            case RemovalState.UnscrewingFront:
            case RemovalState.UnscrewingBack:
                HandleUnscrewing();
                break;
            case RemovalState.MovingAway:
                HandleMovingAway();
                break;
        }
    }

    void HandleUnscrewing()
    {
        Vector3 mousePos = Input.mousePosition;

        Vector3 frontScrewWorldPos = transform.TransformPoint(frontScrewPos);
        Vector3 backScrewWorldPos = transform.TransformPoint(backScrewPos);

        Vector3 frontScrewScreenPos = mainCamera.WorldToScreenPoint(frontScrewWorldPos);
        Vector3 backScrewScreenPos = mainCamera.WorldToScreenPoint(backScrewWorldPos);

        float distToFront = Vector2.Distance(new Vector2(mousePos.x, mousePos.y), new Vector2(frontScrewScreenPos.x, frontScrewScreenPos.y));
        float distToBack = Vector2.Distance(new Vector2(mousePos.x, mousePos.y), new Vector2(backScrewScreenPos.x, backScrewScreenPos.y));

        if (currentState == RemovalState.UnscrewingFront)
        {
            SetScrewColor(frontScrewVisual, distToFront < circleDetectionRadius ? Color.yellow : Color.red);

            if (Input.GetMouseButtonDown(0) && distToFront < circleDetectionRadius)
            {
                screwCenterScreenPos = new Vector2(frontScrewScreenPos.x, frontScrewScreenPos.y);
                StartCircularMotion(mousePos);
            }
        }
        else if (currentState == RemovalState.UnscrewingBack)
        {
            SetScrewColor(backScrewVisual, distToBack < circleDetectionRadius ? Color.yellow : Color.red);

            if (Input.GetMouseButtonDown(0) && distToBack < circleDetectionRadius)
            {
                screwCenterScreenPos = new Vector2(backScrewScreenPos.x, backScrewScreenPos.y);
                StartCircularMotion(mousePos);
            }
        }

        if (Input.GetMouseButton(0) && isRotating)
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

        // Unscrewing = CLOCKWISE
        if (angleDiff > 0)
        {
            totalRotation += Mathf.Abs(angleDiff);
            float requiredRotation = rotationsRequired * 360f;

            if (currentState == RemovalState.UnscrewingFront)
            {
                frontScrewProgress = Mathf.Clamp01(totalRotation / requiredRotation);

                if (frontScrewVisual != null)
                    frontScrewVisual.transform.Rotate(Vector3.up, -Mathf.Abs(angleDiff), Space.Self);

                SetScrewColor(frontScrewVisual, Color.Lerp(Color.red, Color.green, frontScrewProgress));

                if (frontScrewProgress >= 1f)
                {
                    Debug.Log("Front screw done! Now back screw");
                    currentState = RemovalState.UnscrewingBack;
                    isRotating = false;
                }
            }
            else if (currentState == RemovalState.UnscrewingBack)
            {
                backScrewProgress = Mathf.Clamp01(totalRotation / requiredRotation);

                if (backScrewVisual != null)
                    backScrewVisual.transform.Rotate(Vector3.up, -Mathf.Abs(angleDiff), Space.Self);

                SetScrewColor(backScrewVisual, Color.Lerp(Color.red, Color.green, backScrewProgress));

                if (backScrewProgress >= 1f)
                {
                    Debug.Log("Both screws done! Move underbarrel away");
                    currentState = RemovalState.MovingAway;
                    isRotating = false;

                    if (frontScrewVisual) Destroy(frontScrewVisual);
                    if (backScrewVisual) Destroy(backScrewVisual);

                    SetColor(Color.yellow);
                }
            }
        }

        lastMouseAngle = currentMouseAngle;
    }

    void HandleMovingAway()
    {
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
                    Debug.Log($"Grabbed underbarrel: {gameObject.name}");
                    break;
                }
            }
        }

        if (isDraggingPart && Input.GetMouseButton(0))
        {
            Vector3 currentMousePos = Input.mousePosition;
            Vector3 mouseDelta = currentMousePos - grabbedPartDragStart;

            Vector3 worldDelta = mainCamera.ScreenToWorldPoint(new Vector3(mouseDelta.x, mouseDelta.y, mainCamera.WorldToScreenPoint(grabbedPartStartPos).z))
                                - mainCamera.ScreenToWorldPoint(new Vector3(0, 0, mainCamera.WorldToScreenPoint(grabbedPartStartPos).z));

            transform.position = grabbedPartStartPos + worldDelta;

            float dist = Vector3.Distance(transform.position, socketWorldPosition);
            Color feedbackColor = dist >= minDistanceToMoveAway ? Color.green : Color.yellow;
            SetColor(feedbackColor);
        }

        if (Input.GetMouseButtonUp(0) && isDraggingPart)
        {
            float dist = Vector3.Distance(transform.position, socketWorldPosition);

            if (dist >= minDistanceToMoveAway)
            {
                Debug.Log("Underbarrel moved away! Removal complete!");
                CompleteMinigame();
            }
            else
            {
                Debug.Log("Not far enough!");
            }

            isDraggingPart = false;
        }
    }

    protected override void CompleteMinigame()
    {
        SetColor(Color.green);

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

        DestroyScrews();
        base.CancelMinigame();
    }

    private void DestroyScrews()
    {
        if (frontScrewVisual != null) Destroy(frontScrewVisual);
        if (backScrewVisual != null) Destroy(backScrewVisual);
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

    void OnDrawGizmos()
    {
        if (targetSocket != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetSocket.position, minDistanceToMoveAway);
        }
    }
}