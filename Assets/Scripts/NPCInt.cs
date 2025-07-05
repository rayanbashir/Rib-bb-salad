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
    public bool isInDialogue = false;
    private InputAction interactAction;


    void Start()
    {
        animator.SetBool("inRange", false);
        canInteract = false;
        currentDialogue = dialogue;
        defaultDialogue = dialogue; // Store the original dialogue
        interactAction = InputSystem.actions.FindAction("Interact");
        if (interactMark != null)
        {
            interactMark.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
            if (interactMark != null && !isInDialogue)
            {
                interactMark.SetActive(true);
            }
            animator.SetBool("inRange", true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            animator.SetBool("inRange", false);
            canInteract = false;
            isInDialogue = false;
            if (interactMark != null)
            {
                interactMark.SetActive(false);
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
                StartCoroutine(DelayedTriggerDialogue());
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
        if (interactMark != null)
        {
            interactMark.SetActive(false);
        }
        FindObjectOfType<DialogueManager>().StartDialogue(currentDialogue);
    }

    private IEnumerator DelayedTriggerDialogue()
    {
        yield return new WaitForSeconds(0.3f);
        TriggerDialogue();
    }
}

