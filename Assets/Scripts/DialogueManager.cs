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
        Debug.Log("Sami is ended the dialuge");


        playerProgress.TalkToNPC(currentDialogue.name);
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
        
        // Clear the current NPC reference
        currentNPC = null;
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
