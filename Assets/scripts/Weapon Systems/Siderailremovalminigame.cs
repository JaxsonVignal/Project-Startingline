using UnityEngine;

/// <summary>
/// Minigame for REMOVING siderail attachments - slide off and drag away
/// </summary>
public class SiderailRemovalMinigame : AttachmentMinigameBase
{
    [Header("Removal Settings")]
    [SerializeField] private float slideDistance = 0.1f;
    [SerializeField] private float slideSpeed = 2f;
    [SerializeField] private Vector3 slideDirection = Vector3.back; // Slide backwards (negative Z)
    [SerializeField] private float minDistanceToMoveAway = 0.15f;

    [Header("Camera Zoom Settings")]
    [SerializeField] private float zoomAmount = 0.7f;
    [SerializeField] private float zoomSpeed = 2f;

    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private bool isZooming = false;
    private bool isZoomingOut = false;
    private Vector3 targetCameraPosition;

    private enum RemovalState { SlidingOff, MovingAway, Complete }
    private RemovalState currentState = RemovalState.SlidingOff;

    private bool isSliding = false;
    private float slideProgress = 0f;
    private Vector3 slideStartPosition;
    private Vector3 slideTargetPosition;

    private Vector3 socketWorldPosition;
    private bool isDraggingPart = false;
    private Vector3 grabbedPartDragStart;
    private Vector3 grabbedPartStartPos;

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
            Debug.LogError("SiderailRemovalMinigame: No camera found!");
            return;
        }

        originalCameraPosition = mainCamera.transform.position;

        if (targetSocket != null)
        {
            socketWorldPosition = targetSocket.position;
        }

        currentState = RemovalState.SlidingOff;

        // Calculate slide positions
        Vector3 worldSlideDirection = transform.TransformDirection(slideDirection);
        slideStartPosition = transform.position;
        slideTargetPosition = slideStartPosition + (worldSlideDirection * slideDistance);

        ZoomToSiderail();

        Debug.Log("Siderail removal started. Click to slide off the rail.");
    }

    private void ZoomToSiderail()
    {
        if (mainCamera == null || targetSocket == null) return;

        Vector3 directionToSiderail = (transform.position - mainCamera.transform.position).normalized;
        float distanceToSiderail = Vector3.Distance(mainCamera.transform.position, transform.position);
        targetCameraPosition = mainCamera.transform.position + (directionToSiderail * distanceToSiderail * zoomAmount);
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

        // Animate sliding
        if (currentState == RemovalState.SlidingOff && isSliding)
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
                currentState = RemovalState.MovingAway;
                Debug.Log("Slid off! Now drag it away");
            }
        }

        switch (currentState)
        {
            case RemovalState.SlidingOff:
                HandleSlidingOff();
                break;
            case RemovalState.MovingAway:
                HandleMovingAway();
                break;
        }
    }

    void HandleSlidingOff()
    {
        if (Input.GetMouseButtonDown(0) && !isSliding)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                {
                    Debug.Log("Starting slide off animation");
                    isSliding = true;
                    slideProgress = 0f;
                    break;
                }
            }
        }
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
                    Debug.Log($"Grabbed siderail: {gameObject.name}");
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
                Debug.Log("Siderail moved away! Removal complete!");
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

            // Draw slide direction
            Gizmos.color = Color.blue;
            Vector3 worldSlideDir = transform.TransformDirection(slideDirection);
            Gizmos.DrawRay(transform.position, worldSlideDir * slideDistance);
        }
    }
}