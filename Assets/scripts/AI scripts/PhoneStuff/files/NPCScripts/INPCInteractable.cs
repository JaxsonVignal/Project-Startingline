using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public interface INPCInteractable
{
    public UnityAction<INPCInteractable> OnInteractionComplete { get; set; }
    public void Interact(NPCInteractor interactor, out bool interactSuccessful);
    public void EndInteraction();
}