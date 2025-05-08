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

    [SerializeField] private Button interactButton;

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
                interactButton.enabled = true;
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
            interactButton.enabled = false;
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
sbyte         // Make the GameObject bigger when interaction is triggered
        transform.localScale = new Vector3(2f, 2f, 2f); // Adjust scale as needed

        if (interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(false);
        }
        FindObjectOfType<DialogueManager>().StartDialogue(currentDialogue);
    }
}
