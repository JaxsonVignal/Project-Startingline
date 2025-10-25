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
        // Reset time
        var dayNight = GameObject.FindObjectOfType<DayNightCycleManager>();
        if (dayNight != null)
            dayNight.SetTime(morningHour);

        // Save game using the existing save data
        SaveGameManager.SaveData();

        // Reset all NPCs to bed
        var allNpcs = GameObject.FindObjectsOfType<NPCManager>();
        foreach (var npc in allNpcs)
        {
            npc.ResetToBed();
        }

        interactSuccessful = false; // Don't hold the interaction

        Debug.Log("Time reset, game saved, and all NPCs reset to bed.");
    }
}