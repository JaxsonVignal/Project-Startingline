using UnityEngine;
using System;

/// <summary>
/// Manages which minigame to spawn for each attachment type
/// Add this component to your WeaponBuilderUI or create a separate manager
/// </summary>
public class AttachmentMinigameManager : MonoBehaviour
{
    [Header("Minigame Prefabs")]
    [SerializeField] private GameObject silencerMinigamePrefab;
    // Add more minigame prefabs here for other attachment types

    [Header("References")]
    [SerializeField] private Transform minigameParent; // Where to spawn minigames
    [SerializeField] private Camera previewCamera; // The camera used for weapon preview

    private AttachmentMinigameBase currentMinigame;
    private Action<AttachmentData> onMinigameCompleteCallback;

    /// <summary>
    /// Start a minigame for the given attachment
    /// </summary>
    public void StartMinigame(AttachmentData attachment, WeaponData weapon, Transform socket, Action<AttachmentData> onComplete)
    {
        Debug.Log($"AttachmentMinigameManager.StartMinigame called for {attachment.Name}");
        Debug.Log($"Preview camera assigned: {(previewCamera != null ? previewCamera.name : "NULL")}");

        // Cancel any existing minigame
        if (currentMinigame != null)
        {
            Destroy(currentMinigame.gameObject);
        }

        onMinigameCompleteCallback = onComplete;

        // Check if this attachment type has a minigame implementation
        if (!HasMinigameImplementation(attachment.type))
        {
            Debug.LogWarning($"No minigame implementation for {attachment.type}. Adding attachment directly.");
            onComplete?.Invoke(attachment);
            return;
        }

        // Spawn the attachment prefab
        GameObject attachmentObj = Instantiate(attachment.prefab, minigameParent);
        Debug.Log($"Spawned attachment object: {attachmentObj.name}");

        // Add collider if it doesn't have one (needed for mouse interaction)
        if (attachmentObj.GetComponent<Collider>() == null)
        {
            var collider = attachmentObj.AddComponent<BoxCollider>();
            Debug.Log("Added BoxCollider to attachment");
        }

        // Add the appropriate minigame component
        AttachmentMinigameBase minigame = null;

        switch (attachment.type)
        {
            case AttachmentType.Barrel:
                minigame = attachmentObj.AddComponent<SilencerMinigame>();
                Debug.Log("Added SilencerMinigame component");
                break;
            // Add more cases for other attachment types
            default:
                Debug.LogWarning($"No minigame implementation for {attachment.type}");
                onComplete?.Invoke(attachment);
                Destroy(attachmentObj);
                return;
        }

        // Configure the minigame BEFORE starting it
        Debug.Log("Configuring minigame...");
        minigame.attachmentData = attachment;
        minigame.targetSocket = socket;
        minigame.weaponData = weapon;
        minigame.minigameCamera = previewCamera; // Pass the preview camera

        Debug.Log($"Set minigameCamera to: {(minigame.minigameCamera != null ? minigame.minigameCamera.name : "NULL")}");

        // Subscribe to events
        minigame.OnMinigameComplete += OnMinigameCompleted;
        minigame.OnMinigameCancelled += OnMinigameCancelled;

        currentMinigame = minigame;

        // Start the minigame AFTER everything is configured
        Debug.Log("Starting minigame...");
        minigame.StartMinigame();

        Debug.Log($"Started {attachment.type} minigame for {attachment.Name}");
    }

    /// <summary>
    /// Check if a minigame implementation exists for this attachment type
    /// </summary>
    private bool HasMinigameImplementation(AttachmentType type)
    {
        switch (type)
        {
            case AttachmentType.Barrel:
                return true;
            // Add more types as you implement them
            default:
                return false;
        }
    }

    private GameObject GetMinigamePrefabForAttachment(AttachmentData attachment)
    {
        // This method is now optional - keeping for backwards compatibility
        switch (attachment.type)
        {
            case AttachmentType.Barrel:
                return silencerMinigamePrefab;
            // Add more cases for other types
            default:
                return null;
        }
    }

    private void OnMinigameCompleted(AttachmentData attachment)
    {
        Debug.Log($"Minigame completed for {attachment.Name}");

        // Clean up the minigame object
        if (currentMinigame != null)
        {
            Destroy(currentMinigame.gameObject);
            currentMinigame = null;
        }

        // Notify callback
        onMinigameCompleteCallback?.Invoke(attachment);
        onMinigameCompleteCallback = null;
    }

    private void OnMinigameCancelled()
    {
        Debug.Log("Minigame cancelled");

        currentMinigame = null;
        onMinigameCompleteCallback = null;
    }

    /// <summary>
    /// Check if a minigame is currently active
    /// </summary>
    public bool IsMinigameActive()
    {
        return currentMinigame != null;
    }

    /// <summary>
    /// Cancel the current minigame
    /// </summary>
    public void CancelCurrentMinigame()
    {
        if (currentMinigame != null)
        {
            Destroy(currentMinigame.gameObject);
            currentMinigame = null;
            onMinigameCompleteCallback = null;
        }
    }
}