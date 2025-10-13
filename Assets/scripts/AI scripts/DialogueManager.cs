using UnityEngine;
using TMPro;
using Ink.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI nameText; 

    [Header("Choices UI")]
    [SerializeField] private GameObject[] choices;
    private TextMeshProUGUI[] choicesText;

    private Story currentStory;
    public bool dialogueIsPlaying { get; private set; }
    private static DialogueManager instance;

    public System.Action OnDialogueEnded;

    public event System.Action OnDialogueEnd;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("Multiple instances of DialogueManager detected.");
        }
        instance = this;
    }

    public static DialogueManager GetInstance()
    {
        return instance;
    }

    private void Start()
    {
        dialogueIsPlaying = false;
        dialoguePanel.SetActive(false);

        choicesText = new TextMeshProUGUI[choices.Length];
        for (int i = 0; i < choices.Length; i++)
        {
            choicesText[i] = choices[i].GetComponentInChildren<TextMeshProUGUI>();

            int choiceIndex = i;
            Button btn = choices[i].GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => MakeChoice(choiceIndex));
            }
        }

        dialogueText.text = "";
        if (nameText != null) nameText.text = "";
    }

    private void Update()
    {
        if (!dialogueIsPlaying) return;

        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            && currentStory.currentChoices.Count == 0)
        {
            ContinueStory();
        }
    }

    public void EnterDialogueMode(TextAsset inkJSON)
    {
        currentStory = new Story(inkJSON.text);
        dialogueIsPlaying = true;
        dialoguePanel.SetActive(true);
        ContinueStory();
    }

    private IEnumerator ExitDialogueMode()
    {
        yield return new WaitForSeconds(0.2f);
        dialogueIsPlaying = false;
        dialoguePanel.SetActive(false);
        dialogueText.text = "";
        if (nameText != null) nameText.text = "";
        dialogueIsPlaying = false;
        OnDialogueEnded?.Invoke();
        OnDialogueEnd?.Invoke();
    }

    private void ContinueStory()
    {
        if (currentStory.canContinue)
        {
            string text = currentStory.Continue();
            dialogueText.text = text;
            HandleTags(currentStory.currentTags);
            DisplayChoices();
        }
        else
        {
            StartCoroutine(ExitDialogueMode());
        }
    }

    private void HandleTags(List<string> currentTags)
    {
        foreach (string tag in currentTags)
        {
            string[] splitTag = tag.Split(':');
            if (splitTag.Length != 2)
            {
                Debug.LogWarning("Tag could not be parsed: " + tag);
                continue;
            }

            string tagKey = splitTag[0].Trim().ToLower();
            string tagValue = splitTag[1].Trim();

            switch (tagKey)
            {
                case "speaker":
                    if (nameText != null)
                        nameText.text = tagValue;
                    break;
            }
        }
    }

    private void DisplayChoices()
    {
        // Hide all choice buttons before updating
        for (int i = 0; i < choices.Length; i++)
        {
            choices[i].gameObject.SetActive(false);
        }

        List<Choice> currentChoices = currentStory.currentChoices;

        if (currentChoices.Count > choices.Length)
        {
            Debug.LogError("More choices were given than the UI can support.");
        }

        for (int i = 0; i < currentChoices.Count && i < choices.Length; i++)
        {
            choices[i].gameObject.SetActive(true);
            choicesText[i].text = currentChoices[i].text;
        }

        StartCoroutine(SelectFirstChoice());
    }


    private IEnumerator SelectFirstChoice()
    {
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        if (choices.Length > 0 && choices[0].activeSelf)
            EventSystem.current.SetSelectedGameObject(choices[0].gameObject);
    }

    public void MakeChoice(int choiceIndex)
    {
        // --- SAFETY CHECKS ---
        if (currentStory == null)
        {
            Debug.LogWarning("MakeChoice called but currentStory is null.");
            return;
        }

        if (currentStory.currentChoices == null || currentStory.currentChoices.Count == 0)
        {
            Debug.LogWarning("MakeChoice called but no choices are currently available.");
            return;
        }

        if (choiceIndex < 0 || choiceIndex >= currentStory.currentChoices.Count)
        {
            Debug.LogWarning($"MakeChoice called with invalid index {choiceIndex}. Total choices: {currentStory.currentChoices.Count}");
            return;
        }
        // ----------------------

        currentStory.ChooseChoiceIndex(choiceIndex);
        ContinueStory();
    }

}