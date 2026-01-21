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

    [Header("Settings")]
    [SerializeField] private bool pauseGameWhenOpen = true;
    [SerializeField] private KeyCode exitKey = KeyCode.Escape;

    private bool isBuilderOpen = false;
    private Vector3 originalPreviewCameraPosition; // Store original position
    private Quaternion originalPreviewCameraRotation; // Store original rotation

    private void Start()
    {
        // Ensure builder is closed on start
        CloseBuilder();
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

        // Pause game
        // if (pauseGameWhenOpen)
        //     Time.timeScale = 0f;

        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Weapon Builder opened");
    }

    public void CloseBuilder()
    {
        if (!isBuilderOpen) return;

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
            // DON'T call CleanupPreviewContainer - let the minigame cleanup handle everything
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

        // Unpause game
        // if (pauseGameWhenOpen)
        //     Time.timeScale = 1f;

        // Hide cursor (adjust based on your game's needs)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Weapon Builder closed");
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
}