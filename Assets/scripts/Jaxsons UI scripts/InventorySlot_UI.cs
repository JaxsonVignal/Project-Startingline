using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlot_UI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image itemSprite;
    [SerializeField] private TextMeshProUGUI itemCount;
    [SerializeField] private InventorySlot assignedInvetorySlot;

    [SerializeField] private GameObject _slotHighlight;

    private Button Button;

    public InventorySlot AssignedInventorySlot => assignedInvetorySlot;
    public InventoryDisplay ParentDisplay{get; private set;}

    private void Awake()
    {
        clearSlot();
        Button = GetComponent<Button>();
        Button?.onClick.AddListener(onUISlotClick);

        ParentDisplay = transform.parent.GetComponent<InventoryDisplay>();
    }

    public void onUISlotClick()
    {
        ParentDisplay?.SlotClicked(this);
    }

    public void clearSlot()
    {
        assignedInvetorySlot?.ClearSlot();
        itemSprite.sprite = null;
        itemSprite.color = Color.clear;
        itemCount.text = "";
    }

    public void init(InventorySlot slot)
    {
       assignedInvetorySlot = slot;
        UpdateUISlot(slot); 
    }

    public void ToggleHighlight()
    {
        _slotHighlight.SetActive(!_slotHighlight.activeInHierarchy);
    }

    public void UpdateUISlot(InventorySlot slot)
    {
        if(slot.ItemData != null)
        {
            itemSprite.sprite = slot.ItemData.Icon;
            itemSprite.color = Color.white;
            if (slot.StackSize > 1) itemCount.text = slot.StackSize.ToString();
            else itemCount.text = "";
        }
        else
        {
            clearSlot();
        }

    }

    public void UpdateUISlot()
    {
        if(assignedInvetorySlot != null)
        {
            UpdateUISlot(AssignedInventorySlot);
        }
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        // Right click = button 1
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            HandleRightClick();
        }
    }

    private void HandleRightClick()
    {
        // Check if this slot has an ammo box
        if (AssignedInventorySlot?.ItemData is AmmoBoxData ammoBox)
        {
            OpenAmmoBox(ammoBox);
        }
        // You can add other right-click behaviors here for different item types
        else if (AssignedInventorySlot?.ItemData != null)
        {
            // Default behavior - just use the item
            AssignedInventorySlot.ItemData.UseItem();
        }
    }

    private void OpenAmmoBox(AmmoBoxData ammoBox)
    {
        // Get the player inventory
        var playerInventory = FindObjectOfType<PlayerInventoryHolder>();
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventoryHolder not found!");
            return;
        }

        // Try to add the ammo to inventory
        if (playerInventory.PrimaryInventorySystem.AddToInventory(ammoBox.ammoToGive, ammoBox.ammoAmount))
        {
            Debug.Log($"Opened {ammoBox.Name}! Added {ammoBox.ammoAmount}x {ammoBox.ammoToGive.Name} to inventory.");

            // Remove the ammo box from inventory (consume it)
            AssignedInventorySlot.RemoveFromStack(1);

            // If stack is empty, clear the slot
            if (AssignedInventorySlot.StackSize <= 0)
            {
                AssignedInventorySlot.ClearSlot();
            }

            // Update the UI
            UpdateUISlot();
        }
        else
        {
            Debug.Log("Inventory is full! Cannot open ammo box.");
        }
    }
}
