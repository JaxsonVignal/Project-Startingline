using UnityEngine;

public enum AmmoType
{
    Pistol,
    Rifle,
    Shotgun,
    Sniper,
    SMG,
    
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