using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public Transform firePoint;  // Where bullets spawn
    public GameObject bulletPrefab; // Bullet prefab
    public Camera playerCamera; // Reference to your camera

    public static PlayerShooting Instance;
    private void Awake() => Instance = this;

    public void Fire(WeaponData weapon)
    {
        if (bulletPrefab && firePoint && playerCamera)
        {
            // Spawn the bullet
            GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

            // Set bullet direction
            Vector3 shootDirection = playerCamera.transform.forward;
            bulletObj.transform.forward = shootDirection;

            // Set bullet stats
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.damage = weapon.damage;
                bullet.speed = weapon.bulletSpeed; // make sure your WeaponData has a bulletSpeed field
            }

            // Apply initial force (optional, for physics bullets)
            Rigidbody rb = bulletObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(shootDirection * 50f, ForceMode.Impulse);
            }
        }

        Debug.Log($"Fired {weapon.Name}!");
    }

}
