using UnityEngine;

public enum FireMode
{
    SemiAuto,
    FullAuto,
    Burst
}

[CreateAssetMenu(menuName = "Inventory System/Weapon")]
public class WeaponData : InventoryItemData
{
    [Header("Weapon Stats")]
    public float damage = 30f;
    public float fireRate = 0.1f;
    public float reloadTime = 2.5f;
    public int magazineSize = 30;
    public int bulletSpeed = 50;

    [Header("Fire Settings")]
    public FireMode fireMode = FireMode.SemiAuto;  // <--- Add this

    [Header("Recoil & Accuracy")]
    public float recoilAmount = 1.5f;
    public float spread = 0.05f;

    [Header("3D Model")]
    public GameObject weaponPrefab; // drag your gun prefab here

    [Header("Audio & VFX")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public GameObject muzzleFlashPrefab;
    public float ShootingSoundDelay;

    public override void UseItem()
    {
        Debug.Log($"Firing {Name}!");
    }
}