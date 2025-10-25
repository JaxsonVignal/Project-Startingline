using UnityEngine;
using UnityEngine.Events;

public class TimeResetInteractable : MonoBehaviour, IInteractable
{
    [Header("Time Settings")]
    public float morningHour = 6f;

    public UnityAction<IInteractable> OnInteractionComplete { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public void EndInteraction()
    {
        throw new System.NotImplementedException();
    }

    public void Interact(Interactor interactor, out bool interactSuccessful)
    {
        interactSuccessful = false;

        // Reset time
        var dayNight = GameObject.FindObjectOfType<DayNightCycleManager>();
        if (dayNight != null)
            dayNight.SetTime(morningHour);

        // Save game using the EXISTING save data (not a new empty one)
        SaveGameManager.SaveData(); // Use this instead!

        // Reset all NPCs to bed
        var allNpcs = GameObject.FindObjectsOfType<NPCManager>();
        foreach (var npc in allNpcs)
        {
            npc.ResetToBed();
        }

        interactSuccessful = true;
        Debug.Log("Time reset, game saved, and all NPCs reset to bed.");
    }
}
