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
    [SerializeField] private Canvas joystick;
    public Button[] optionButtons;
    private bool showingOptions = false;

    private InputAction interactAction;

    public PlayerProgress playerProgress;
    private Queue<string> sentences;

    [Header("Character Busts")]
    public GameObject bustsContainer;
    public Image leftBustImage;
    public Image rightBustImage;
    public float bustFadeSpeed = 5f;

    void Start()
    {
        interactAction = InputSystem.actions.FindAction("Interact");

        sentences = new Queue<string>();
        // Initialize bust images with full alpha
        if (leftBustImage != null)
        {
            Color leftColor = leftBustImage.color;
            leftColor.a = 1f;
            leftBustImage.color = leftColor;
        }
        
        if (rightBustImage != null)
        {
            Color rightColor = rightBustImage.color;
            rightColor.a = 1f;
            rightBustImage.color = rightColor;
        }
    }

    public void StartDialogue(Dialogue dialogue)
    {
        StartDialogue(dialogue, null);
    }

    public void StartDialogue(Dialogue dialogue, NPCInt npc)
    {
        joystick.enabled = false;
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

        // Handle character busts
        bustsContainer.SetActive(dialogue.showBusts);
        if (dialogue.showBusts)
        {
            leftBustImage.sprite = dialogue.leftBust;
            rightBustImage.sprite = dialogue.rightBust;
            
            // Set alpha directly to 1
            Color leftColor = leftBustImage.color;
            Color rightColor = rightBustImage.color;
            leftColor.a = 1f;
            rightColor.a = 1f;
            leftBustImage.color = leftColor;
            rightBustImage.color = rightColor;
            
            // Comment out fade for now
            // StartCoroutine(FadeBustsIn());
        }

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
        if (currentDialogue != null && currentDialogue.showBusts)
        {
            StartCoroutine(FadeBustsOut());
        }

        playerProgress.TalkToNPC(currentDialogue.name);
        joystick.enabled = true;
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

        // Set up options
        for (int i = 0; i < currentDialogue.options.Length; i++)
        {
            if (i < optionButtons.Length)
            {
                int optionIndex = i; // Needed for closure
                DialogueOption option = currentDialogue.options[i];
                
                // Set button text
                optionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = option.optionText;
                optionButtons[i].gameObject.SetActive(true);
                
                // Add click listener
                optionButtons[i].onClick.AddListener(() => SelectOption(option));
            }
        }

        // Hide unused buttons
        for (int i = currentDialogue.options.Length; i < optionButtons.Length; i++)
        {
            optionButtons[i].gameObject.SetActive(false);
        }
    }

    private void SelectOption(DialogueOption option)
    {
        optionsPanel.SetActive(false);
        
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

    private IEnumerator FadeBustsIn()
    {
        // Set initial alpha to 0
        Color leftColor = leftBustImage.color;
        Color rightColor = rightBustImage.color;
        leftColor.a = 0f;
        rightColor.a = 0f;
        leftBustImage.color = leftColor;
        rightBustImage.color = rightColor;

        // Fade to full opacity
        while (leftColor.a < 1f)
        {
            leftColor.a += Time.deltaTime * bustFadeSpeed;
            rightColor.a += Time.deltaTime * bustFadeSpeed;
            
            // Clamp values to prevent exceeding 1
            leftColor.a = Mathf.Clamp01(leftColor.a);
            rightColor.a = Mathf.Clamp01(rightColor.a);
            
            leftBustImage.color = leftColor;
            rightBustImage.color = rightColor;
            yield return null;
        }
    }

    private IEnumerator FadeBustsOut()
    {
        Color leftColor = leftBustImage.color;
        Color rightColor = rightBustImage.color;

        while (leftColor.a > 0f)
        {
            leftColor.a -= Time.deltaTime * bustFadeSpeed;
            rightColor.a -= Time.deltaTime * bustFadeSpeed;
            
            // Clamp values to prevent going below 0
            leftColor.a = Mathf.Clamp01(leftColor.a);
            rightColor.a = Mathf.Clamp01(rightColor.a);
            
            leftBustImage.color = leftColor;
            rightBustImage.color = rightColor;
            yield return null;
        }
        
        bustsContainer.SetActive(false);
    }

    IEnumerator Undialogue(NPCInt npcintscript){
        yield return new WaitForSeconds(0.2f);
        npcintscript.isInDialogue = false;
    }
}
