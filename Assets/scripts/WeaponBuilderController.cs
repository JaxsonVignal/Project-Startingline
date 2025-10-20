using UnityEngine;

public class WeaponBuilderController : MonoBehaviour
{
    [Header("References")]
    public GameObject builderCanvas;      // Assign your WeaponBuilderCanvas
    public GameObject previewContainer;   // Optional: 3D preview parent
    [SerializeField] private Interactor interactor;

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
        // Allow Escape to close builder even during pause
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseBuilder();
        }
    }

    public void OpenBuilder()
    {
        isOpen = true;
        builderCanvas.SetActive(true);
        if (previewContainer != null)
            previewContainer.SetActive(true);
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseBuilder()
    {
        isOpen = false;
        builderCanvas.SetActive(false);
        if (previewContainer != null)
            previewContainer.SetActive(false);
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Notify interactor that interaction ended
        if (interactor != null)
            interactor.EndInteraction();
    }

    public bool IsOpen() => isOpen;
}