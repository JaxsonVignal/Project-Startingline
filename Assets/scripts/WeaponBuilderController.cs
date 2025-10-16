using UnityEngine;

public class WeaponBuilderController : MonoBehaviour
{
    [Header("References")]
    public GameObject builderCanvas;      // Assign your WeaponBuilderCanvas
    public GameObject previewContainer;   // Optional: 3D preview parent

    private bool isOpen = false;

    void Start()
    {
        // Ensure builder UI and preview start hidden
        builderCanvas.SetActive(false);
        if (previewContainer != null)
            previewContainer.SetActive(false);
    }

    void Update()
    {
        // Toggle builder with "I"
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleBuilder();
        }
    }

    public void ToggleBuilder()
    {
        isOpen = !isOpen;

        // Show/hide builder UI
        builderCanvas.SetActive(isOpen);

        // Show/hide preview container
        if (previewContainer != null)
            previewContainer.SetActive(isOpen);

        // Optional: pause game while builder is open
        Time.timeScale = isOpen ? 0f : 1f;

        // Show/hide cursor
        Cursor.visible = isOpen;
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
