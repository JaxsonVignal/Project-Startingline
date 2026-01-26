using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarDisplay : StaticInventoryDisplay
{
    private int _maxIndexSize = 9;
    private int _currentIndex = 0;

    private GameInput _gameInput;

    [SerializeField] private Transform weaponHolder;
    [SerializeField] private Database itemDatabase;
    private GameObject currentWeapon;

    private bool isAiming = false;

    private void Awake()
    {
        _gameInput = new GameInput();
    }

    private void Start()
    {
        _currentIndex = 0;
        _maxIndexSize = slots.Length - 1;
    }


    private void OnEnable()
    {
        _gameInput.Enable();

        _gameInput.Player.useItem.performed += StartFiring;
        _gameInput.Player.useItem.canceled += StopFiring;
        _gameInput.Player.Reload.performed += _ => PlayerShooting.Instance.Reload();

        _gameInput.Player.Hotbar1.performed += _ => SetIndex(0);
        _gameInput.Player.Hotbar2.performed += _ => SetIndex(1);
        _gameInput.Player.Hotbar3.performed += _ => SetIndex(2);
        _gameInput.Player.Hotbar4.performed += _ => SetIndex(3);
        _gameInput.Player.Hotbar5.performed += _ => SetIndex(4);
        _gameInput.Player.Hotbar6.performed += _ => SetIndex(5);
        _gameInput.Player.Hotbar7.performed += _ => SetIndex(6);
        _gameInput.Player.Hotbar8.performed += _ => SetIndex(7);
        _gameInput.Player.Hotbar9.performed += _ => SetIndex(8);
        _gameInput.Player.Hotbar10.performed += _ => SetIndex(9);

        _gameInput.Player.Aim.performed += _ => isAiming = true;
        _gameInput.Player.Aim.canceled += _ => isAiming = false;
    }

    private void OnDisable()
    {
        _gameInput.Disable();

        _gameInput.Player.useItem.performed -= StartFiring;
        _gameInput.Player.useItem.canceled -= StopFiring;
        _gameInput.Player.Reload.performed -= _ => PlayerShooting.Instance.Reload();

        _gameInput.Player.Aim.performed -= _ => isAiming = true;
        _gameInput.Player.Aim.canceled -= _ => isAiming = false;
    }

    private void Update()
    {
        float scroll = _gameInput.Player.MouseWheel.ReadValue<float>();
        if (scroll > 0.1f) ChangeIndex(1);
        if (scroll < -0.1f) ChangeIndex(-1);
    }

    // --------------------------------------------------
    // HOTBAR LOGIC
    // --------------------------------------------------

    private void SetIndex(int newIndex)
    {
        if (isAiming)
            return;

        if (newIndex == _currentIndex)
            return;

        _currentIndex = Mathf.Clamp(newIndex, 0, _maxIndexSize);

        ApplyHighlight();

        var slot = slots[_currentIndex].AssignedInventorySlot;
        if (slot.ItemData is WeaponData weapon)
            EquipWeapon(weapon, slot.UniqueSlotID);
        else
            UnequipWeapon();
    }


    private void ChangeIndex(int direction)
    {
        if (isAiming)
            return;

        int newIndex = _currentIndex + direction;
        if (newIndex > _maxIndexSize) newIndex = 0;
        if (newIndex < 0) newIndex = _maxIndexSize;

        SetIndex(newIndex);
    }

    // --------------------------------------------------
    // WEAPONS
    // --------------------------------------------------

    private void EquipWeapon(WeaponData weapon, string slotID)
    {
        if (currentWeapon != null)
            Destroy(currentWeapon);

        if (weapon.weaponPrefab == null)
            return;

        currentWeapon = Instantiate(weapon.weaponPrefab, weaponHolder);
        currentWeapon.transform.localPosition = Vector3.zero;
        currentWeapon.transform.localRotation = Quaternion.identity;

        PlayerShooting.Instance.firePoint = currentWeapon.transform.Find("FirePoint");
        PlayerShooting.Instance.EquipWeapon(weapon, slotID);

        var ads = currentWeapon.AddComponent<ADS>();
        ads.weaponRoot = currentWeapon.transform;
        ads.playerCamera = Camera.main;
        ads.hipPosition = currentWeapon.transform.Find("HipPosition");
        ads.adsPosition = currentWeapon.transform.Find("ADSPosition");
        ads.scopeAdsPosition = currentWeapon.transform.Find("ScopeAdsPosition");

        var attachSys = ApplyStoredAttachments(currentWeapon, slotID, weapon);
        if (attachSys != null)
        {
            PlayerShooting.Instance.SetAttachmentSystem(attachSys);
            ads.SetAttachmentSystem(attachSys);
        }

        ads.SetWeaponData(weapon);
    }

    private WeaponAttachmentSystem ApplyStoredAttachments(GameObject weaponObject, string slotID, WeaponData weaponData)
    {
        WeaponInstance storedInstance = WeaponInstanceStorage.GetInstance(slotID);
        if (storedInstance == null || storedInstance.attachments.Count == 0)
            return null;

        if (itemDatabase == null)
            return null;

        var runtime = weaponObject.GetComponent<WeaponRuntime>() ??
                      weaponObject.AddComponent<WeaponRuntime>();

        runtime.InitFromInstance(
            storedInstance,
            weaponData,
            itemDatabase.GetAttachmentLookup()
        );

        return runtime.attachmentSystem;
    }

    public void UnequipWeapon()
    {
        if (currentWeapon != null)
        {
            Destroy(currentWeapon);
            currentWeapon = null;
        }

        PlayerShooting.Instance.EquipWeapon(null, null);
    }

    // --------------------------------------------------
    // INPUT
    // --------------------------------------------------

    private void StartFiring(InputAction.CallbackContext _) =>
        PlayerShooting.Instance.StartFiring();

    private void StopFiring(InputAction.CallbackContext _) =>
        PlayerShooting.Instance.StopFiring();

    private void RefreshHighlight()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].SetHighlight(i == _currentIndex);
        }
    }

    protected override void AfterSlotsAssigned()
    {
        ApplyHighlight();
    }


    private void ApplyHighlight()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].SetHighlight(i == _currentIndex);
        }
    }

}
