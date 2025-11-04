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
    public Slider timeSlider;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI timeText;
    public Button sendOfferButton;
    
    private string currentConversationNPC;
    private Dictionary<string, GameObject> contactButtons = new Dictionary<string, GameObject>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Setup UI
        phoneMainPanel.SetActive(false);
        contactListPanel.SetActive(false);
        conversationPanel.SetActive(false);
        priceOfferPanel.SetActive(false);
        
        // Setup sliders
        priceSlider.minValue = 0;
        priceSlider.maxValue = 50000;
        priceSlider.value = 10000;
        priceSlider.onValueChanged.AddListener(OnPriceChanged);
        
        timeSlider.minValue = 1;
        timeSlider.maxValue = 60; // 60 minutes max
        timeSlider.value = 10;
        timeSlider.onValueChanged.AddListener(OnTimeChanged);
        
        sendOfferButton.onClick.AddListener(SendPriceOffer);
        
        UpdatePriceText();
        UpdateTimeText();
    }
    
    public void OpenPhone()
    {
        phoneMainPanel.SetActive(true);
    }
    
    public void ClosePhone()
    {
        phoneMainPanel.SetActive(false);
        contactListPanel.SetActive(false);
        conversationPanel.SetActive(false);
        priceOfferPanel.SetActive(false);
    }
    
    public void OpenContactList()
    {
        contactListPanel.SetActive(true);
        conversationPanel.SetActive(false);
        priceOfferPanel.SetActive(false);
        
        RefreshContactList();
    }
    
    private void RefreshContactList()
    {
        // Clear existing buttons
        foreach (Transform child in contactListContent)
        {
            Destroy(child.gameObject);
        }
        contactButtons.Clear();
        
        // Get all conversations
        List<string> conversations = TextingManager.Instance.GetAllConversationNames();
        
        foreach (string npcName in conversations)
        {
            GameObject contactBtn = Instantiate(contactButtonPrefab, contactListContent);
            
            // Setup button text
            TextMeshProUGUI buttonText = contactBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = npcName;
                
                // Add unread indicator
                if (TextingManager.Instance.HasUnreadMessages(npcName))
                {
                    buttonText.text += " •";
                    buttonText.color = Color.yellow;
                }
            }
            
            // Setup button click
            Button btn = contactBtn.GetComponent<Button>();
            string npcNameCopy = npcName; // Capture for closure
            btn.onClick.AddListener(() => OpenConversation(npcNameCopy));
            
            contactButtons[npcName] = contactBtn;
        }
    }
    
    public void OpenConversation(string npcName)
    {
        currentConversationNPC = npcName;
        conversationPanel.SetActive(true);
        contactListPanel.SetActive(false);
        priceOfferPanel.SetActive(false);
        
        conversationHeaderText.text = npcName;
        
        RefreshConversation();
    }
    
    private void RefreshConversation()
    {
        // Clear existing messages
        foreach (Transform child in messageListContent)
        {
            Destroy(child.gameObject);
        }
        
        TextConversation conversation = TextingManager.Instance.GetConversation(currentConversationNPC);
        
        if (conversation != null)
        {
            foreach (TextMessage message in conversation.messages)
            {
                GameObject messagePrefab = message.isPlayerMessage ? playerMessagePrefab : npcMessagePrefab;
                GameObject messageObj = Instantiate(messagePrefab, messageListContent);
                
                TextMeshProUGUI messageText = messageObj.GetComponentInChildren<TextMeshProUGUI>();
                if (messageText != null)
                {
                    messageText.text = message.messageContent;
                }
                
                // Add timestamp (optional)
                TextMeshProUGUI timestampText = messageObj.transform.Find("Timestamp")?.GetComponent<TextMeshProUGUI>();
                if (timestampText != null)
                {
                    timestampText.text = message.timestamp.ToString("HH:mm");
                }
            }
        }
        
        // Check if there's an active order to show price offer button
        WeaponOrder activeOrder = TextingManager.Instance.GetActiveOrderForNPC(currentConversationNPC);
        if (activeOrder != null && !activeOrder.isPriceSet)
        {
            // Show the send price offer button or panel
            ShowPriceOfferOption();
        }
    }
    
    private void ShowPriceOfferOption()
    {
        // You could either show a button in the conversation to open the price panel,
        // or automatically show it at the bottom
        priceOfferPanel.SetActive(true);
    }
    
    public void OpenPriceOfferPanel()
    {
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
        priceText.text = $"${priceSlider.value:F0}";
    }
    
    private void UpdateTimeText()
    {
        timeText.text = $"{timeSlider.value:F0} min";
    }
    
    private void SendPriceOffer()
    {
        float price = priceSlider.value;
        float time = timeSlider.value;
        
        // Send the player's offer message
        string offerMessage = $"I can get you that for ${price:F0}. Pick up in {time:F0} minutes.";
        TextingManager.Instance.SendMessage(currentConversationNPC, offerMessage, true, TextMessage.MessageType.PriceOffer);
        
        // Have the NPC respond
        TextingManager.Instance.SendPriceOffer(currentConversationNPC, price, time);
        
        // Refresh conversation to show new messages
        priceOfferPanel.SetActive(false);
        RefreshConversation();
    }
    
    public void OnNewMessage(string npcName)
    {
        // Update UI if we're looking at the contact list or this conversation
        if (contactListPanel.activeSelf)
        {
            RefreshContactList();
        }
        else if (conversationPanel.activeSelf && currentConversationNPC == npcName)
        {
            RefreshConversation();
        }
    }
    
    public void BackToContactList()
    {
        conversationPanel.SetActive(false);
        priceOfferPanel.SetActive(false);
        OpenContactList();
    }
    
    public void BackToMainPhone()
    {
        contactListPanel.SetActive(false);
        conversationPanel.SetActive(false);
        priceOfferPanel.SetActive(false);
    }
}
