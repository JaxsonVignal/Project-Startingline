using UnityEngine;
using UnityEngine.Events;

public class TimeResetInteractable : MonoBehaviour, IInteractable
{
    [Header("Time Settings")]
    public float morningHour = 6f;

    public UnityAction<IInteractable> OnInteractionComplete { get; set; }

    public void EndInteraction()
    {
        // Not needed for instant interactions
    }

    public void Interact(Interactor interactor, out bool interactSuccessful)
    {
        // Get day/night manager
        var dayNight = FindObjectOfType<DayNightCycleManager>();
        if (dayNight != null)
        {
            // Call PlayerSlept to reset time and unpause if needed
            // This will advance the day and trigger the sleeping system naturally
            dayNight.PlayerSlept(morningHour);
        }

        // Save game using the existing save data
        SaveGameManager.SaveData();

        // DON'T call ResetToBed() - the NPCSleepManager handles waking NPCs automatically
        // When the day changes, NPCs that are asleep (disabled) will be woken up and spawned
        // at their first scheduled location for the new day

        interactSuccessful = false; // Don't hold the interaction
        Debug.Log("Time reset and game saved. NPCs will wake naturally.");
    }
}