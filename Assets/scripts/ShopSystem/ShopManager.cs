using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cinemachine;

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
        public InventoryItemData itemData;
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

    [Header("Cinemachine Cameras")]
    public CinemachineVirtualCamera mainVCam;
    public CinemachineVirtualCamera shopVCam;

    [Header("Cinemachine Priority Settings")]
    public int mainPriority = 20; // Main camera has higher default priority
    public int shopPriority = 10;

    private bool shopOpen = false;

    [Header("NPC Dialogue Lines (Set in Inspector)")]
    public List<string> npcLines = new List<string>();
    public TextMeshProUGUI npcDialogueText;
    public Button nextDialogueButton;

    private int currentLineIndex = 0;

    private void Start()
    {
        // Populate shop and UI
        PopulateShop();
        UpdateMoneyUI();

        // Ensure shop is closed at start
        ToggleShop(false);

        // Set up button events
        if (exitButton != null)
            exitButton.onClick.AddListener(() => ToggleShop(false));

        if (nextDialogueButton != null)
            nextDialogueButton.onClick.AddListener(NextDialogue);
    }

    private void Update()
    {
        // ESC closes shop
        if (shopOpen && Input.GetKeyDown(KeyCode.Escape))
            ToggleShop(false);

        // Spacebar shows next dialogue line
        if (shopOpen && Input.GetKeyDown(KeyCode.Space))
            NextDialogue();
    }

    private void PopulateShop()
    {
        // Clear existing buttons
        foreach (Transform child in shopContentParent)
            Destroy(child.gameObject);

        // Add shop items
        foreach (ShopItem item in shopItems)
        {
            GameObject buttonObj = Instantiate(shopItemButtonPrefab, shopContentParent);
            buttonObj.name = $"ShopButton_{item.itemName}";

            // Update texts
            TextMeshProUGUI[] texts = buttonObj.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                if (text.name == "ItemName") text.text = item.itemName;
                if (text.name == "Price") text.text = "$" + item.price;
            }

            // Update icon
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
                pickup = spawned.AddComponent<ItemPickup>();

            // Assign InventoryItemData
            if (item.itemData != null)
                pickup.ItemData = item.itemData;

            // Ensure required components
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

        if (state)
        {
            // Open shop shop camera active
            shopVCam.Priority = mainPriority + 1; // higher than main
            shopVCam.MoveToTopOfPrioritySubqueue();
            mainVCam.Priority = mainPriority;

            NextDialogue();
        }
        else
        {
            // Close shop main camera active
            mainVCam.Priority = mainPriority + 1;
            mainVCam.MoveToTopOfPrioritySubqueue();
            shopVCam.Priority = shopPriority;

            // Clear dialogue
            if (npcDialogueText != null)
                npcDialogueText.text = "";
        }
    }

    private void NextDialogue()
    {
        if (npcLines.Count == 0)
        {
            if (npcDialogueText != null)
                npcDialogueText.text = "…";
            return;
        }

        currentLineIndex = Random.Range(0, npcLines.Count);
        if (npcDialogueText != null)
            npcDialogueText.text = npcLines[currentLineIndex];
    }
}
