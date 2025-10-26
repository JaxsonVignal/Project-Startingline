using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Minigame for attaching scopes - drag to sight socket, then screw in front and back screws
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

    [Header("Screw Positions (relative to scope)")]
    [SerializeField] private Vector3 frontScrewLocalPos = new Vector3(-0.05f, 0.05f, -0.05f);
    [SerializeField] private Vector3 backScrewLocalPos = new Vector3(0.05f, 0.05f, -0.05f);
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

    private Vector3 actualFrontScrewPos;
    private Vector3 actualBackScrewPos;
    private float actualScrewRadius;

    private Vector3 originalCameraPosition;
    private bool isZooming = false;
    private bool isZoomingOut = false;
    private Vector3 targetCameraPosition;

    private List<GameObject> weaponPartsToDisable = new List<GameObject>(); // Retrieved from WeaponData

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
        Debug.Log($"ScopeMinigame started. Camera: {mainCamera.name}, Position: {originalCameraPosition}");

        if (targetSocket != null)
        {
            Vector3 worldOffset = targetSocket.TransformDirection(spawnOffset);
            transform.position = targetSocket.position + worldOffset;
            transform.rotation = targetSocket.rotation;
        }

        CreateScrewVisuals();
        SetColor(normalColor);
    }

    /// <summary>
    /// Set multiple weapon parts that should be disabled when the scope is attached.
    /// Call this before starting the minigame.
    /// </summary>
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

            // Try to find the part by path
            Transform partTransform = weaponTransform.Find(partPath);

            if (partTransform == null)
            {
                Debug.Log($"Could not find '{partPath}' using Find(), searching recursively...");
                // If not found by path, search all children by name
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

    /// <summary>
    /// LEGACY: Set a single weapon part to disable (for backwards compatibility)
    /// </summary>
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

    void CreateScrewVisuals()
    {
        bool usingAttachmentData = attachmentData != null;
        actualFrontScrewPos = attachmentData != null ? attachmentData.frontScrewLocalPos : frontScrewLocalPos;
        actualBackScrewPos = attachmentData != null ? attachmentData.backScrewLocalPos : backScrewLocalPos;
        actualScrewRadius = attachmentData != null ? attachmentData.screwRadius : screwRadius;

        Debug.Log($"Using AttachmentData: {usingAttachmentData}");
        Debug.Log($"Creating screws at: Front={actualFrontScrewPos}, Back={actualBackScrewPos}, Radius={actualScrewRadius}");

        frontScrewVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        frontScrewVisual.transform.SetParent(transform);
        frontScrewVisual.transform.localPosition = actualFrontScrewPos;
        frontScrewVisual.transform.localRotation = Quaternion.Euler(90, 0, 90);
        frontScrewVisual.transform.localScale = new Vector3(actualScrewRadius * 2, 0.01f, actualScrewRadius * 2);
        Destroy(frontScrewVisual.GetComponent<Collider>());

        backScrewVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        backScrewVisual.transform.SetParent(transform);
        backScrewVisual.transform.localPosition = actualBackScrewPos;
        backScrewVisual.transform.localRotation = Quaternion.Euler(90, 0, 90);
        backScrewVisual.transform.localScale = new Vector3(actualScrewRadius * 2, 0.01f, actualScrewRadius * 2);
        Destroy(backScrewVisual.GetComponent<Collider>());

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

    protected override void Update()
    {
        base.Update();

        if (isComplete)
        {
            Debug.Log("Update: isComplete=true, returning early");
            return;
        }

        if ((isZooming || isZoomingOut) && mainCamera != null)
        {
            Debug.Log($"Camera moving: isZooming={isZooming}, isZoomingOut={isZoomingOut}, targetPos={targetCameraPosition}, currentPos={mainCamera.transform.position}");

            mainCamera.transform.position = Vector3.Lerp(
                mainCamera.transform.position,
                targetCameraPosition,
                Time.deltaTime * zoomSpeed
            );

            if (Vector3.Distance(mainCamera.transform.position, targetCameraPosition) < 0.01f)
            {
                mainCamera.transform.position = targetCameraPosition;
                Debug.Log($"Camera reached target. Setting isZooming=false, isZoomingOut=false");
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
            Vector3 directionToScope = (transform.position - mainCamera.transform.position).normalized;
            float distanceToScope = Vector3.Distance(mainCamera.transform.position, transform.position);
            targetCameraPosition = mainCamera.transform.position + (directionToScope * distanceToScope * zoomAmount);
            isZooming = true;
        }

        Debug.Log("Scope snapped! Screw in the FRONT screw by moving mouse in circles.");
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
                frontScrewVisual.transform.Rotate(Vector3.up, Mathf.Abs(angleDiff), Space.Self);

                Color progressColor = Color.Lerp(Color.red, Color.green, frontScrewProgress);
                SetScrewColor(frontScrewVisual, progressColor);

                if (frontScrewProgress >= 1f)
                {
                    Debug.Log($"Front screw complete! Checking if back screw is also done: backScrewProgress={backScrewProgress}");
                    currentState = ScrewState.None;
                    isRotating = false;

                    // Check if BOTH screws are now complete
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
                backScrewVisual.transform.Rotate(Vector3.up, Mathf.Abs(angleDiff), Space.Self);

                Color progressColor = Color.Lerp(Color.red, Color.green, backScrewProgress);
                SetScrewColor(backScrewVisual, progressColor);

                if (backScrewProgress >= 1f)
                {
                    Debug.Log($"Back screw complete! Checking if front screw is also done: frontScrewProgress={frontScrewProgress}");
                    currentState = ScrewState.None;
                    isRotating = false;

                    // Check if BOTH screws are now complete
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

        // Disable all weapon parts (e.g., iron sights) when scope is attached
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
        else
        {
            Debug.LogWarning("weaponPartsToDisable is empty or NULL - no weapon parts to disable!");
        }

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

        // Re-enable all weapon parts if minigame was cancelled
        if (weaponPartsToDisable != null && weaponPartsToDisable.Count > 0)
        {
            Debug.Log($"Re-enabling {weaponPartsToDisable.Count} weapon part(s) after cancel");
            foreach (var part in weaponPartsToDisable)
            {
                if (part != null && !part.activeSelf)
                {
                    part.SetActive(true);
                    Debug.Log($"  Re-enabled weapon part: {part.name}");
                }
            }
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