using UnityEngine;

public class Bullet : MonoBehaviour
{
    [HideInInspector] public WeaponData weaponData;
    public float lifetime = 5f;

    private void Start()
    {
        Destroy(gameObject, lifetime);

        // DEBUG: Check if BulletModifier is attached
        BulletModifier modifier = GetComponent<BulletModifier>();
        if (modifier != null)
        {
            Debug.Log("[Bullet] BulletModifier component found on bullet");
        }
        else
        {
            Debug.LogWarning("[Bullet] NO BulletModifier component on bullet! Add it to bullet prefab!");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[Bullet] Hit: {collision.collider.gameObject.name}");

        // Call the BulletModifier FIRST to check if we should ricochet
        BulletModifier bulletModifier = GetComponent<BulletModifier>();
        bool shouldDestroy = true;

        if (bulletModifier != null)
        {
            Debug.Log("[Bullet] Calling BulletModifier.OnBulletHit()");

            // Check if this bullet should ricochet before destroying
            shouldDestroy = bulletModifier.ShouldDestroyOnHit(collision);
            bulletModifier.OnBulletHit(collision);
        }
        else
        {
            Debug.LogWarning("[Bullet] BulletModifier component NOT FOUND!");
        }

        // Only apply damage if hitting a valid target (Enemies OR NPCs, not on ricochet surfaces)
        int enemyLayer = LayerMask.NameToLayer("Enemies");
        int npcLayer = LayerMask.NameToLayer("NPCs");

        if (collision.collider.gameObject.layer == enemyLayer || collision.collider.gameObject.layer == npcLayer)
        {
            EnemyHealth health = collision.collider.GetComponent<EnemyHealth>();
            if (health != null && weaponData != null)
            {
                // Check if bullet has ricochet modifier and apply damage multiplier
                float finalDamage = weaponData.damage;

                if (bulletModifier != null)
                {
                    float damageMultiplier = bulletModifier.GetCurrentDamageMultiplier();
                    finalDamage *= damageMultiplier;

                    if (damageMultiplier < 1f)
                    {
                        Debug.Log($"[Bullet] Applying ricochet damage reduction: {damageMultiplier}x (Final damage: {finalDamage})");
                    }
                }

                health.TakeDamage(finalDamage);
                Debug.Log($"[Bullet] Dealt {finalDamage} damage to {collision.collider.gameObject.name}");
            }
        }

        // Only destroy if we're not ricocheting
        if (shouldDestroy)
        {
            Destroy(gameObject);
        }
    }
}