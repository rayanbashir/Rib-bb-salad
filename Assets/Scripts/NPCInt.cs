using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Serialization; // <- add this

public class NPCInt : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("Looping sound that plays while the player is in range, until they talk to this NPC for the first time.")]
    [SerializeField] private AudioClip proximityLoopClip;
    [Range(0f, 20f)] [SerializeField] private float proximityLoopVolume = 0.5f;
    [Tooltip("SFX played once when a conversation with this NPC starts.")]
    [SerializeField] private AudioClip conversationStartClip;
    [Range(0f, 1f)] [SerializeField] private float conversationStartVolume = 1f;
    [Tooltip("Optional dedicated AudioSource for the loop. If not assigned and a clip is set, one will be created at runtime.")]
    [SerializeField] private AudioSource loopSource;
    [Tooltip("Optional dedicated AudioSource for SFX/one-shots. If not assigned, one will be created at runtime.")]
    [SerializeField] private AudioSource sfxSource;
    [Header("Proximity Loop Settings")]
    [Tooltip("If true, the loop starts at scene load (before entering range) and keeps playing until you talk to the NPC once.")]
    [SerializeField] private bool startLoopOnStart = true;
    [Range(0f, 1f)] [SerializeField] private float nearVolumeScale = 1f;
    [Range(0f, 1f)] [SerializeField] private float farVolumeScale = 0.35f;
    [Range(0f, 1f)] [SerializeField] private float loopSpatialBlend = 1f;
    [SerializeField] private float loopMinDistance = 2f;
    [SerializeField] private float loopMaxDistance = 15f;
    [SerializeField] private AudioRolloffMode loopRolloffMode = AudioRolloffMode.Logarithmic;

    [Header("Post-Dialogue Event")] 
    [Tooltip("If set, this event will trigger after a dialogue ends when the dialogue's name matches triggerEventDialogueName.")]
    public UnityEvent onDialogueComplete;

    [Tooltip("Dialogue name that should trigger the post-dialogue event. Leave empty to disable.")]
    [FormerlySerializedAs("triggerEventNpcName")]
    public string triggerEventDialogueName;
    [Space(4)]
    [Tooltip("Optional second event that will trigger after a dialogue with this name ends.")]
    public UnityEvent onSecondaryDialogueComplete;
    [Tooltip("Dialogue name that should trigger the secondary post-dialogue event. Leave empty to disable.")]
    public string secondaryTriggerDialogueName;
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

    void Awake()
    {
        // Auto-provision audio sources if needed
        if (loopSource == null && proximityLoopClip != null)
        {
            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.playOnAwake = false;
            loopSource.loop = true;
            loopSource.spatialBlend = loopSpatialBlend; // 3D by default; adjust in inspector as needed
            loopSource.rolloffMode = loopRolloffMode;
            loopSource.minDistance = loopMinDistance;
            loopSource.maxDistance = loopMaxDistance;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 1f;
        }
    }

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

        // Configure loop source in case it was assigned in inspector
        if (loopSource != null)
        {
            loopSource.spatialBlend = loopSpatialBlend;
            loopSource.rolloffMode = loopRolloffMode;
            loopSource.minDistance = loopMinDistance;
            loopSource.maxDistance = loopMaxDistance;
        }

        // Start loop at scene load (quietly at distance) if configured and not yet spoken
        if (startLoopOnStart)
        {
            TryStartProximityLoop();
            SetLoopVolumeForRange(false); // Assume out of range initially
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

            // Start proximity loop if applicable (only if the player hasn't spoken yet)
            TryStartProximityLoop();
            SetLoopVolumeForRange(true);
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
            // Reduce loop volume when leaving range, but keep it playing quietly until first conversation
            SetLoopVolumeForRange(false);
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
                // Stop loop immediately on initiating interaction
                StopProximityLoop();
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
        
        // Stop loop in case it wasn't already stopped, and play the conversation start SFX
        StopProximityLoop();
        PlayConversationStartSfx();

        // Mark as spoken once always so the proximity loop won't return after first talk
        hasSpokenOnce = true;

        dialogueManager.StartDialogue(currentDialogue, this);
        Debug.Log("Triggered dialogue with " + gameObject.name);
    }

    private IEnumerator DelayedTriggerDialogue()
    {
        yield return new WaitForSeconds(0.3f);
        TriggerDialogue();
    }

    private void TryStartProximityLoop()
    {
        if (hasSpokenOnce) return; // Do not play after first conversation if one-time
        if (proximityLoopClip == null) return;
        if (loopSource == null) return;

        if (!loopSource.isPlaying)
        {
            loopSource.clip = proximityLoopClip;
            loopSource.volume = proximityLoopVolume * farVolumeScale;
            loopSource.loop = true;
            loopSource.Play();
        }
    }

    private void StopProximityLoop()
    {
        if (loopSource != null && loopSource.isPlaying)
        {
            loopSource.Stop();
            loopSource.clip = null; // Optional: clear clip so other SFX can't accidentally stop it
        }
    }

    private void SetLoopVolumeForRange(bool inRange)
    {
        if (loopSource == null) return;
        float scale = inRange ? nearVolumeScale : farVolumeScale;
        loopSource.volume = proximityLoopVolume * Mathf.Clamp01(scale);
    }

    private void PlayConversationStartSfx()
    {
        if (conversationStartClip == null || sfxSource == null) return;
        sfxSource.volume = conversationStartVolume;
        sfxSource.PlayOneShot(conversationStartClip, conversationStartVolume);
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
        // Hook left in case DialogueManager triggers this differently; ensure loop is stopped.
        StopProximityLoop();
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

        // Post-dialogue event logic (match on dialogue name, not GameObject name)
        bool shouldInvoke =
            !string.IsNullOrEmpty(triggerEventDialogueName) &&
            endedDialogue != null &&
            string.Equals(endedDialogue.name, triggerEventDialogueName, System.StringComparison.OrdinalIgnoreCase);

        if (shouldInvoke)
        {
            onDialogueComplete?.Invoke();
            Debug.Log($"Post-dialogue event triggered for dialogue: {endedDialogue.name} (NPC: {gameObject.name})");
        }

        // Secondary event logic
        bool shouldInvokeSecondary =
            !string.IsNullOrEmpty(secondaryTriggerDialogueName) &&
            endedDialogue != null &&
            string.Equals(endedDialogue.name, secondaryTriggerDialogueName, System.StringComparison.OrdinalIgnoreCase);

        if (shouldInvokeSecondary)
        {
            onSecondaryDialogueComplete?.Invoke();
            Debug.Log($"Secondary post-dialogue event triggered for dialogue: {endedDialogue.name} (NPC: {gameObject.name})");
        }
    }
}

