using UnityEngine;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;

    [Header("Faction")]
    public Faction faction;
    public int reputationLossPenalty = 50; // How much rep the player loses when killing this enemy


    [HideInInspector]public float currentHealth;

    [Header("Optional Settings")]
    public bool destroyOnDeath = true;
    public AudioClip deathSound;
    public AudioClip hitSound;

    [Header("Events")]
    public UnityEvent onDeath;
    public UnityEvent onHit;

    private AudioSource audioSource;
    private bool isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake = false;
        }
    }

    /// <summary>
    /// Called by Bullet.cs when hit. Applies damage and triggers events.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (hitSound) audioSource.PlayOneShot(hitSound);
        onHit.Invoke();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        FactionReputationSystem.Instance.LoseReputation(faction, reputationLossPenalty);
        Debug.Log($"Enemy from {faction} defeated. Player lost {reputationLossPenalty} reputation.");

        if (deathSound)
            audioSource.PlayOneShot(deathSound);

        onDeath.Invoke();

        // Optionally add animation or effects here
        // e.g., GetComponent<Animator>().SetTrigger("Die");

        if (destroyOnDeath)
            Destroy(gameObject, deathSound ? deathSound.length : 0f);
    }

    /// <summary>
    /// Returns the normalized health (0 to 1).
    /// </summary>
    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }

    /// <summary>
    /// Heals the entity (for pickups or scripts).
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
    }
}
