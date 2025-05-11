using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class NPCInt : MonoBehaviour
{
    public GameObject interactMark;
    public Animator animator;
    private bool canInteract;
    public Dialogue dialogue;
    private Dialogue currentDialogue; // Track current dialogue state
    private bool hasInteracted = false;
    public bool allowDialogueChanges = false; // Toggle for dialogue change feature
    private Dialogue defaultDialogue; // Store the original dialogue
    public TextMeshProUGUI interactionPrompt; // Reference to UI text element
    public string promptMessage = "Press E to interact"; // Customizable message
    public bool isInDialogue = false;
    private InputAction interactAction;


    void Start()
    {
        animator.SetBool("inRange", false);
        canInteract = false;
        currentDialogue = dialogue;
        defaultDialogue = dialogue; // Store the original dialogue
        interactAction = InputSystem.actions.FindAction("Interact");
        if (interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            animator.SetBool("inRange", true);
            canInteract = true;
            if (interactionPrompt != null && !isInDialogue)
            {
                interactionPrompt.text = promptMessage;
                interactionPrompt.gameObject.SetActive(true);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            animator.SetBool("inRange", false);
            canInteract = false;
            isInDialogue = false;
            if (interactionPrompt != null)
            {
                interactionPrompt.gameObject.SetActive(false);
            }
            FindObjectOfType<DialogueManager>().EndDialogue();
        }
    }

    void Update()
    {
        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        bool isDialogueActive = dialogueManager != null && dialogueManager.IsDialogueActive();

        if (interactAction.IsPressed())
        {
            if (canInteract && !isInDialogue && !isDialogueActive)
            {
                TriggerDialogue();
            }
        }
    }

    public void UpdateDialogue(Dialogue newDialogue)
    {
        if (allowDialogueChanges)
        {
            currentDialogue = newDialogue;
            hasInteracted = true;
        }
    }

    public void ResetDialogue()
    {
        currentDialogue = defaultDialogue;
        hasInteracted = false;
    }

    public void TriggerDialogue()
    {
        isInDialogue = true;
        // Make the GameObject bigger when interaction is triggered
        transform.localScale = new Vector3(2f, 2f, 15f); // Adjust scale as needed

        if (interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(false);
        }

        // Insert a message after "Will you accept or decline?" if it exists
        string targetSentence = "Will you accept or decline?";
        string messageToAdd = "This is the message after accept/decline."; // Change this to your desired message

        // Assuming Dialogue has a List<string> or string[] called sentences
        var sentencesList = new List<string>(currentDialogue.sentences);
        int idx = sentencesList.IndexOf(targetSentence);
        if (idx != -1)
        {
            sentencesList.Insert(idx + 1, messageToAdd);
            currentDialogue.sentences = sentencesList.ToArray();
        }

        FindObjectOfType<DialogueManager>().StartDialogue(currentDialogue);
    }
}
