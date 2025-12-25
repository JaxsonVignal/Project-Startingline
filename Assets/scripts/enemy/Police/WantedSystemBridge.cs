using UnityEngine;

/// <summary>
/// Bridges your existing PlayerMovement.isWanted with the new WantedSystem
/// Attach this to your Player GameObject
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class WantedSystemBridge : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private bool lastWantedState = false;

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();

        if (playerMovement == null)
        {
            Debug.LogError("WantedSystemBridge: PlayerMovement component not found!");
            enabled = false;
            return;
        }

        if (WantedSystem.Instance == null)
        {
            Debug.LogError("WantedSystemBridge: WantedSystem.Instance not found! Make sure WantedSystem exists in scene.");
            enabled = false;
            return;
        }

        Debug.Log("WantedSystemBridge: Successfully connected PlayerMovement with WantedSystem");
    }

    private void Update()
    {
        if (playerMovement == null || WantedSystem.Instance == null) return;

        // Check if wanted state changed
        if (playerMovement.isWanted != lastWantedState)
        {
            // Sync the wanted state to WantedSystem
            WantedSystem.Instance.SetWanted(playerMovement.isWanted);

            lastWantedState = playerMovement.isWanted;

            if (playerMovement.isWanted)
            {
                Debug.Log("WantedSystemBridge: Player became wanted!");
            }
            else
            {
                Debug.Log("WantedSystemBridge: Player is no longer wanted!");
            }
        }
    }
}