using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class ItemInteractable : MonoBehaviour
{
    [Header("Item Requirements")]
    [SerializeField] private bool requiresItem = false;
    [SerializeField] private string requiredItemName;
    [SerializeField] private bool isSingleUse = true;

    [Header("UI")]
    [SerializeField] private Button interactButton;
    public TextMeshProUGUI interactionPrompt;
    public string defaultPromptMessage = "Press E to interact";
    public string itemPromptMessage = "Press E to use {0}";
    
    [Header("Dialogue")]
    [SerializeField] private ItemInteractableDialogue dialogueSettings;
    [SerializeField] private float reinteractDelay = 0.5f;
    
    public UnityEvent onInteractionTriggered;
    
    private bool canInteract = false;
    private bool hasBeenUsed = false;
    private DialogueManager dialogueManager;
    public bool isInDialogue = false;
    private InputAction interactAction;
    private bool cooldownActive = false;

    void Start()
    {
        canInteract = false;
        interactAction = InputSystem.actions.FindAction("Interact");
        dialogueManager = FindObjectOfType<DialogueManager>();
        if (interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && (!hasBeenUsed || !isSingleUse))
        {
            canInteract = true;
            
            if (interactionPrompt != null && !isInDialogue && !cooldownActive)
            {
                if (requiresItem && !InventoryManager.Instance.HasItem(requiredItemName))
                {
                    interactionPrompt.text = defaultPromptMessage;
                }
                else if (requiresItem)
                {
                    interactionPrompt.text = string.Format(itemPromptMessage, requiredItemName);
                }
                else
                {
                    interactionPrompt.text = defaultPromptMessage;
                }
                interactionPrompt.gameObject.SetActive(true);
                interactButton.enabled = true;
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
            isInDialogue = false;
            if (interactionPrompt != null)
            {
                interactionPrompt.gameObject.SetActive(false);
            }
            interactButton.enabled = false;
            FindObjectOfType<DialogueManager>().EndDialogue();
            StopAllCoroutines();
            cooldownActive = false;
        }
    }

    void Update()
    {
        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        bool isDialogueActive = dialogueManager != null && dialogueManager.IsDialogueActive();

        if (isInDialogue && !isDialogueActive)
        {
            isInDialogue = false;
            StartCoroutine(StartReinteractCooldown());
        }

        if (interactAction.IsPressed())
        {
            if (canInteract && !isInDialogue && !isDialogueActive && (!hasBeenUsed || !isSingleUse) && !cooldownActive)
            {
                TriggerInteraction();
            }
        }
    }

    private IEnumerator StartReinteractCooldown()
    {
        cooldownActive = true;
        
        if (interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(false);
        }
        
        yield return new WaitForSeconds(reinteractDelay);
        
        cooldownActive = false;
        
        if (interactionPrompt != null && canInteract && (!hasBeenUsed || !isSingleUse) && !isInDialogue)
        {
            interactionPrompt.gameObject.SetActive(true);
            interactButton.enabled = true;
        }
    }

    public void TriggerInteraction()
    {
        isInDialogue = true;
        if (interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(false);
        }

        if (requiresItem && !InventoryManager.Instance.HasItem(requiredItemName))
        {
            if (dialogueSettings.hasDialogue && dialogueSettings.beforeItemDialogue != null)
            {
                dialogueManager.StartDialogue(dialogueSettings.beforeItemDialogue);
            }
            return;
        }

        if (dialogueSettings.hasDialogue)
        {
            if (requiresItem && dialogueSettings.afterItemDialogue != null)
            {
                dialogueManager.StartDialogue(dialogueSettings.afterItemDialogue);
            }
            else if (!requiresItem && dialogueSettings.beforeItemDialogue != null)
            {
                dialogueManager.StartDialogue(dialogueSettings.beforeItemDialogue);
            }
        }

        onInteractionTriggered.Invoke();
        
        hasBeenUsed = true;
        if (hasBeenUsed && isSingleUse)
        {
            canInteract = false;
        }
    }
} 