using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the weapon builder UI visibility and input handling
/// </summary>
public class WeaponBuilderController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject builderUIPanel;
    [SerializeField] private GameObject previewContainer;
    [SerializeField] private WeaponBuilderUI builderUI;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private GameObject mainUIPanel;
    [SerializeField] private HotbarDisplay playerHotbar;
    [SerializeField] private AttachmentMinigameManager minigameManager;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Interactor playerInteractor;

    [Header("Player UI References")]
    [SerializeField] private CanvasGroup playerUICanvasGroup; // NEW: Reference to your player UI
    [SerializeField] private GameObject playerUI; // NEW: Alternative GameObject reference

    [Header("Settings")]
    [SerializeField] private bool pauseGameWhenOpen = true;
    [SerializeField] private KeyCode exitKey = KeyCode.Escape;

    private bool isBuilderOpen = false;
    private Vector3 originalPreviewCameraPosition;
    private Quaternion originalPreviewCameraRotation;

    private void Start()
    {
        // Ensure builder is closed on start
        CloseBuilder();

        // Try to find PlayerMovement if not assigned
        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerMovement>();
            if (playerMovement == null)
            {
                Debug.LogWarning("WeaponBuilderController: PlayerMovement not found! Assign it manually.");
            }
        }

        // Try to find Interactor if not assigned
        if (playerInteractor == null)
        {
            playerInteractor = FindObjectOfType<Interactor>();
            if (playerInteractor == null)
            {
                Debug.LogWarning("WeaponBuilderController: Interactor not found! Assign it manually.");
            }
        }

        // NEW: Make sure player UI is visible at start
        if (playerUICanvasGroup != null)
        {
            playerUICanvasGroup.alpha = 1f;
            playerUICanvasGroup.interactable = true;
            playerUICanvasGroup.blocksRaycasts = true;
        }

        if (playerUI != null)
        {
            playerUI.SetActive(true);
        }
    }

    private void Update()
    {
        // Allow closing with Escape key
        if (isBuilderOpen && Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            CloseBuilder();
        }
    }

    public void OpenBuilder()
    {
        if (isBuilderOpen) return;
        isBuilderOpen = true;

        // Unequip current weapon from player
        if (playerHotbar != null)
        {
            playerHotbar.UnequipWeapon();
        }

        // Hide main UI
        if (mainUIPanel != null)
            mainUIPanel.SetActive(false);

        // NEW: Hide player UI
        HidePlayerUI();

        // Show builder UI
        if (builderUIPanel != null)
            builderUIPanel.SetActive(true);

        // Show preview container
        if (previewContainer != null)
            previewContainer.SetActive(true);

        // Refresh available items from inventory
        if (builderUI != null)
            builderUI.RefreshAvailableItems();

        // Enable preview camera if it exists
        if (previewCamera != null)
        {
            // Store the original position and rotation
            originalPreviewCameraPosition = previewCamera.transform.position;
            originalPreviewCameraRotation = previewCamera.transform.rotation;
            previewCamera.enabled = true;
        }

        // Disable main camera if needed
        if (mainCamera != null && previewCamera != null)
            mainCamera.enabled = false;

        // Enter UI mode
        if (playerMovement != null)
        {
            playerMovement.EnableUIMode();
        }
        else
        {
            // Fallback: Just show cursor if PlayerMovement is missing
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        Debug.Log("Weapon Builder opened");
    }

    public void CloseBuilder()
    {
        if (!isBuilderOpen) return;

        Debug.Log("=== CLOSING WEAPON BUILDER ===");

        // ALWAYS reset the preview camera position first, before anything else
        if (previewCamera != null)
        {
            previewCamera.transform.position = originalPreviewCameraPosition;
            previewCamera.transform.rotation = originalPreviewCameraRotation;
            Debug.Log($"Reset preview camera to original position: {originalPreviewCameraPosition}");
        }

        // Check if minigame is active
        bool hadActiveMinigame = minigameManager != null && minigameManager.IsMinigameActive();

        if (hadActiveMinigame)
        {
            Debug.Log("Closing builder - cancelling active minigame");
            minigameManager.CancelCurrentMinigame();
        }
        else
        {
            // No active minigame - safe to clean up preview
            if (builderUI != null)
            {
                Debug.Log("Cleaning up preview before closing builder (no active minigame)");
                builderUI.CleanupPreviewContainer();
            }
        }

        isBuilderOpen = false;

        // Hide builder UI
        if (builderUIPanel != null)
            builderUIPanel.SetActive(false);

        // Hide preview container
        if (previewContainer != null)
            previewContainer.SetActive(false);

        // Disable preview camera
        if (previewCamera != null)
            previewCamera.enabled = false;

        // Re-enable main camera
        if (mainCamera != null)
            mainCamera.enabled = true;

        // Re-enable main UI
        if (mainUIPanel != null)
            mainUIPanel.SetActive(true);

        // NEW: Show player UI
        ShowPlayerUI();

        // End the interaction in the Interactor system
        if (playerInteractor != null)
        {
            Debug.Log("Ending interaction via Interactor.EndInteraction()");
            playerInteractor.EndInteraction();
        }

        // Return to gameplay mode
        if (playerMovement != null)
        {
            Debug.Log("Calling EnableGameplayMode()");
            playerMovement.EnableGameplayMode();
        }
        else
        {
            // Fallback: Just hide cursor if PlayerMovement is missing
            Debug.LogWarning("PlayerMovement is null, using fallback cursor settings");
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Debug.Log("Weapon Builder closed - gameplay mode should be restored");
    }

    public void ToggleBuilder()
    {
        if (isBuilderOpen)
            CloseBuilder();
        else
            OpenBuilder();
    }

    public bool IsBuilderOpen()
    {
        return isBuilderOpen;
    }

    // NEW: Hide the player UI
    private void HidePlayerUI()
    {
        // Option 1: Using CanvasGroup (recommended for fade effects)
        if (playerUICanvasGroup != null)
        {
            playerUICanvasGroup.alpha = 0f;
            playerUICanvasGroup.interactable = false;
            playerUICanvasGroup.blocksRaycasts = false;
        }

        // Option 2: Using SetActive (complete disable)
        if (playerUI != null)
        {
            playerUI.SetActive(false);
        }
    }

    // NEW: Show the player UI
    private void ShowPlayerUI()
    {
        // Option 1: Using CanvasGroup
        if (playerUICanvasGroup != null)
        {
            playerUICanvasGroup.alpha = 1f;
            playerUICanvasGroup.interactable = true;
            playerUICanvasGroup.blocksRaycasts = true;
        }

        // Option 2: Using SetActive
        if (playerUI != null)
        {
            playerUI.SetActive(true);
        }
    }
}