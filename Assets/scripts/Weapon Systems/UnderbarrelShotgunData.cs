using UnityEngine;

[CreateAssetMenu(menuName = "Inventory System/Underbarrel Shotgun")]
public class UnderbarrelShotgunData : ScriptableObject
{
    [Header("Shotgun Info")]
    public string shotgunName = "Masterkey Underbarrel Shotgun";
    public string shotgunId = "ubs_masterkey";

    [Header("Shotgun Stats")]
    public float damage = 25f;
    public int pelletsPerShot = 8;
    public float spreadAngle = 15f;
    public int magazineSize = 4;
    public float reloadTime = 2f;
    public float fireRate = 0.8f;
    public int bulletSpeed = 50;

    [Header("Ammo Configuration")]
    public AmmoType requiredAmmoType = AmmoType.Shotgun_Buck;

    [Header("Projectile & Effects")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public GameObject muzzleFlashPrefab;

    [Header("Recoil")]
    public float recoilX = 4f;
    public float recoilY = 3f;
    public float recoilZ = 3f;
}