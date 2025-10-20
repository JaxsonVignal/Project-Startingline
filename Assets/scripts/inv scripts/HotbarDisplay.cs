using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarDisplay : StaticInventoryDisplay
{
    private int _maxIndexSize = 9;
    private int _currentIndex = 0;

    private GameInput _gameInput;

    [SerializeField] private Transform weaponHolder;
    private GameObject currentWeapon;

    private bool isAiming = false;

    private void Awake()
    {
        _gameInput = new GameInput();
    }

    public override void Start()
    {
        base.Start();
        _currentIndex = 0;
        _maxIndexSize = slots.Length - 1;
        slots[_currentIndex].ToggleHighlight();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _gameInput.Enable();

        _gameInput.Player.useItem.performed += StartFiring;
        _gameInput.Player.useItem.canceled += StopFiring;
        _gameInput.Player.Reload.performed += ctx => PlayerShooting.Instance.Reload();

        _gameInput.Player.Hotbar1.performed += ctx => SetIndex(0);
        _gameInput.Player.Hotbar2.performed += ctx => SetIndex(1);
        _gameInput.Player.Hotbar3.performed += ctx => SetIndex(2);
        _gameInput.Player.Hotbar4.performed += ctx => SetIndex(3);
        _gameInput.Player.Hotbar5.performed += ctx => SetIndex(4);
        _gameInput.Player.Hotbar6.performed += ctx => SetIndex(5);
        _gameInput.Player.Hotbar7.performed += ctx => SetIndex(6);
        _gameInput.Player.Hotbar8.performed += ctx => SetIndex(7);
        _gameInput.Player.Hotbar9.performed += ctx => SetIndex(8);
        _gameInput.Player.Hotbar10.performed += ctx => SetIndex(9);

        // NEW: Track aiming state
        _gameInput.Player.Aim.performed += ctx => isAiming = true;
        _gameInput.Player.Aim.canceled += ctx => isAiming = false;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _gameInput.Disable();

        _gameInput.Player.useItem.performed -= StartFiring;
        _gameInput.Player.useItem.canceled -= StopFiring;
        _gameInput.Player.Reload.performed -= ctx => PlayerShooting.Instance.Reload();

        _gameInput.Player.Hotbar1.performed -= ctx => SetIndex(0);
        _gameInput.Player.Hotbar2.performed -= ctx => SetIndex(1);
        _gameInput.Player.Hotbar3.performed -= ctx => SetIndex(2);
        _gameInput.Player.Hotbar4.performed -= ctx => SetIndex(3);
        _gameInput.Player.Hotbar5.performed -= ctx => SetIndex(4);
        _gameInput.Player.Hotbar6.performed -= ctx => SetIndex(5);
        _gameInput.Player.Hotbar7.performed -= ctx => SetIndex(6);
        _gameInput.Player.Hotbar8.performed -= ctx => SetIndex(7);
        _gameInput.Player.Hotbar9.performed -= ctx => SetIndex(8);
        _gameInput.Player.Hotbar10.performed -= ctx => SetIndex(9);

        // NEW: Unsubscribe from aim events
        _gameInput.Player.Aim.performed -= ctx => isAiming = true;
        _gameInput.Player.Aim.canceled -= ctx => isAiming = false;
    }

    private void Update()
    {
        float scroll = _gameInput.Player.MouseWheel.ReadValue<float>();
        if (scroll > 0.1f) ChangeIndex(1);
        if (scroll < -0.1f) ChangeIndex(-1);
    }

    private void SetIndex(int newIndex)
    {
        // NEW: Prevent switching while aiming
        if (isAiming)
        {
            Debug.Log("Cannot switch items while aiming!");
            return;
        }

        slots[_currentIndex].ToggleHighlight();

        _currentIndex = Mathf.Clamp(newIndex, 0, _maxIndexSize);

        slots[_currentIndex].ToggleHighlight();

        var slot = slots[_currentIndex].AssignedInventorySlot;
        if (slot.ItemData is WeaponData weapon)
            EquipWeapon(weapon, slot.UniqueSlotID);
        else
            UnequipWeapon();
    }

    private void ChangeIndex(int direction)
    {
        // NEW: Prevent switching while aiming
        if (isAiming)
            return;

        int newIndex = _currentIndex + direction;
        if (newIndex > _maxIndexSize) newIndex = 0;
        if (newIndex < 0) newIndex = _maxIndexSize;

        SetIndex(newIndex);
    }

    private void EquipWeapon(WeaponData weapon, string slotID)
    {
        if (currentWeapon != null) Destroy(currentWeapon);

        if (weapon.weaponPrefab != null)
        {
            currentWeapon = Instantiate(weapon.weaponPrefab, weaponHolder);
            currentWeapon.transform.localPosition = Vector3.zero;
            currentWeapon.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            var follow = currentWeapon.AddComponent<WeaponFollow>();
            follow.cameraTransform = Camera.main.transform;
            follow.smoothSpeed = 10f;
            follow.swayAmount = 0f;
            follow.swaySmooth = 4f;

            PlayerShooting.Instance.firePoint = currentWeapon.transform.Find("FirePoint");
            PlayerShooting.Instance.EquipWeapon(weapon, slotID);

            // Attach ADS system
            var ads = currentWeapon.AddComponent<ADS>();
            ads.weaponRoot = currentWeapon.transform;
            ads.playerCamera = Camera.main;
            ads.hipPosition = currentWeapon.transform.Find("HipPosition");
            ads.adsPosition = currentWeapon.transform.Find("ADSPosition");
            ads.scopeAdsPosition = currentWeapon.transform.Find("ScopeAdsPosition");

            // Apply attachments from WeaponInstance if available
            var attachSys = ApplyStoredAttachments(currentWeapon, slotID);

            // Set the attachment system on PlayerShooting to apply modifiers
            if (attachSys != null)
            {
                PlayerShooting.Instance.SetAttachmentSystem(attachSys);
                Debug.Log($"Attachment system set on PlayerShooting - modifiers now active");
            }
        }
    }

    private WeaponAttachmentSystem ApplyStoredAttachments(GameObject weaponObject, string slotID)
    {
        // Get the WeaponInstance stored for this slot
        WeaponInstance storedInstance = WeaponInstanceStorage.GetInstance(slotID);
        if (storedInstance == null || storedInstance.attachments.Count == 0)
        {
            Debug.Log($"No stored attachments found for slot {slotID}");
            return null; // No stored attachments
        }

        // Ensure attachment system exists
        var attachSys = weaponObject.GetComponent<WeaponAttachmentSystem>();
        if (attachSys == null)
        {
            attachSys = weaponObject.AddComponent<WeaponAttachmentSystem>();
        }

        // Get the weapon data from the equipped weapon's slot
        var slot = FindSlotByID(slotID);
        if (slot != null && slot.ItemData is WeaponData weaponData)
        {
            attachSys.weaponData = weaponData;
        }

        // Build attachment lookup from Resources
        var allAttachments = Resources.LoadAll<AttachmentData>("Attachments");
        var attachmentLookup = new Dictionary<string, AttachmentData>();
        foreach (var att in allAttachments)
        {
            if (att != null && !string.IsNullOrEmpty(att.id))
                attachmentLookup[att.id] = att;
        }

        // Apply each attachment
        foreach (var entry in storedInstance.attachments)
        {
            if (attachmentLookup.TryGetValue(entry.attachmentId, out var attData))
            {
                attachSys.EquipAttachment(attData, entry);
                Debug.Log($"Applied attachment: {attData.id} to equipped weapon");
            }
        }

        Debug.Log($"Successfully applied {storedInstance.attachments.Count} attachments to equipped weapon");

        return attachSys; // Return the attachment system
    }

    private InventorySlot FindSlotByID(string slotID)
    {
        var playerInv = FindObjectOfType<PlayerInventoryHolder>();
        if (playerInv != null)
        {
            foreach (var slot in playerInv.PrimaryInventorySystem.InventorySlots)
            {
                if (slot.UniqueSlotID == slotID)
                    return slot;
            }
        }
        return null;
    }

    private void UnequipWeapon()
    {
        if (currentWeapon != null)
            Destroy(currentWeapon);

        PlayerShooting.Instance.EquipWeapon(null, null);
    }

    private void StartFiring(InputAction.CallbackContext ctx) => PlayerShooting.Instance.StartFiring();
    private void StopFiring(InputAction.CallbackContext ctx) => PlayerShooting.Instance.StopFiring();
}