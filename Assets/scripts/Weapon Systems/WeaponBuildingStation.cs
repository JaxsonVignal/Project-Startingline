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
        if (builderController != null)
        {
            builderController.CloseBuilder();
        }

        OnInteractionComplete?.Invoke(this);
    }
}