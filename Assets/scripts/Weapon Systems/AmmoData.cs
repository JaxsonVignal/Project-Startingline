using UnityEngine;

public enum AmmoType
{
    Pistol_9mm,
    Pistol_45,
    Pistol_50cal,
    Rifle_762,
    Rifle_556,
    Shotgun_Slug,
    Shotgun_Buck,
    Sniper,
    SMG,
    Rockets
    
}

[CreateAssetMenu(menuName = "Inventory System/Ammo")]
public class AmmoData : InventoryItemData
{
    [Header("Ammo Properties")]
    public AmmoType ammoType;

    public override void UseItem()
    {
        Debug.Log($"Using {Name} ammo");
    }
}