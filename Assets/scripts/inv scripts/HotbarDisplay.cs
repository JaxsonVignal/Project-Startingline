using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;
using UnityEngine.InputSystem;


public class HotbarDisplay : StaticInventoryDisplay
{
    private int _maxIndexSize = 9;
    private int _currentIndex = 0;

    private bool isFiring;
    private float nextFireTime;
    private int currentAmmo;
    private bool isReloading;

    private GameInput _gameInput;

    [SerializeField] private Transform weaponHolder;
    private GameObject currentWeapon;
    public Transform firePoint; // assign this in the weapon prefab
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

        _gameInput.Player.Hotbar1.performed += Hotbar1;
        _gameInput.Player.Hotbar2.performed += Hotbar2;
        _gameInput.Player.Hotbar3.performed += Hotbar3;
        _gameInput.Player.Hotbar4.performed += Hotbar4;
        _gameInput.Player.Hotbar5.performed += Hotbar5;
        _gameInput.Player.Hotbar6.performed += Hotbar6;
        _gameInput.Player.Hotbar7.performed += Hotbar7;
        _gameInput.Player.Hotbar8.performed += Hotbar8;
        _gameInput.Player.Hotbar9.performed += Hotbar9;
        _gameInput.Player.Hotbar10.performed += Hotbar10;
        _gameInput.Player.Reload.performed += ctx => ReloadCurrentWeapon();
        _gameInput.Player.useItem.performed += UseItem;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        _gameInput.Disable();

        _gameInput.Player.useItem.performed -= StartFiring;
        _gameInput.Player.useItem.canceled -= StopFiring;

