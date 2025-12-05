using UnityEngine;

[System.Serializable]
public class WeaponOrder
{
    public string npcName;
    public WeaponData weaponRequested;
    public AttachmentData sightAttachment;
    public AttachmentData underbarrelAttachment;
    public AttachmentData barrelAttachment;
    public AttachmentData magazineAttachment;
    public AttachmentData sideRailAttachment;
    public Transform meetingLocation;
    public float agreedPrice;
    public float pickupTime;
    public bool isPriceSet;
    public bool isAccepted;
    public bool isCompleted;
    public float pickupTimeGameHour;

    public WeaponOrder(string npc, WeaponData weapon, Transform location)
    {
        npcName = npc;
        weaponRequested = weapon;
        meetingLocation = location;
        isPriceSet = false;
        isAccepted = false;
        isCompleted = false;
    }
}
