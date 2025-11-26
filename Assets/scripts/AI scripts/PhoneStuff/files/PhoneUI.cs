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
    public Slider timeSlider;
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
        phoneMainPanel.SetActive(false);
        contactListPanel.SetActive(false);
        conversationPanel.SetActive(false);
        priceOfferPanel.SetActive(false);

        priceSlider.minValue = 0;
        priceSlider.maxValue = 50000;
        priceSlider.value = 10000;
        priceSlider.onValueChanged.AddListener(OnPriceChanged);

        timeSlider.minValue = 1;
        timeSlider.maxValue = 60;
        timeSlider.value = 10;
        timeSlider.onValueChanged.AddListener(OnTimeChanged);

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
        homePanel.SetActive(false);
        RefreshContactList();
    }

    private void RefreshContactList()
    {
        foreach (Transform child in contactListContent)
            Destroy(child.gameObject);

        contactButtons.Clear();

        List<string> conversations = TextingManager.Instance.GetAllConversationNames();

        foreach (string npcName in conversations)
        {
            GameObject contactBtn = Instantiate(contactButtonPrefab, contactListContent);

            TextMeshProUGUI buttonText = contactBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = npcName;

                if (TextingManager.Instance.HasUnreadMessages(npcName))
                {
                    buttonText.text += " •";
                    buttonText.color = Color.yellow;
                }
            }

            Button btn = contactBtn.GetComponent<Button>();
            string npcNameCopy = npcName;
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
        foreach (Transform child in messageListContent)
            Destroy(child.gameObject);

        TextConversation conversation = TextingManager.Instance.GetConversation(currentConversationNPC);

        if (conversation != null)
        {
            foreach (TextMessage message in conversation.messages)
            {
                GameObject messagePrefab = message.isPlayerMessage ? playerMessagePrefab : npcMessagePrefab;
                GameObject messageObj = Instantiate(messagePrefab, messageListContent);

                TextMeshProUGUI messageText = messageObj.GetComponentInChildren<TextMeshProUGUI>();
                if (messageText != null)
                    messageText.text = message.messageContent;

                TextMeshProUGUI timestampText = messageObj.transform.Find("Timestamp")?.GetComponent<TextMeshProUGUI>();
                if (timestampText != null)
                    timestampText.text = message.timestamp.ToString("HH:mm");
            }
        }

        WeaponOrder activeOrder = TextingManager.Instance.GetActiveOrderForNPC(currentConversationNPC);
        if (activeOrder != null && !activeOrder.isPriceSet)
        {
            ShowPriceOfferOption();
        }
    }

    private void ShowPriceOfferOption()
    {
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

        string offerMessage = $"I can get you that for ${price:F0}. Pick up in {time:F0} minutes.";
        TextingManager.Instance.SendMessage(currentConversationNPC, offerMessage, true, TextMessage.MessageType.PriceOffer);

        TextingManager.Instance.SendPriceOffer(currentConversationNPC, price, time);

        priceOfferPanel.SetActive(false);
        RefreshConversation();
    }

    public void OnNewMessage(string npcName)
    {
        if (contactListPanel.activeSelf)
            RefreshContactList();
        else if (conversationPanel.activeSelf && currentConversationNPC == npcName)
            RefreshConversation();
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
