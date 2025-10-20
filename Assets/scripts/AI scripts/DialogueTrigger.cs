using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Visual Cue")]
    [SerializeField] private GameObject visualCue;

    [Header("Other UI Elements")]
    [SerializeField] private GameObject[] uiToDisable;

    [Header("Ink JSON")]
    [SerializeField] private TextAsset inkJSON;

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement; // Drag in Player object

    private bool playerInRange;

    private void Awake()
    {
        playerInRange = false;
        visualCue.SetActive(false);
    }

    private void Update()
    {
        if (playerInRange && !DialogueManager.GetInstance().dialogueIsPlaying)
        {
            visualCue.SetActive(true);

            if (Input.GetKeyDown(KeyCode.E))
            {
                // Disable gameplay UI and player control
                SetOtherUIActive(false);

                // Switch to UI mode and disable PlayerMovement
                playerMovement.EnableUIMode();
                playerMovement.enabled = false; 

                // Start dialogue
                DialogueManager.GetInstance().EnterDialogueMode(inkJSON);

                // Subscribe to dialogue end event
                DialogueManager.GetInstance().OnDialogueEnd += OnDialogueEnd;
            }
        }
        else
        {
            visualCue.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            visualCue.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            visualCue.SetActive(false);
        }
    }

    private void OnDialogueEnd()
    {
        // Re-enable UI and player movement
        SetOtherUIActive(true);

        // Restore control and lock cursor again
        playerMovement.enabled = true; 
        playerMovement.EnableGameplayMode();

        // Unsubscribe to prevent multiple calls
        DialogueManager.GetInstance().OnDialogueEnd -= OnDialogueEnd;
    }

    private void SetOtherUIActive(bool isActive)
    {
        foreach (GameObject ui in uiToDisable)
        {
            if (ui != null)
                ui.SetActive(isActive);
        }
    }
}
