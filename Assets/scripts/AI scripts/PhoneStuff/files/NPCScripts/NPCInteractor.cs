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

    private INPCInteractable currentNPC;
    private PlayerMovement playerMovement; // NEW

    private void Start()
    {
        // NEW: Get reference to PlayerMovement
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogWarning("NPCInteractor: PlayerMovement component not found on same GameObject!");
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

            // NEW: Enter UI mode
            if (playerMovement != null)
            {
                playerMovement.EnableUIMode();
            }
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

        // NEW: Return to gameplay mode
        if (playerMovement != null)
        {
            playerMovement.EnableGameplayMode();
        }
    }
}