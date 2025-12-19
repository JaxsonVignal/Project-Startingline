using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Player Reference")]
    public PlayerMovement playerMovement;

    private bool hasTriggeredAggro = false;
    private GuardManager guardManager;
    private NPCManager npcManager;

    private void Start()
    {
        currentHealth = maxHealth;
        guardManager = GetComponent<GuardManager>();
        npcManager = GetComponent<NPCManager>();

        // Auto-find PlayerMovement if not assigned
        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerMovement>();
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        // Set player as wanted when enemy takes damage
        if (playerMovement != null)
        {
            playerMovement.isWanted = true;
        }

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (guardManager != null && !hasTriggeredAggro)
        {
            hasTriggeredAggro = true;
            guardManager.OnDamaged();
        }

        if (npcManager != null)
        {
            npcManager.RunAwayFromPlayer();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}