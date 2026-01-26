using UnityEngine;
using UnityEngine.Events;

public class WeaponBuilderStation : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private WeaponBuilderController builderController;

    public UnityAction<IInteractable> OnInteractionComplete { get; set; }

    public void Interact(Interactor interactor, out bool interactSuccessful)
    {
        if (builderController != null)
        {
            Debug.Log("WeaponBuilderStation: Opening builder");
            builderController.OpenBuilder();
            interactSuccessful = true;
        }
        else
        {
            Debug.LogError("WeaponBuilderStation: builderController not assigned!");
            interactSuccessful = false;
        }
    }

    public void EndInteraction()
    {
        // This gets called when the player presses E to close the interaction
        // OR when the Interactor.EndInteraction() is called from WeaponBuilderController
        Debug.Log("WeaponBuilderStation: EndInteraction called");

        if (builderController != null && builderController.IsBuilderOpen())
        {
            Debug.Log("WeaponBuilderStation: Closing builder");
            builderController.CloseBuilder();
        }

        OnInteractionComplete?.Invoke(this);
    }
}