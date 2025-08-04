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
    [SerializeField] private bool consumeRequiredItem = false; // Whether to consume the item when used
    [SerializeField] private bool isSingleUse = true;

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
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && (!hasBeenUsed || !isSingleUse))
        {
            canInteract = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
            isInDialogue = false;
            FindObjectOfType<DialogueManager>().EndDialogue();
            StopAllCoroutines();
            cooldownActive = false;
        }
    }

    void Update()
    {
        // Cache dialogue manager reference for efficiency
        if (dialogueManager == null)
            dialogueManager = FindObjectOfType<DialogueManager>();
            
        bool isDialogueActive = dialogueManager != null && dialogueManager.IsDialogueActive();

        // Handle dialogue ending
        if (isInDialogue && !isDialogueActive)
        {
            isInDialogue = false;
            StartCoroutine(StartReinteractCooldown());
        }

        // Handle interaction input
        if (interactAction.IsPressed())
        {
            if (canInteract && !isInDialogue && !isDialogueActive && (!hasBeenUsed || !isSingleUse) && !cooldownActive)
            {
                // Additional check for required item before triggering
                if (requiresItem && !HasRequiredItem())
                {
                    // Show feedback that item is missing (could play sound effect here)
                    Debug.Log($"Cannot interact: Missing required item '{requiredItemName}'");
                    return;
                }
                
                TriggerInteraction();
            }
        }
    }

    private bool HasRequiredItem()
    {
        if (string.IsNullOrEmpty(requiredItemName))
            return true;
            
        return InventoryManager.Instance.HasItem(requiredItemName);
    }

    private IEnumerator StartReinteractCooldown()
    {
        cooldownActive = true;
        
        yield return new WaitForSeconds(reinteractDelay);
        
        cooldownActive = false;
    }

    public void TriggerInteraction()
    {
        isInDialogue = true;

        // Check if required item is available
        if (requiresItem && !HasRequiredItem())
        {
            if (dialogueSettings.hasDialogue && dialogueSettings.beforeItemDialogue != null)
            {
                dialogueManager.StartDialogue(dialogueSettings.beforeItemDialogue);
            }
            return;
        }

        // Consume required item if specified
        if (requiresItem && consumeRequiredItem && HasRequiredItem())
        {
            InventoryManager.Instance.RemoveItemByName(requiredItemName);
            Debug.Log($"Consumed item: {requiredItemName}");
        }

        // Show appropriate dialogue
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

        // Trigger the interaction event
        onInteractionTriggered.Invoke();
        
        // Mark as used for single-use items
        hasBeenUsed = true;
        if (hasBeenUsed && isSingleUse)
        {
            canInteract = false;
        }
    }

    public void ResetInteraction()
    {
        hasBeenUsed = false;
        canInteract = true;
        isInDialogue = false;
        cooldownActive = false;
        Debug.Log("Reset interaction state for " + gameObject.name);
    }
} 