using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [System.Serializable]
    public class ShopItem
    {
        public string itemName;
        public GameObject prefab;
        public int price;
        public Sprite icon;

        [Header("Inventory Link")]
        public InventoryItemData itemData; // Links directly to your item ScriptableObject
    }

    [Header("Shop Settings")]
    public List<ShopItem> shopItems = new List<ShopItem>();
    public Transform spawnPoint;

    [Header("UI References")]
    public GameObject shopUI;
    public GameObject shopItemButtonPrefab;
    public Transform shopContentParent;
    public GameObject playerHUD;
    public Button exitButton;

    [Header("Player Settings")]
    public int playerMoney = 100;
    public TextMeshProUGUI playerMoneyText;
    public MonoBehaviour playerMovementScript;

    private bool shopOpen = false;

    private void Start()
    {
        PopulateShop();
        UpdateMoneyUI();
        shopUI.SetActive(false);

        if (exitButton != null)
            exitButton.onClick.AddListener(() => ToggleShop(false));
    }

    private void Update()
    {
        // Allow ESC to close shop
        if (shopOpen && Input.GetKeyDown(KeyCode.Escape))
            ToggleShop(false);
    }

    private void PopulateShop()
    {
        foreach (Transform child in shopContentParent)
            Destroy(child.gameObject);

        foreach (ShopItem item in shopItems)
        {
            GameObject buttonObj = Instantiate(shopItemButtonPrefab, shopContentParent);
            buttonObj.name = $"ShopButton_{item.itemName}";

            // Text fields
            TextMeshProUGUI[] texts = buttonObj.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                if (text.name == "ItemName") text.text = item.itemName;
                if (text.name == "Price") text.text = "$" + item.price;
            }

            // Icon
            Image iconImage = buttonObj.GetComponentInChildren<Image>();
            if (iconImage != null && item.icon != null)
                iconImage.sprite = item.icon;

            // Button click
            Button button = buttonObj.GetComponent<Button>();
            button.onClick.AddListener(() => BuyItem(item));
        }
    }

    private void UpdateMoneyUI()
    {
        if (playerMoneyText != null)
            playerMoneyText.text = $"Money: ${playerMoney}";
    }

    private void BuyItem(ShopItem item)
    {
        if (playerMoney < item.price)
        {
            Debug.Log("Not enough money!");
            return;
        }

        playerMoney -= item.price;
        UpdateMoneyUI();

        if (item.prefab != null && spawnPoint != null)
        {
            // Random offset
            Vector3 spawnPos = spawnPoint.position + new Vector3(
                Random.Range(-0.5f, 0.5f),
                0f,
                Random.Range(-0.5f, 0.5f)
            );

            GameObject spawned = Instantiate(item.prefab, spawnPos, spawnPoint.rotation);
            Debug.Log($"Spawned {item.itemName}");

            // Add ItemPickup automatically
            ItemPickup pickup = spawned.GetComponent<ItemPickup>();
            if (pickup == null)
            {
                pickup = spawned.AddComponent<ItemPickup>();
                Debug.Log($"Added ItemPickup script to {spawned.name}");
            }

            // Assign InventoryItemData from ShopItem
            if (item.itemData != null)
            {
                pickup.ItemData = item.itemData;
            }
            else
            {
                Debug.LogWarning($"No InventoryItemData assigned for {item.itemName}");
            }

            // Ensure required components exist
            if (spawned.GetComponent<SphereCollider>() == null)
                spawned.AddComponent<SphereCollider>();

            if (spawned.GetComponent<UniqueID>() == null)
                spawned.AddComponent<UniqueID>();
        }
        else
        {
            Debug.LogWarning("Missing prefab or spawn point!");
        }
    }

    public void ToggleShop(bool state)
    {
        shopOpen = state;
        shopUI.SetActive(state);

        if (playerHUD != null)
            playerHUD.SetActive(!state);

        if (playerMovementScript != null)
            playerMovementScript.enabled = !state;

        Cursor.visible = state;
        Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
