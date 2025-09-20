using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;


public class DialogueManager : MonoBehaviour
{
    private Dialogue currentDialogue;
    private NPCInt currentNPC; // Track which NPC started the dialogue
    private string lastDialogueName; // Remember dialogue name even if cleared
    private bool talkReported; // Ensure TalkToNPC runs once per dialogue
    private bool isEndingDialogue; // Re-entrancy guard
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public Movement playerMovement;
    public Animator animator;
    public GameObject optionsPanel;
    public Button[] optionButtons;
    private bool showingOptions = false;

    private InputAction interactAction;

    public PlayerProgress playerProgress;
    private Queue<string> sentences;

    void Start()
    {
        interactAction = InputSystem.actions.FindAction("Interact");

        sentences = new Queue<string>();

    }

    public void StartDialogue(Dialogue dialogue)
    {
        StartDialogue(dialogue, null);
    }

    public void StartDialogue(Dialogue dialogue, NPCInt npc)
    {
        Debug.Log(dialogue.sentences[0]);
        currentDialogue = dialogue;
        currentNPC = npc; // Track which NPC started this dialogue
        // Reset per-dialogue flags and capture name early
        talkReported = false;
        isEndingDialogue = false;
        lastDialogueName = dialogue != null ? dialogue.name : null;
        if (currentNPC != null)
        {
            // Notify the NPC which dialogue just started (for stateful behaviors)
            currentNPC.OnDialogueStarted(currentDialogue);
        }
        Debug.Log(currentDialogue.sentences[0]);
        animator.SetBool("IsOpen", true);
        Debug.Log(sentences);
        sentences.Clear(); 
        nameText.text = dialogue.name;
        Debug.Log(dialogue.LockPlayerMovement); 
        playerMovement.canMove = !dialogue.LockPlayerMovement;



        foreach (string sentence in dialogue.sentences)
        {
            sentences.Enqueue(sentence);
        }

        // showingOptions = dialogue.hasOptions;
        showingOptions = (dialogue.options != null && dialogue.options.Length > 0);
        DisplayNextSentence();
    }

    private void Update()
    {
        if (currentDialogue != null)
        {
            if (interactAction.WasPressedThisFrame())
            {
                Debug.Log("Display next sentenced");
                DisplayNextSentence();
            }
        }
    }

    public void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            // if (showingOptions)
            // {
            //     ShowOptions();
            // }
            // else
            // {
            //     EndDialogue();
            // }
            // return;
            if (showingOptions)
            {
                ShowOptions();
            }
            else
            {
                EndDialogue();
            }
            return;
        }

        string sentence = sentences.Dequeue();
        StopAllCoroutines();
        StartCoroutine(TypeSentence(sentence));
    }

    IEnumerator TypeSentence (string sentence)
    {
        dialogueText.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return null;
        }
    }



    public void EndDialogue()
    {
        if (isEndingDialogue) {
            // Prevent double handling if EndDialogue gets called twice quickly
            return;
        }
        isEndingDialogue = true;
        Debug.Log("Sami is ended the dialuge");

        // Capture ended dialogue safely
        Dialogue endedDialogue = currentDialogue;
        string endedDialogueName = endedDialogue != null ? endedDialogue.name : null;

        // Determine best available name to report
        string nameToReport = !string.IsNullOrEmpty(endedDialogueName)
            ? endedDialogueName
            : (!string.IsNullOrEmpty(lastDialogueName)
                ? lastDialogueName
                : (nameText != null && !string.IsNullOrEmpty(nameText.text)
                    ? nameText.text
                    : (currentNPC != null ? currentNPC.gameObject.name : null)));

        // Lazy-assign PlayerProgress if missing
        if (playerProgress == null)
        {
            playerProgress = FindObjectOfType<PlayerProgress>();
        }

        if (!talkReported)
        {
            if (playerProgress != null && !string.IsNullOrEmpty(nameToReport))
            {
                playerProgress.TalkToNPC(nameToReport);
                talkReported = true;
            }
            else
            {
                if (playerProgress == null)
                    Debug.LogWarning("PlayerProgress is not assigned and could not be found; skipping TalkToNPC.");
                else
                    Debug.LogWarning("Could not determine dialogue/NPC name to report; skipping TalkToNPC.");
            }
        }
        Debug.Log("End of conversation");
        animator.SetBool("IsOpen", false);
        optionsPanel.SetActive(false);
        playerMovement.canMove = true;
        showingOptions = false;

        NPCInt npcToUndialogue = currentNPC != null ? currentNPC : FindObjectOfType<NPCInt>();
        

        if (npcToUndialogue != null)
        {
            StartCoroutine(Undialogue(npcToUndialogue));
        }
        
        // Notify NPC and then clear the reference
        if (npcToUndialogue != null)
        {
            npcToUndialogue.OnDialogueEnded(endedDialogue);
        }

        // Clear the current NPC reference
        currentNPC = null;
        currentDialogue = null;
        lastDialogueName = null;
        // Reset end guard for future dialogues
        isEndingDialogue = false;
    }


    private void ShowOptions()
    {
        Debug.Log("Showed Option");
        optionsPanel.SetActive(true);

        // Clear existing listeners
        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].onClick.RemoveAllListeners();
        }

        // Filter options based on required item/clue
        List<DialogueOption> availableOptions = new List<DialogueOption>();
        foreach (var option in currentDialogue.options)
        {
            bool hasItem = string.IsNullOrEmpty(option.requiredItem) || InventoryManager.Instance.HasItem(option.requiredItem);
            bool hasClue = string.IsNullOrEmpty(option.requiredClue) || InventoryManager.Instance.HasItem(option.requiredClue);
            if (hasItem && hasClue)
            {
                availableOptions.Add(option);
            }
        }

        // Set up available options
        for (int i = 0; i < availableOptions.Count; i++)
        {
            if (i < optionButtons.Length)
            {
                int optionIndex = i; // Needed for closure
                DialogueOption option = availableOptions[i];

                // Set button text
                optionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = option.optionText;
                optionButtons[i].gameObject.SetActive(true);

                // Add click listener
                optionButtons[i].onClick.AddListener(() => SelectOption(option));
            }
        }

        // Hide unused buttons
        for (int i = availableOptions.Count; i < optionButtons.Length; i++)
        {
            optionButtons[i].gameObject.SetActive(false);
        }
    }

    private void SelectOption(DialogueOption option)
    {
        optionsPanel.SetActive(false);

        // Consume required item if needed
        if (!string.IsNullOrEmpty(option.requiredItem) && option.consumeRequiredItem)
        {
            InventoryManager.Instance.RemoveItemByName(option.requiredItem);
        }

        // Consume required clue if needed
        if (!string.IsNullOrEmpty(option.requiredClue) && option.consumeRequiredClue)
        {
            InventoryManager.Instance.RemoveItemByName(option.requiredClue);
        }

        if (option.changesFutureDialogue && currentNPC != null && currentNPC.allowDialogueChanges && option.subsequentDialogue != null)
        {
            currentNPC.UpdateDialogue(option.subsequentDialogue);
        }

        StartDialogue(option.nextDialogue, currentNPC);
    }

    public bool IsDialogueActive()
    {
        return animator.GetBool("IsOpen");
    }



    IEnumerator Undialogue(NPCInt npcintscript){
        yield return new WaitForSeconds(0.2f);
        npcintscript.isInDialogue = false;
    }
}
