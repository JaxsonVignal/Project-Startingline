using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class NPCInteractor : MonoBehaviour
{
    public Transform InteractionPoint;
    public LayerMask NPCLayer;
    public float InteractionPointRadius = 5f;
    public bool isInteracting { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup playerUICanvasGroup; // Reference to your player UI
    [SerializeField] private GameObject playerUI; // Alternative: direct GameObject reference

    private INPCInteractable currentNPC;
    private PlayerMovement playerMovement;

    private void Start()
    {
        // Get reference to PlayerMovement
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogWarning("NPCInteractor: PlayerMovement component not found on same GameObject!");
        }

        // Make sure UI is visible at start
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
        var colliders = Physics.OverlapSphere(InteractionPoint.position, InteractionPointRadius, NPCLayer);

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            // If already interacting, end the interaction
            if (isInteracting)
            {
                EndInteraction();
                return;
            }

            // Otherwise, find the closest interactable NPC
            INPCInteractable closestNPC = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < colliders.Length; i++)
            {
                var npcInteractable = colliders[i].GetComponent<INPCInteractable>();
                if (npcInteractable != null)
                {
                    // Check if it's an NPCManager and if it can be interacted with
                    var npcManager = colliders[i].GetComponent<NPCManager>();
                    if (npcManager != null && !npcManager.CanBeInteractedWith())
                    {
                        continue; // Skip this NPC
                    }

                    float distance = Vector3.Distance(InteractionPoint.position, colliders[i].transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestNPC = npcInteractable;
                    }
                }
            }

            // Interact with the closest NPC
            if (closestNPC != null)
            {
                StartInteraction(closestNPC);
            }
        }
    }

    void StartInteraction(INPCInteractable npcInteractable)
    {
        npcInteractable.Interact(this, out bool interactSuccessful);

        if (interactSuccessful)
        {
            isInteracting = true;
            currentNPC = npcInteractable;

            // Enter UI mode
            if (playerMovement != null)
            {
                playerMovement.EnableUIMode();
            }

            // Hide player UI
            HidePlayerUI();
        }
    }

    public void EndInteraction()
    {
        if (isInteracting && currentNPC != null)
        {
            currentNPC.EndInteraction();
            currentNPC = null;
        }

        isInteracting = false;

        // Return to gameplay mode
        if (playerMovement != null)
        {
            playerMovement.EnableGameplayMode();
        }

        // Show player UI
        ShowPlayerUI();
    }

    // Hide the player UI
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

    // Show the player UI
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