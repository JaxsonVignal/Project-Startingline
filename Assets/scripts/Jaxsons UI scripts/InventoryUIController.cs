using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
public class InventoryUIController : MonoBehaviour
{
    [FormerlySerializedAs (oldName: "chestPanel")]public DynamicInventoryDisplay inventoryPanel;
    public DynamicInventoryDisplay PlayerBackpackPanel;

    private void Awake()
    {
        inventoryPanel.gameObject.SetActive(false);
        PlayerBackpackPanel.gameObject.SetActive(false);
    }
    private void OnEnable()
    {
        InventoryHolder.OnDynamicInventoryDisplayRequested += DisplayInventory;
        PlayerInventoryHolder.OnPlayerInventoryDisplayRequested += DisplayPlayerInventory;
    }

    private void OnDisable()
    {
        InventoryHolder.OnDynamicInventoryDisplayRequested -= DisplayInventory;
        PlayerInventoryHolder.OnPlayerInventoryDisplayRequested -= DisplayPlayerInventory;
    }

    // Update is called once per frame
    void Update()
    {
       

        if (inventoryPanel.gameObject.activeInHierarchy && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            inventoryPanel.gameObject.SetActive(false);
        }

        if (PlayerBackpackPanel.gameObject.activeInHierarchy && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            PlayerBackpackPanel.gameObject.SetActive(false);
        }

    }

    void DisplayInventory(InventorySystem invToDisplay, int offset)
    {
        inventoryPanel.gameObject.SetActive(true);
        inventoryPanel.RefreshDynamicInventory(invToDisplay, offset);
    }

    void DisplayPlayerInventory(InventorySystem invToDisplay, int offset)
    {
        PlayerBackpackPanel.gameObject.SetActive(true);
        PlayerBackpackPanel.RefreshDynamicInventory(invToDisplay, offset);
    }



}
