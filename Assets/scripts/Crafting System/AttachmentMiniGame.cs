using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base class for attachment minigames
/// Each attachment type can have its own minigame implementation
/// </summary>
public abstract class AttachmentMinigameBase : MonoBehaviour
{
    [Header("Minigame Settings")]
    public AttachmentData attachmentData;
    public Transform targetSocket;
    public WeaponData weaponData;
    public Camera minigameCamera; // The camera used for the preview

    [Header("Visual Feedback")]
    public Color validColor = Color.green;
    public Color invalidColor = Color.red;
    public Color normalColor = Color.white;

    protected Renderer attachmentRenderer;
    protected bool isComplete = false;
    protected bool isDragging = false;

    public UnityAction<AttachmentData> OnMinigameComplete;
    public UnityAction OnMinigameCancelled;

    protected virtual void Awake()
    {
        attachmentRenderer = GetComponent<Renderer>();
        if (attachmentRenderer == null)
            attachmentRenderer = GetComponentInChildren<Renderer>();
    }

    protected virtual void Update()
    {
        if (isComplete) return;

        // Allow cancelling with right click
        if (Input.GetMouseButtonDown(1))
        {
            CancelMinigame();
        }
    }

    /// <summary>
    /// Called when the minigame starts
    /// </summary>
    public virtual void StartMinigame()
    {
        isComplete = false;
        isDragging = false;
    }

    /// <summary>
    /// Called when minigame is successfully completed
    /// </summary>
    protected virtual void CompleteMinigame()
    {
        isComplete = true;
        OnMinigameComplete?.Invoke(attachmentData);
    }

    /// <summary>
    /// Called when minigame is cancelled
    /// </summary>
    protected virtual void CancelMinigame()
    {
        OnMinigameCancelled?.Invoke();
        Destroy(gameObject);
    }

    /// <summary>
    /// Set the material color for visual feedback
    /// </summary>
    protected void SetColor(Color color)
    {
        if (attachmentRenderer != null && attachmentRenderer.material != null)
        {
            attachmentRenderer.material.color = color;
        }
    }

    /// <summary>
    /// Check if attachment is close enough to the socket
    /// </summary>
    protected bool IsNearSocket(float threshold = 0.5f)
    {
        if (targetSocket == null) return false;
        return Vector3.Distance(transform.position, targetSocket.position) < threshold;
    }
}