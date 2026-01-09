using UnityEngine;

public class Bullet : MonoBehaviour
{
    [HideInInspector] public WeaponData weaponData;
    public float lifetime = 5f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Try to apply damage to hit object
        EnemyHealth health = collision.collider.GetComponent<EnemyHealth>();
        if (health != null && weaponData != null)
        {
            health.TakeDamage(weaponData.damage);
        }

        // === ADD THIS SECTION ===
        // Call the BulletModifier to apply modifier effects
        BulletModifier modifier = GetComponent<BulletModifier>();
        if (modifier != null)
        {
            modifier.OnBulletHit(collision);
        }

        Destroy(gameObject);
    }


}
