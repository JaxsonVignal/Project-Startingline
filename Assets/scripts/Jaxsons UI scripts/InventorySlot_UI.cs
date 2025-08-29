using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlot_UI : MonoBehaviour
{
    [SerializeField] private Image itemSprite;
    [SerializeField] private TextMeshProUGUI itemCount;
    [SerializeField] private InventorySlot assignedInvetorySlot;

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
}
