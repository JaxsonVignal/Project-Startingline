using UnityEngine;

public class GrenadeProjectile : MonoBehaviour
{
    public GrenadeLauncherData grenadeLauncherData;
    public float lifetime = 10f;

    private bool hasExploded = false;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        Explode();
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        if (grenadeLauncherData == null)
        {
            Debug.LogWarning("GrenadeProjectile has no GrenadeLauncherData assigned!");
            Destroy(gameObject);
            return;
        }

        // Spawn explosion effect
        if (grenadeLauncherData.explosionEffectPrefab != null)
        {
            Instantiate(grenadeLauncherData.explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // Play explosion sound
        if (grenadeLauncherData.explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(grenadeLauncherData.explosionSound, transform.position);
        }

        // Apply explosion damage
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, grenadeLauncherData.explosionRadius);

        foreach (var hitCollider in hitColliders)
        {
            // Check for enemy
            EnemyHealth enemyHealth = hitCollider.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                // Calculate damage based on distance
                float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                float damageMultiplier = 1f - (distance / grenadeLauncherData.explosionRadius);
                float actualDamage = grenadeLauncherData.damage * damageMultiplier;

                enemyHealth.TakeDamage(actualDamage);
                Debug.Log($"Grenade hit {hitCollider.name} for {actualDamage} damage");
            }

            // Apply physics force
            Rigidbody rb = hitCollider.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                Vector3 direction = (hitCollider.transform.position - transform.position).normalized;
                float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                float forceMultiplier = 1f - (distance / grenadeLauncherData.explosionRadius);
                rb.AddForce(direction * 500f * forceMultiplier, ForceMode.Impulse);
            }
        }

        // Destroy the grenade
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (grenadeLauncherData != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, grenadeLauncherData.explosionRadius);
        }
    }
}