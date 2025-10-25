using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Interactor : MonoBehaviour
{
    public Transform InteractionPoint;
    public LayerMask InteractionLayer;
    public float InteractionPointRadius = 5f;
    public bool isInteracting { get; private set; }

    private void Update()
    {
        var colliders = Physics.OverlapSphere(InteractionPoint.position, InteractionPointRadius, InteractionLayer);

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                var interactable = colliders[i].GetComponent<IInteractable>();
                if (interactable != null)
                {
                    startInteraction(interactable);
                    break; // Only interact with one object
                }
            }
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            EndInteraction();
        }
    }

    void startInteraction(IInteractable interactable)
    {
        interactable.Interact(this, out bool interactSuccessful);

        // Only set isInteracting to true if the interaction should be held
        isInteracting = interactSuccessful;
    }

    public void EndInteraction()
    {
        isInteracting = false;
    }
}