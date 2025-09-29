using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarDisplay : StaticInventoryDisplay
{
    private int _maxIndexSize = 9;
    private int _currentIndex = 0;

    private GameInput _gameInput;

    [SerializeField] private Transform weaponHolder;
    private GameObject currentWeapon;

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

        // Weapon fire/reload
        _gameInput.Player.useItem.performed += StartFiring;
        _gameInput.Player.useItem.canceled += StopFiring;
        _gameInput.Player.Reload.performed += ctx => PlayerShooting.Instance.Reload();

        // Hotbar slots
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
    }

    private void Update()
    {
        float scroll = _gameInput.Player.MouseWheel.ReadValue<float>();
        if (scroll > 0.1f) ChangeIndex(1);
        if (scroll < -0.1f) ChangeIndex(-1);
    }

    private void SetIndex(int newIndex)
    {
        slots[_currentIndex].ToggleHighlight();

        _currentIndex = Mathf.Clamp(newIndex, 0, _maxIndexSize);

        slots[_currentIndex].ToggleHighlight();

        var item = slots[_currentIndex].AssignedInventorySlot.ItemData;
        if (item is WeaponData weapon)
            EquipWeapon(weapon);
        else
            UnequipWeapon();
    }

    private void ChangeIndex(int direction)
    {
        int newIndex = _currentIndex + direction;
        if (newIndex > _maxIndexSize) newIndex = 0;
        if (newIndex < 0) newIndex = _maxIndexSize;

        SetIndex(newIndex);
    }

    private void EquipWeapon(WeaponData weapon)
    {
        // Destroy previous weapon
        if (currentWeapon != null) Destroy(currentWeapon);

        if (weapon.weaponPrefab != null)
        {
            currentWeapon = Instantiate(weapon.weaponPrefab, weaponHolder);
            currentWeapon.transform.localPosition = Vector3.zero;
            currentWeapon.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            // Add follow behaviour
            var follow = currentWeapon.AddComponent<WeaponFollow>();
            follow.cameraTransform = Camera.main.transform;
            follow.smoothSpeed = 10f;
            follow.swayAmount = 0.05f;
            follow.swaySmooth = 4f;

            // Set fire point in PlayerShooting
            PlayerShooting.Instance.firePoint = currentWeapon.transform.Find("FirePoint");

            // Equip weapon in PlayerShooting
            PlayerShooting.Instance.EquipWeapon(weapon);
        }
    }

    private void UnequipWeapon()
    {
        if (currentWeapon != null)
            Destroy(currentWeapon);

        PlayerShooting.Instance.EquipWeapon(null);
    }

    private void StartFiring(InputAction.CallbackContext ctx) => PlayerShooting.Instance.StartFiring();
    private void StopFiring(InputAction.CallbackContext ctx) => PlayerShooting.Instance.StopFiring();
}
