using UnityEngine;

public enum AttachmentType { Sight, Barrel, Grip, Magazine, Stock, Cosmetic, Underbarrel,SideRail}

[CreateAssetMenu(menuName = "Inventory System/Attachment")]
public class AttachmentData : ScriptableObject
{
    [Tooltip("Unique id; keep stable for save/load. Could use asset name.")]
    public string id;

    public AttachmentType type;
    public GameObject prefab; // visual/model to instantiate when equipped
    public Sprite icon;
    [TextArea] public string description;

    [Header("Local transform when parented to socket")]
    public Vector3 localPosition;
    public Vector3 localEuler;
    public Vector3 localScale = Vector3.one;

    [Header("Stat modifiers (apply in WeaponAttachmentSystem)")]
    public float damageBonus = 0f;          // additive
    public float fireRateMultiplier = 1f;  // multiplies base fireRate
    public float reloadTimeMultiplier = 1f;
    public float spreadMultiplier = 1f;
    public float recoilMultiplier = 1f;
    public int magazineBonus = 0;          // add to mag size

    [Header("Scope ADS Modifiers (for Sight attachments)")]
    [Tooltip("Offset to apply to scopeAdsPosition when this scope is equipped")]
    public Vector3 scopePositionOffset = Vector3.zero;
    [Tooltip("Rotation offset to apply to scopeAdsPosition when this scope is equipped")]
    public Vector3 scopeRotationOffset = Vector3.zero;
    [Tooltip("FOV override for this specific scope (0 = use default scopeFOV)")]
    public float scopeFOVOverride = 0f;
}