using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlot_UI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image itemSprite;
    [SerializeField] private TextMeshProUGUI itemCount;
    [SerializeField] private GameObject slotHighlight;

    private InventorySlot assignedInventorySlot;
    private Button button;

    public InventorySlot AssignedInventorySlot => assignedInventorySlot;
    public InventoryDisplay ParentDisplay { get; private set; }

    private void Awake()
    {
        button = GetComponent<Button>();
        button?.onClick.AddListener(OnUISlotClick);

        ParentDisplay = transform.parent.GetComponent<InventoryDisplay>();

        ClearVisuals();
    }

    // --------------------------------------------------
    // INITIALIZATION
    // --------------------------------------------------

    public void Init(InventorySlot slot)
    {
        // Unsubscribe from old slot
        if (assignedInventorySlot != null)
            assignedInventorySlot.OnSlotChanged -= Refresh;

        assignedInventorySlot = slot;

        if (assignedInventorySlot != null)
            assignedInventorySlot.OnSlotChanged += Refresh;

        Refresh();
    }

    // --------------------------------------------------
    // UI UPDATE (READ-ONLY)
    // --------------------------------------------------

    private void Refresh()
    {
        if (assignedInventorySlot == null ||
            assignedInventorySlot.ItemData == null ||
            assignedInventorySlot.StackSize <= 0)
        {
            ClearVisuals();
            return;
        }

        itemSprite.enabled = true;
        itemSprite.sprite = assignedInventorySlot.ItemData.Icon;
        itemSprite.color = Color.white;

        itemCount.text = assignedInventorySlot.StackSize > 1
            ? assignedInventorySlot.StackSize.ToString()
            : "";
    }

    private void ClearVisuals()
    {
        itemSprite.sprite = null;
        itemSprite.enabled = false;
        itemSprite.color = Color.white;
        itemCount.text = "";
    }

    // --------------------------------------------------
    // INPUT
    // --------------------------------------------------

    private void OnUISlotClick()
    {
        ParentDisplay?.SlotClicked(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            HandleRightClick();
    }

    private void HandleRightClick()
    {
        if (assignedInventorySlot?.ItemData is AmmoBoxData ammoBox)
        {
            OpenAmmoBox(ammoBox);
        }
        else if (assignedInventorySlot?.ItemData != null)
        {
            assignedInventorySlot.ItemData.UseItem();
        }
    }

    // --------------------------------------------------
    // AMMO BOX
    // --------------------------------------------------

    private void OpenAmmoBox(AmmoBoxData ammoBox)
    {
        var playerInventory = FindObjectOfType<PlayerInventoryHolder>();
        if (playerInventory == null)
            return;

        if (playerInventory.PrimaryInventorySystem.AddToInventory(
            ammoBox.ammoToGive, ammoBox.ammoAmount))
        {
            assignedInventorySlot.RemoveFromStack(1);
        }
    }

    // --------------------------------------------------
    // HIGHLIGHT
    // --------------------------------------------------

    public void SetHighlight(bool state)
    {
        if (slotHighlight != null)
            slotHighlight.SetActive(state);
    }


    private void OnDestroy()
    {
        if (assignedInventorySlot != null)
            assignedInventorySlot.OnSlotChanged -= Refresh;
    }
}
