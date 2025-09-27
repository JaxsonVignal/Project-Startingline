
using System.Windows.Input;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Visual Cue")]
    [SerializeField] private GameObject visualCue; // Visual cue to indicate interaction availability

    private bool playerInRange; // Track if the player is in range

    [Header("Ink JSON")]
    [SerializeField] private TextAsset inkJSON; // Reference to the Ink JSON file

    private void Awake()
    {
        playerInRange = false;
        visualCue.SetActive(false); // Ensure the visual cue is initially inactive
    }

    private void Update()
    {
        if (playerInRange && !DialogueManager.GetInstance().dialogueIsPlaying)
        {
            visualCue.SetActive(true); // Show the visual cue when the player is in range
            if (Input.GetKeyDown(KeyCode.E)) // Check for interaction key press
            {
                DialogueManager.GetInstance().EnterDialogueMode(inkJSON); // Start the dialogue
            }
        }
        else
        {
            visualCue.SetActive(false); // Hide the visual cue when the player is out of range
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            visualCue.SetActive(true); // Show the visual cue when the player enters the trigger
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            visualCue.SetActive(false); // Hide the visual cue when the player exits the trigger
        }
    }
}
