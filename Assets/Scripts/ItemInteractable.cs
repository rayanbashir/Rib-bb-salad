using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ItemInteractable : MonoBehaviour
{
    [Header("Item Requirements")]
    [SerializeField] private bool requiresItem = false;
    [SerializeField] private string requiredItemName;

    [Header("UI")]
    [SerializeField] private Button interactButton;
    [SerializeField] private TextMeshProUGUI interactionPrompt;
    [SerializeField] private string defaultPromptMessage = "Press E to interact";
    [SerializeField] private string itemPromptMessage = "Press E to use {0}";
    
    [Header("Dialogue")]
    [SerializeField] private ItemInteractableDialogue dialogueSettings;
    
    public UnityEvent onInteractionTriggered;
    
    private bool canInteract = false;
    private bool hasBeenUsed = false;
    private DialogueManager dialogueManager;
    private bool isInDialogue = false;
    private InputAction interactAction;

    void Awake()
    {
        interactAction = InputSystem.actions.FindAction("Interact");
        dialogueManager = FindObjectOfType<DialogueManager>();
        if (interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Check if dialogue state has changed
        bool currentDialogueState = dialogueManager.IsDialogueActive();
        bool isDialogueActive = dialogueManager != null && dialogueManager.IsDialogueActive();
        if (isInDialogue != currentDialogueState)
        {
            isInDialogue = currentDialogueState;
            UpdatePromptVisibility();
        }
        if (interactAction.WasPressedThisFrame())
        {
            if (canInteract && !isInDialogue && !isDialogueActive)
            {
                TriggerInteraction();
            }
        }
    }

    void UpdatePromptVisibility()
    {
        if (interactionPrompt != null && canInteract && !hasBeenUsed)
        {
            interactionPrompt.gameObject.SetActive(!isInDialogue);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasBeenUsed)
        {
            canInteract = true;
            
            if (interactionPrompt != null && !isInDialogue)
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
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
            if (interactionPrompt != null)
            {
                interactionPrompt.gameObject.SetActive(false);
            }
        }
    }

    void TriggerInteraction()
    {
        if (requiresItem && !InventoryManager.Instance.HasItem(requiredItemName))
        {
            if (dialogueSettings.hasDialogue && dialogueSettings.beforeItemDialogue != null)
            {
                dialogueManager.StartDialogue(dialogueSettings.beforeItemDialogue);
                isInDialogue = true;
                UpdatePromptVisibility();
            }
            return;
        }

        if (dialogueSettings.hasDialogue)
        {
            if (requiresItem && dialogueSettings.afterItemDialogue != null)
            {
                dialogueManager.StartDialogue(dialogueSettings.afterItemDialogue);
                isInDialogue = true;
            }
            else if (!requiresItem && dialogueSettings.beforeItemDialogue != null)
            {
                dialogueManager.StartDialogue(dialogueSettings.beforeItemDialogue);
                isInDialogue = true;
            }
            UpdatePromptVisibility();
        }

        onInteractionTriggered.Invoke();
        hasBeenUsed = true;
        canInteract = false;
        
        if (interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(false);
        }
    }
} 