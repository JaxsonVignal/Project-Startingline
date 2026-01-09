using UnityEngine;

public enum AttachmentType { Sight, Barrel, Grip, Magazine, Stock, Cosmetic, Underbarrel, SideRail, Modifier }

[CreateAssetMenu(menuName = "Inventory System/Attachment")]
public class AttachmentData : InventoryItemData
{
    [Tooltip("Unique id; keep stable for save/load. Could use asset name.")]
    public string id;
    public AttachmentType type;
    public GameObject prefab;
    public Sprite icon;
    [TextArea] public string description;
    public float Value = 0;

    [Header("Local transform when parented to socket")]
    public Vector3 localPosition;
    public Vector3 localEuler;
    public Vector3 localScale = Vector3.one;

    [Header("Stat modifiers (apply in WeaponAttachmentSystem)")]
    public float damageBonus = 0f;
    public float fireRateMultiplier = 1f;
    public float reloadTimeMultiplier = 1f;
    public float spreadMultiplier = 1f;
    public float recoilMultiplier = 1f;
    public int magazineBonus = 0;

    [Header("Scope Settings (for Sight attachments)")]
    [Tooltip("FOV override for this specific scope (0 = use default scopeFOV)")]
    public float scopeFOVOverride = 0f;

    [Header("Scope Reticle Settings (for Sight attachments)")]
    [Tooltip("Custom reticle sprite to show when aiming with this scope (null = use default crosshair)")]
    public Sprite scopeReticle;
    [Tooltip("Scale of the reticle image when displayed")]
    public float reticleScale = 1f;
    [Tooltip("Color tint for the reticle")]
    public Color reticleColor = Color.red;

    [Header("Scope Minigame Settings (for Sight attachments)")]
    [Tooltip("Local position of front screw relative to scope")]
    public Vector3 frontScrewLocalPos = new Vector3(-0.05f, 0.05f, -0.05f);
    [Tooltip("Local position of back screw relative to scope")]
    public Vector3 backScrewLocalPos = new Vector3(0.05f, 0.05f, -0.05f);
    [Tooltip("Visual size of screw indicators")]
    public float screwRadius = 0.015f;

    [Header("Grenade Launcher Settings (for Underbarrel type)")]
    [Tooltip("If this underbarrel attachment is a grenade launcher, assign the data here")]
    public GrenadeLauncherData grenadeLauncherData;

    [Header("Underbarrel Shotgun Settings (for Underbarrel type)")]
    [Tooltip("If this underbarrel attachment is a shotgun, assign the data here")]
    public UnderbarrelShotgunData underbarrelShotgunData;

    [Header("Flashlight Settings (for SideRail type)")]
    [Tooltip("If this siderail attachment is a flashlight, assign the data here")]
    public FlashlightData flashlightData;

    [Header("Modifier Settings (for Modifier type)")]
    [Tooltip("If this is a modifier attachment, configure behavior changes here")]
    public ModifierData modifierData;
}