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

        // Handle GuardManager (old system)
        if (guardManager != null && !hasTriggeredAggro)
        {
            hasTriggeredAggro = true;
            guardManager.OnDamaged();
        }

        // Handle NPCManager (new unified system)
        // FIXED: Call OnDamaged() instead of RunAwayFromPlayer()
        // OnDamaged() will check enableCombat and decide whether to fight or flee
        if (npcManager != null && !hasTriggeredAggro)
        {
            hasTriggeredAggro = true;
            npcManager.OnDamaged();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}