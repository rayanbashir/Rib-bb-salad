using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using UnityEngine.Events;

public class NPCInt : MonoBehaviour
{
    [Header("Post-Dialogue Event")] 
    [Tooltip("If set, this event will trigger after dialogue ends, but only if the NPC's name matches triggerEventNpcName.")]
    public UnityEvent onDialogueComplete;
    [Tooltip("The NPC name that will trigger the post-dialogue event. Leave empty to disable.")]
    public string triggerEventNpcName;
    public GameObject interactMark;
    public Animator animator;
    public DialogueManager dialogueManager;
    private bool canInteract;
    public Dialogue dialogue;
    private Dialogue currentDialogue; // Track current dialogue state
    private bool hasInteracted = false;
    public bool allowDialogueChanges = false; // Toggle for dialogue change feature
    public bool oneTimeOnly = false; // Toggle to allow only one conversation
    private bool hasSpokenOnce = false; // Track if player has already spoken to this NPC
    
    [Header("Item Reward System")]
    public bool givesItemAfterDialogue = false; // Toggle to give item after dialogue
    [Tooltip("If set, the item will only be given after the dialogue with this Name finishes (e.g., the one reached after selecting a specific option). Leave empty to give after the next dialogue end.")]
    public string itemRewardDialogueName; // Name of the dialogue that triggers the reward
    public string itemToGive = ""; // Name of item to give
    public Sprite itemIcon; // Icon for the item
    [TextArea(2,4)]
    public string itemDescription = ""; // Description of the item
    public ItemRewardType itemType = ItemRewardType.Generic; // Type of item to give
    public string itemSource = ""; // Source for clues
    public string toolType = ""; // Tool type for tools
    private bool hasGivenItem = false; // Track if item has been given
    
    public enum ItemRewardType { Generic, Clue, Tool }
    
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
            // Don't allow interaction if it's one-time only and already spoken
            if (oneTimeOnly && hasSpokenOnce)
            {
                return;
            }
            
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
            dialogueManager.EndDialogue();
        }
    }

    void Update()
    {
        bool isDialogueActive = dialogueManager.IsDialogueActive();

        if (interactAction.IsPressed())
        {
            // Don't allow interaction if it's one-time only and already spoken
            if (canInteract && !isInDialogue && !isDialogueActive && !(oneTimeOnly && hasSpokenOnce))
            {
                StartCoroutine(DelayedTriggerDialogue());
                Debug.Log("Interact pressed");
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

    public void ResetOneTimeInteraction()
    {
        hasSpokenOnce = false;
        Debug.Log("Reset one-time interaction for " + gameObject.name);
    }

    public void ResetItemReward()
    {
        hasGivenItem = false;
        Debug.Log("Reset item reward for " + gameObject.name);
    }

    public void ResetAll()
    {
        ResetDialogue();
        ResetOneTimeInteraction();
        ResetItemReward();
        Debug.Log("Reset all states for " + gameObject.name);
    }

    public void TriggerDialogue()
    {
        isInDialogue = true;
        if (interactMark != null)
        {
            interactMark.SetActive(false);
        }
        
        // Mark as spoken if it's one-time only
        if (oneTimeOnly)
        {
            hasSpokenOnce = true;
        }

        dialogueManager.StartDialogue(currentDialogue, this);
        Debug.Log("Triggered dialogue with " + gameObject.name);
    }

    private IEnumerator DelayedTriggerDialogue()
    {
        yield return new WaitForSeconds(0.3f);
        TriggerDialogue();
    }

    private void GiveItemToPlayer()
    {
        if (hasGivenItem || string.IsNullOrEmpty(itemToGive))
            return;

        InventoryManager inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            Debug.LogError("InventoryManager not found!");
            return;
        }

        // Add item based on type
        switch (itemType)
        {
            case ItemRewardType.Clue:
                inventoryManager.AddClue(itemToGive, itemSource, itemIcon, itemDescription);
                Debug.Log($"NPC {gameObject.name} gave clue: {itemToGive}");
                break;
            case ItemRewardType.Tool:
                inventoryManager.AddTool(itemToGive, toolType, itemIcon, itemDescription);
                Debug.Log($"NPC {gameObject.name} gave tool: {itemToGive}");
                break;
            default:
                inventoryManager.AddItem(itemToGive, itemIcon, itemDescription);
                Debug.Log($"NPC {gameObject.name} gave item: {itemToGive}");
                break;
        }

        hasGivenItem = true;
    }

    // Called by DialogueManager when a dialogue starts for this NPC
    public void OnDialogueStarted(Dialogue startedDialogue)
    {
        // No-op for now; left for future use if we want to trigger on start
    }

    // Called by DialogueManager when a dialogue ends for this NPC
    public void OnDialogueEnded(Dialogue endedDialogue)
    {
        // Item reward logic (unchanged)
        if (givesItemAfterDialogue && !hasGivenItem && !string.IsNullOrEmpty(itemToGive))
        {
            if (!string.IsNullOrEmpty(itemRewardDialogueName))
            {
                if (endedDialogue != null && endedDialogue.name == itemRewardDialogueName)
                {
                    GiveItemToPlayer();
                }
            }
            else
            {
                GiveItemToPlayer();
            }
        }

        // Post-dialogue event logic
        if (!string.IsNullOrEmpty(triggerEventNpcName) && gameObject.name == triggerEventNpcName)
        {
            if (onDialogueComplete != null)
            {
                onDialogueComplete.Invoke();
                Debug.Log($"Post-dialogue event triggered for NPC: {gameObject.name}");
            }
        }
    }
}

