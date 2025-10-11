using UnityEngine;

[CreateAssetMenu(menuName = "Inventory System/Ammo Box")]
public class AmmoBoxData : InventoryItemData
{
    [Header("Ammo Box Properties")]
    public AmmoData ammoToGive; // The ammo type this box contains
    public int ammoAmount = 30; // How much ammo you get

    public override void UseItem()
    {
        Debug.Log($"Opening {Name}! Converting to {ammoAmount}x {ammoToGive.Name}");
    }
}