using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhoneUI : MonoBehaviour
{
    public static PhoneUI Instance { get; private set; }

    [Header("Phone Panels")]
    public GameObject phoneMainPanel;
    public GameObject contactListPanel;
    public GameObject conversationPanel;
    public GameObject priceOfferPanel;
    public GameObject homePanel;

    [Header("Contact List")]
    public Transform contactListContent;
    public GameObject contactButtonPrefab;

    [Header("Conversation View")]
    public Transform messageListContent;
    public GameObject playerMessagePrefab;
    public GameObject npcMessagePrefab;
    public TextMeshProUGUI conversationHeaderText;

    [Header("Price Offer UI")]
    public Slider priceSlider;
    public Slider timeSlider; // NOW HOURS
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI timeText;
    public Button sendOfferButton;

    [Header("Player References")]
    public GameObject playerHUD;
    public MonoBehaviour playerMovementScript;

    private bool phoneOpen = false;
    private string currentConversationNPC;
    private Dictionary<string, GameObject> contactButtons = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Ensure references exist to avoid null refs on startup
        if (phoneMainPanel != null) phoneMainPanel.SetActive(false);
        if (contactListPanel != null) contactListPanel.SetActive(false);
        if (conversationPanel != null) conversationPanel.SetActive(false);
        if (priceOfferPanel != null) priceOfferPanel.SetActive(false);
        if (homePanel != null) homePanel.SetActive(true);

        // PRICE SLIDER
        if (priceSlider != null)
        {
            priceSlider.minValue = 0;
            priceSlider.maxValue = 50000;
            priceSlider.value = 10000;
            priceSlider.onValueChanged.AddListener(OnPriceChanged);
        }

        // TIME SLIDER (IN-GAME HOURS)
        if (timeSlider != null)
        {
            timeSlider.minValue = 0.1f;   // 0.1 hours = 6 min
            timeSlider.maxValue = 8f;     // max 8 in-game hours
            timeSlider.value = 1f;        // default 1 hour
            timeSlider.onValueChanged.AddListener(OnTimeChanged);
        }

        if (sendOfferButton != null)
            sendOfferButton.onClick.AddListener(SendPriceOffer);

        UpdatePriceText();
        UpdateTimeText();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePhone();
        }
    }

    private void TogglePhone()
    {
        phoneOpen = !phoneOpen;

        if (phoneOpen)
            OpenPhone();
        else
            ClosePhone();

        ApplyPhoneState(phoneOpen);
    }

    private void ApplyPhoneState(bool state)
    {
        if (playerHUD != null)
            playerHUD.SetActive(!state);

        if (playerMovementScript != null)
            playerMovementScript.enabled = !state;

        Cursor.visible = state;
        Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void OpenPhone()
    {
        if (phoneMainPanel != null)
            phoneMainPanel.SetActive(true);
    }

    public void ClosePhone()
    {
        if (phoneMainPanel != null) phoneMainPanel.SetActive(false);
        if (contactListPanel != null) contactListPanel.SetActive(false);
        if (conversationPanel != null) conversationPanel.SetActive(false);
        if (priceOfferPanel != null) priceOfferPanel.SetActive(false);
        if (homePanel != null) homePanel.SetActive(true);
    }

    public void OpenContactList()
    {
        if (contactListPanel != null) contactListPanel.SetActive(true);
        if (conversationPanel != null) conversationPanel.SetActive(false);
        if (priceOfferPanel != null) priceOfferPanel.SetActive(false);
        if (homePanel != null) homePanel.SetActive(false);
        RefreshContactList();
    }

    private void RefreshContactList()
    {
        if (contactListContent != null)
        {
            foreach (Transform child in contactListContent)
                Destroy(child.gameObject);
        }

        contactButtons.Clear();

        // Safely get conversations from TextingManager
        List<string> conversations = new List<string>();
        if (TextingManager.Instance != null)
        {
            var conv = TextingManager.Instance.GetAllConversationNames();
            if (conv != null)
                conversations = conv;
        }

        // If no conversations, optionally show nothing
        foreach (string npcName in conversations)
        {
            if (contactButtonPrefab == null || contactListContent == null) break;

            GameObject contactBtn = Instantiate(contactButtonPrefab, contactListContent);

            TextMeshProUGUI buttonText = contactBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = npcName;

                // Null-safe unread check
                bool hasUnread = false;
                if (TextingManager.Instance != null)
                {
                    try
                    {
                        hasUnread = TextingManager.Instance.HasUnreadMessages(npcName);
                    }
                    catch
                    {
                        hasUnread = false;
                    }
                }

                if (hasUnread)
                {
                    buttonText.text += " •";
                    buttonText.color = Color.yellow;
                }
            }

            Button btn = contactBtn.GetComponent<Button>();
            string npcCopy = npcName;
            if (btn != null)
                btn.onClick.AddListener(() => OpenConversation(npcCopy));

            contactButtons[npcName] = contactBtn;
        }
    }

    public void OpenConversation(string npcName)
    {
        if (string.IsNullOrEmpty(npcName)) return;

        currentConversationNPC = npcName;
        if (conversationPanel != null) conversationPanel.SetActive(true);
        if (contactListPanel != null) contactListPanel.SetActive(false);
        if (priceOfferPanel != null) priceOfferPanel.SetActive(false);

        if (conversationHeaderText != null)
            conversationHeaderText.text = npcName;

        RefreshConversation();
    }

    private void RefreshConversation()
    {
        if (messageListContent != null)
        {
            foreach (Transform child in messageListContent)
                Destroy(child.gameObject);
        }

        // Null-safe conversation fetch
        TextConversation convo = null;
        if (TextingManager.Instance != null && !string.IsNullOrEmpty(currentConversationNPC))
            convo = TextingManager.Instance.GetConversation(currentConversationNPC);

        if (convo != null && messageListContent != null)
        {
            foreach (TextMessage msg in convo.messages)
            {
                GameObject prefab = msg.isPlayerMessage ? playerMessagePrefab : npcMessagePrefab;
                if (prefab == null) continue;

                GameObject obj = Instantiate(prefab, messageListContent);

                TextMeshProUGUI messageText = obj.GetComponentInChildren<TextMeshProUGUI>();
                if (messageText != null)
                    messageText.text = msg.messageContent;

                TextMeshProUGUI timestamp = obj.transform.Find("Timestamp")?.GetComponent<TextMeshProUGUI>();
                if (timestamp != null)
                    timestamp.text = msg.timestamp.ToString("HH:mm");
            }
        }

        // Null-safe check for active order
        WeaponOrder activeOrder = null;
        if (TextingManager.Instance != null && !string.IsNullOrEmpty(currentConversationNPC))
            activeOrder = TextingManager.Instance.GetActiveOrderForNPC(currentConversationNPC);

        if (activeOrder != null && !activeOrder.isPriceSet)
        {
            ShowPriceOfferOption();
        }
        else
        {
            // Ensure price panel hidden if no active order
            if (priceOfferPanel != null && (activeOrder == null || activeOrder.isPriceSet))
                priceOfferPanel.SetActive(false);
        }
    }

    private void ShowPriceOfferOption()
    {
        if (priceOfferPanel != null)
            priceOfferPanel.SetActive(true);
    }

    private void OnPriceChanged(float value)
    {
        UpdatePriceText();
    }

    private void OnTimeChanged(float value)
    {
        UpdateTimeText();
    }

    private void UpdatePriceText()
    {
        if (priceText != null)
            priceText.text = $"${(priceSlider != null ? priceSlider.value : 0f):F0}";
    }

    private void UpdateTimeText()
    {
        if (timeText != null)
            timeText.text = $"{(timeSlider != null ? timeSlider.value : 0f):F1} hours";
    }

    private void SendPriceOffer()
    {
        if (string.IsNullOrEmpty(currentConversationNPC))
            return;

        float price = priceSlider != null ? priceSlider.value : 0f;
        float hours = timeSlider != null ? timeSlider.value : 1f; // DO NOT CONVERT — pure in-game hours

        // Player message inside chat
        string offerMessage = $"I can get you that for ${price:F0}. Pick up in {hours:F1} hours.";
        if (TextingManager.Instance != null)
            TextingManager.Instance.SendMessage(currentConversationNPC, offerMessage, true, TextMessage.MessageType.PriceOffer);

        // Send to TextingManager (in-game hours)
        if (TextingManager.Instance != null)
            TextingManager.Instance.SendPriceOffer(currentConversationNPC, price, hours);

        if (priceOfferPanel != null)
            priceOfferPanel.SetActive(false);

        RefreshConversation();
    }

    public void OnNewMessage(string npcName)
    {
        if (contactListPanel != null && contactListPanel.activeSelf)
            RefreshContactList();
        else if (conversationPanel != null && conversationPanel.activeSelf && currentConversationNPC == npcName)
            RefreshConversation();
    }

    public void BackToContactList()
    {
        if (conversationPanel != null) conversationPanel.SetActive(false);
        if (priceOfferPanel != null) priceOfferPanel.SetActive(false);
        OpenContactList();
    }

    public void BackToMainPhone()
    {
        if (contactListPanel != null) contactListPanel.SetActive(false);
        if (conversationPanel != null) conversationPanel.SetActive(false);
        if (priceOfferPanel != null) priceOfferPanel.SetActive(false);
        if (homePanel != null) homePanel.SetActive(true);
    }
}
