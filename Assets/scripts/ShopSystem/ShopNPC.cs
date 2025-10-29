using UnityEngine;

public class ShopNPC : MonoBehaviour
{
    public ShopManager shopManager;
    public KeyCode interactionKey = KeyCode.E;
    private bool playerInRange = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            shopManager.ToggleShop(false);
        }
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactionKey))
        {
            shopManager.ToggleShop(true);
        }

        if (playerInRange && Input.GetKeyDown(KeyCode.Escape))
        {
            shopManager.ToggleShop(false);
        }
    }
}
