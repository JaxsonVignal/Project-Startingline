using UnityEngine;

public class RocketProjectile : MonoBehaviour
{
    public WeaponData weaponData;
    private bool hasExploded = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        hasExploded = true;
        Explode(collision.contacts[0].point);
    }

    private void Explode(Vector3 explosionPoint)
    {
        if (weaponData == null)
        {
            Debug.LogWarning("RocketProjectile has no WeaponData assigned!");
            Destroy(gameObject);
            return;
        }

        // Spawn explosion effect
        if (weaponData.explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(weaponData.explosionEffectPrefab, explosionPoint, Quaternion.identity);
            Destroy(effect, 5f); // Clean up after 5 seconds
        }

        // Play explosion sound
        if (weaponData.explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(weaponData.explosionSound, explosionPoint);
        }

        // Deal damage in radius
        float damage = weaponData.explosionDamage > 0 ? weaponData.explosionDamage : weaponData.damage;
        Collider[] hitColliders = Physics.OverlapSphere(explosionPoint, weaponData.explosionRadius);

        foreach (Collider hit in hitColliders)
        {
            // Check for enemy or damageable objects
            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                // Calculate distance-based damage falloff
                float distance = Vector3.Distance(explosionPoint, hit.transform.position);
                float damageMultiplier = 1f - (distance / weaponData.explosionRadius);
                float finalDamage = damage * Mathf.Clamp01(damageMultiplier);

                enemy.TakeDamage(finalDamage);
                Debug.Log($"Rocket hit {hit.name} for {finalDamage} damage (distance: {distance}m)");
            }

            // Optional: Apply physics force to nearby rigidbodies
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.AddExplosionForce(500f, explosionPoint, weaponData.explosionRadius, 1f, ForceMode.Impulse);
            }
        }

        // Destroy the rocket
        Destroy(gameObject);
    }

    // Optional: Visualize explosion radius in editor
    private void OnDrawGizmosSelected()
    {
        if (weaponData != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, weaponData.explosionRadius);
        }
    }
}