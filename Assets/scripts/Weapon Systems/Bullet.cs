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

        // Try to apply damage to hit object
        EnemyHealth health = collision.collider.GetComponent<EnemyHealth>();
        if (health != null && weaponData != null)
        {
            health.TakeDamage(weaponData.damage);
            Debug.Log($"[Bullet] Dealt {weaponData.damage} damage to {collision.collider.gameObject.name}");
        }

        // Call the BulletModifier to apply modifier effects
        BulletModifier modifier = GetComponent<BulletModifier>();
        if (modifier != null)
        {
            Debug.Log("[Bullet] Calling BulletModifier.OnBulletHit()");
            modifier.OnBulletHit(collision);
        }
        else
        {
            Debug.LogWarning("[Bullet] BulletModifier component NOT FOUND!");
        }

        Destroy(gameObject);
    }
}