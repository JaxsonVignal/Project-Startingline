using UnityEngine;

/// <summary>
/// Simple script to trigger wanted status - can be attached to crime zones,
/// or called from other scripts when player commits a crime
/// </summary>
public class WantedTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public bool triggerOnEnter = true;
    public string playerTag = "Player";

    [Header("Wanted Settings")]
    public bool makeWanted = true;
    public float addWantedTime = 0f; // Optional: add extra time to wanted timer

    private void OnTriggerEnter(Collider other)
    {
        if (!triggerOnEnter) return;

        if (other.CompareTag(playerTag))
        {
            TriggerWanted();
        }
    }

    public void TriggerWanted()
    {
        if (WantedSystem.Instance == null)
        {
            Debug.LogWarning("WantedSystem instance not found!");
            return;
        }

        if (makeWanted)
        {
            WantedSystem.Instance.SetWanted(true);
            Debug.Log("Player is now wanted!");
        }

        if (addWantedTime > 0f)
        {
            WantedSystem.Instance.AddWantedTime(addWantedTime);
            Debug.Log($"Added {addWantedTime}s to wanted time");
        }
    }

    // Public method that can be called from other scripts
    public static void MakePlayerWanted()
    {
        if (WantedSystem.Instance != null)
        {
            WantedSystem.Instance.SetWanted(true);
        }
    }

    // Public method to clear wanted status (for hideouts, bribes, etc.)
    public static void ClearWanted()
    {
        if (WantedSystem.Instance != null)
        {
            WantedSystem.Instance.ClearWantedStatus();
        }
    }
}