using UnityEngine;

[CreateAssetMenu(menuName = "Inventory System/Grenade Launcher")]
public class GrenadeLauncherData : ScriptableObject
{
    [Header("Grenade Launcher Info")]
    public string launcherName = "M203 Grenade Launcher";
    public string launcherId = "gl_m203";

    [Header("Grenade Stats")]
    public float damage = 150f;
    public float explosionRadius = 8f;
    public int magazineSize = 1;
    public float reloadTime = 3f;
    public float fireRate = 1f;
    public int grenadeSpeed = 30;

    [Header("Ammo Configuration")]
    public AmmoType requiredAmmoType = AmmoType.Grenade40mm;

    [Header("Projectile & Effects")]
    public GameObject grenadePrefab;
    public GameObject explosionEffectPrefab;
    public AudioClip launchSound;
    public AudioClip reloadSound;
    public AudioClip explosionSound;
    public GameObject muzzleFlashPrefab;

    [Header("Recoil")]
    public float recoilX = 3f;
    public float recoilY = 2f;
    public float recoilZ = 2f;
}