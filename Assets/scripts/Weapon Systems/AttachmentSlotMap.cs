using UnityEngine;

public class AttachmentSlotMap : MonoBehaviour
{
    public Transform sightSocket;
    public Transform barrelSocket;
    public Transform gripSocket;
    public Transform magazineSocket;
    public Transform stockSocket;
    public Transform underbarrelSocket;
    public Transform sideRailSocket;

    public Transform GetSocket(AttachmentType t)
    {
        switch (t)
        {
            case AttachmentType.Sight: return sightSocket;
            case AttachmentType.Barrel: return barrelSocket;
            case AttachmentType.Grip: return gripSocket;
            case AttachmentType.Magazine: return magazineSocket;
            case AttachmentType.Stock: return stockSocket;
            case AttachmentType.Underbarrel: return underbarrelSocket;
            case AttachmentType.SideRail: return sideRailSocket;
            default: return transform;
        }
    }
}
