using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    private bool hasTriggeredAggro = false;

    private GuardManager guardManager;
    private NPCManager npcManager;

    private void Start()
    {
        currentHealth = maxHealth;
        guardManager = GetComponent<GuardManager>();
        npcManager = GetComponent<NPCManager>();
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Destroy(gameObject);
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
