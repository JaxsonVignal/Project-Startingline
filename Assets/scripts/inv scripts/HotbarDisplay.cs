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

                    // Only advance nextFireTime if the weapon is full-auto
                    if (weapon.fireMode == FireMode.FullAuto)
                    {
                        nextFireTime = Time.time + weapon.fireRate;
                    }
                    else
                    {
                        // Semi-auto fires only once per button press
                        isFiring = false;
                    }
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
            FireWeapon(weapon);
        }
        // 2) Otherwise, just use it normally (consumables, tools, etc.)
        else
        {
            item.UseItem();
        }
    }

    private void FireWeapon(WeaponData weapon)
    {
        Debug.Log($"PEW PEW! Fired {weapon.Name} | Damage: {weapon.damage}");

        //call shooting script from player

        PlayerShooting.Instance.Fire(weapon);

        if (weapon.muzzleFlashPrefab != null)
        {
            // Example: Spawn muzzle flash at your weapon's fire point
            // Instantiate(weapon.muzzleFlashPrefab, firePoint.position, firePoint.rotation);
        }

        if (weapon.shootSound != null)
        {
            // Example: Play sound
            // AudioSource.PlayClipAtPoint(weapon.shootSound, transform.position);
        }
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
            // Spawn new weapon prefab
            Debug.Log("weapon spawned");
            currentWeapon = Instantiate(weapon.weaponPrefab, weaponHolder);
            currentWeapon.transform.localPosition = Vector3.zero;
            currentWeapon.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);


            var follow = currentWeapon.AddComponent<WeaponFollow>();
            follow.cameraTransform = Camera.main.transform;
            follow.smoothSpeed = 10f;
            follow.swayAmount = 0.05f;
            follow.swaySmooth = 4f;

            PlayerShooting.Instance.firePoint = currentWeapon.transform.Find("FirePoint");
        }
    }
}