        _gameInput.Player.Hotbar1.performed -= Hotbar1;
        _gameInput.Player.Hotbar2.performed -= Hotbar2;
        _gameInput.Player.Hotbar3.performed -= Hotbar3;
        _gameInput.Player.Hotbar4.performed -= Hotbar4;
        _gameInput.Player.Hotbar5.performed -= Hotbar5;
        _gameInput.Player.Hotbar6.performed -= Hotbar6;
        _gameInput.Player.Hotbar7.performed -= Hotbar7;
        _gameInput.Player.Hotbar8.performed -= Hotbar8;
        _gameInput.Player.Hotbar9.performed -= Hotbar9;
        _gameInput.Player.Hotbar10.performed-= Hotbar10;
        _gameInput.Player.Reload.performed -= ctx => ReloadCurrentWeapon();
        _gameInput.Player.useItem.performed -= UseItem;
    }

    private void Hotbar1(InputAction.CallbackContext obj)
    {
        SetIndex(0);
    }

    private void Hotbar2(InputAction.CallbackContext obj)
    {
        SetIndex(1);
    }

    private void Hotbar3(InputAction.CallbackContext obj)
    {
        SetIndex(2);
    }

    private void Hotbar4(InputAction.CallbackContext obj)
    {
        SetIndex(3);
    }

    private void Hotbar5(InputAction.CallbackContext obj)
    {
        SetIndex(4);
    }

    private void Hotbar6(InputAction.CallbackContext obj)
    {
        SetIndex(5);
    }

    private void Hotbar7(InputAction.CallbackContext obj)
    {
        SetIndex(6);
    }

    private void Hotbar8(InputAction.CallbackContext obj)
    {
        SetIndex(7);
    }

    private void Hotbar9(InputAction.CallbackContext obj)
    {
        SetIndex(8);
    }

    private void Hotbar10(InputAction.CallbackContext obj)
    {
        SetIndex(9);
    }

    private void Update()
    {
        if(_gameInput.Player.MouseWheel.ReadValue<float>() > 0.1f)
        {
            ChangeIndex(1);
        }

        if (_gameInput.Player.MouseWheel.ReadValue<float>() < -0.1f)
        {
            ChangeIndex(-1);
        }

        // Handle full auto shooting
        if (isFiring)
        {
            var currentSlot = slots[_currentIndex];
            if (currentSlot.AssignedInventorySlot.ItemData is WeaponData weapon)
            {
                if (Time.time >= nextFireTime)
                {
                    FireWeapon(weapon); 

                    if (weapon.fireMode == FireMode.FullAuto)
                        nextFireTime = Time.time + weapon.fireRate;
                    else
                        isFiring = false; // Semi-auto stops after one shot
                }
            }
        }
    }

    private void UseItem(InputAction.CallbackContext obj)
    {
        var currentSlot = slots[_currentIndex];

        if (currentSlot.AssignedInventorySlot.ItemData == null)
            return;

        InventoryItemData item = currentSlot.AssignedInventorySlot.ItemData;

        // 1) If it's a weapon, handle shooting
        if (item is WeaponData weapon)
        {
            return;
        }
        // 2) Otherwise, just use it normally (consumables, tools, etc.)
        else
        {
            item.UseItem();
        }
    }

    private void FireWeapon(WeaponData weapon)
    {
        if (currentAmmo <= 0)
        {
            Debug.Log("Out of ammo! Must reload before firing.");
            return;
        }

        // Consume ONE round
        currentAmmo--;

        Debug.Log($"PEW PEW! Fired {weapon.Name} | Damage: {weapon.damage}");
        PlayerShooting.Instance.Fire(weapon);

        Debug.Log($"Ammo left: {currentAmmo}");
    }


    private void ChangeIndex(int direction)
    {
        slots[_currentIndex].ToggleHighlight();

        _currentIndex += direction;

        if (_currentIndex > _maxIndexSize) _currentIndex = 0;
        if (_currentIndex < 0) _currentIndex = _maxIndexSize;

        slots[_currentIndex].ToggleHighlight();
    }

    private void SetIndex(int newIndex)
    {
        slots[_currentIndex].ToggleHighlight();

        if (newIndex < 0) _currentIndex = 0;
        else if (newIndex > _maxIndexSize) newIndex = _maxIndexSize;
        else _currentIndex = newIndex;

        slots[_currentIndex].ToggleHighlight();

        // Equip weapon if slot has one
        var item = slots[_currentIndex].AssignedInventorySlot.ItemData;
        if (item is WeaponData weapon)
            EquipWeapon(weapon);
        else
        {
            if (currentWeapon != null)
            {
                Destroy(currentWeapon); // hide weapon if the slot isn’t a gun
            }
        }
    }


    private void StartFiring(InputAction.CallbackContext ctx)
    {
        isFiring = true;
    }

    private void StopFiring(InputAction.CallbackContext ctx)
    {
        isFiring = false;
    }


    private void EquipWeapon(WeaponData weapon)
    {
        // Destroy previous weapon
        if (currentWeapon != null)
            Destroy(currentWeapon);

        if (weapon.weaponPrefab != null)
        {
            Debug.Log("weapon spawned");
            currentWeapon = Instantiate(weapon.weaponPrefab, weaponHolder);
            currentWeapon.transform.localPosition = Vector3.zero;
            currentWeapon.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            // Add follow behaviour
            var follow = currentWeapon.AddComponent<WeaponFollow>();
            follow.cameraTransform = Camera.main.transform;
            follow.smoothSpeed = 10f;
            follow.swayAmount = 0.05f;
            follow.swaySmooth = 4f;

            PlayerShooting.Instance.firePoint = currentWeapon.transform.Find("FirePoint");


            // --- Initialize ammo count ---
            currentAmmo = weapon.magazineSize;
        }
    }

    public void ReloadCurrentWeapon()
    {
        var currentSlot = slots[_currentIndex];
        if (!(currentSlot.AssignedInventorySlot.ItemData is WeaponData weapon)) return;

        if (isReloading || currentAmmo == weapon.magazineSize)
            return; // already reloading or full

        StartCoroutine(ReloadCoroutine(weapon));
    }

    private IEnumerator ReloadCoroutine(WeaponData weapon)
    {
        isReloading = true;
        Debug.Log("Reloading...");

        yield return new WaitForSeconds(weapon.reloadTime);

        currentAmmo = weapon.magazineSize;
        Debug.Log($"Reloaded! Ammo full: {currentAmmo}");

        isReloading = false;
    }
}
