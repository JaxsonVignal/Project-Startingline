using UnityEngine;

public class Bullet : MonoBehaviour
{
    [HideInInspector] public WeaponData weaponData;
    public float lifetime = 5f;

    [Header("Hit Effects")]
    [Tooltip("Default hit effect prefab (sparks, impact, etc.)")]
    public GameObject defaultHitEffectPrefab;
    [Tooltip("Hit effect when hitting enemies/NPCs")]
    public GameObject fleshHitEffectPrefab;
    [Tooltip("How long before the hit effect destroys itself")]
    public float hitEffectLifetime = 2f;

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
        bool hitLivingTarget = false;

        if (collision.collider.gameObject.layer == enemyLayer || collision.collider.gameObject.layer == npcLayer)
        {
            hitLivingTarget = true;
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

        // Spawn appropriate hit effect
        SpawnHitEffect(collision, hitLivingTarget);

        // Only destroy if we're not ricocheting
        if (shouldDestroy)
        {
            Destroy(gameObject);
        }
    }

    private void SpawnHitEffect(Collision collision, bool hitLivingTarget)
    {
        // Determine which effect to use
        GameObject effectPrefab = hitLivingTarget ? fleshHitEffectPrefab : defaultHitEffectPrefab;

        if (effectPrefab == null)
        {
            Debug.LogWarning($"[Bullet] No hit effect prefab assigned! (hitLivingTarget: {hitLivingTarget})");
            return;
        }

        // Get hit point and normal from collision
        ContactPoint contact = collision.contacts[0];
        Vector3 hitPoint = contact.point;
        Vector3 hitNormal = contact.normal;

        // Create rotation that faces away from the surface
        Quaternion hitRotation = Quaternion.LookRotation(hitNormal);

        // Spawn the effect
        GameObject hitEffect = Instantiate(effectPrefab, hitPoint, hitRotation);
        hitEffect.transform.localScale *= 0.25f;

        // Destroy the effect after a short time
        Destroy(hitEffect, hitEffectLifetime);

        Debug.Log($"[Bullet] HIT EFFECT SPAWNED at {hitPoint} (ADS active: {FindObjectOfType<ADS>()?.GetComponent<ADS>() != null})");
    }
}