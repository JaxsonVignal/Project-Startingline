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
            Die();
            return;
        }

        // Only trigger aggro the first time we take damage
        if (!hasTriggeredAggro)
        {
            hasTriggeredAggro = true;

            // Call appropriate manager if it exists
            if (guardManager != null)
                guardManager.OnDamaged();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} died!");
        Destroy(gameObject);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        hasTriggeredAggro = false;
    }
}