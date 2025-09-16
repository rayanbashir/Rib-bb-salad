using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
public class DoorTeleport : MonoBehaviour
{
    public Transform destination;    // The destination transform where player will teleport
    public float verticalOffset = -1f; // Optional offset to place player slightly in front of destination
    public float horizontalOffset = -0.5f;
    public float exitDirection = -180f; // 0 = up, 90 = right, 180 = down, -90 = left
    public Movement playerMovement;
    public Animator animator;

    public bool isActive = true; // Flag to check if the door is active
    public bool isLocked = false; // If true, requires lockpick minigame

    [Header("Scene Load Option")]
    public bool loadSceneInstead = false; // If true, load a scene instead of teleporting
    public string sceneName;              // Scene to load when using scene load option
    public float transitionDelay = 1.3f;  // Delay before teleport/scene load (matches fade)

    [Header("Item Requirement (separate from lockpick)")]
    public bool requiresItem = false;     // If true, requires an item to use the door
    public string requiredItemName;       // Name of the required item
    public bool consumeRequiredItem = false; // If true, remove the item on use

    // Internals to continue action after lockpick
    private Transform pendingPlayer;
    private LockpickGame currentLockpickGame;

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (!isActive) return;

            // Enforce item requirement first (separate from lockpick)
            if (requiresItem)
            {
                var inventory = InventoryManager.Instance;
                bool hasItem = inventory != null && !string.IsNullOrEmpty(requiredItemName) && inventory.HasItem(requiredItemName);
                if (!hasItem)
                {
                    DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
                    if (dialogueManager != null)
                    {
                        string msg = string.IsNullOrEmpty(requiredItemName) ? "You can't enter yet." : $"You need {requiredItemName} to enter.";
                        Dialogue needItemDialogue = new Dialogue {
                            name = "",
                            sentences = new string[] { msg },
                            LockPlayerMovement = true
                        };
                        dialogueManager.StartDialogue(needItemDialogue);
                    }
                    else
                    {
                        Debug.Log($"Door requires item: {requiredItemName}");
                    }
                    return;
                }
                else if (consumeRequiredItem)
                {
                    inventory.RemoveItemByName(requiredItemName);
                }
            }

            if (isLocked)
            {
                bool hasLockpick = false;
                var inventory = InventoryManager.Instance;
                if (inventory != null)
                {
                    hasLockpick = inventoryHasLockpick();
                }
                if (hasLockpick)
                {
                    LockpickGame lockpickGame = FindObjectOfType<LockpickGame>();
                    if (lockpickGame != null)
                    {
                        // Subscribe to success event
                        currentLockpickGame = lockpickGame;
                        pendingPlayer = other.transform;
                        currentLockpickGame.OnLockpickSuccess += UnlockDoor;
                        currentLockpickGame.StartGame();
                    }
                    else
                    {
                        Debug.LogWarning("No LockpickGame found in scene!");
                    }
                }
                else
                {
                    DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
                    if (dialogueManager != null)
                    {
                        Dialogue lockedDialogue = new Dialogue {
                            name = "",
                            sentences = new string[] { "Door is locked" },
                            LockPlayerMovement = true
                        };
                        dialogueManager.StartDialogue(lockedDialogue);
                    }
                    else
                    {
                        Debug.Log("Player needs a lockpick tool to open this door.");
                    }
                }
                return;
            }

            // Not locked, proceed immediately
            StartCoroutine(TeleportOrLoad(other.transform));
        }
    }

    // Unlocks the door when lockpick game is successful
    private void UnlockDoor()
    {
        isLocked = false;
        Debug.Log("Door unlocked!");
        // Unsubscribe to avoid duplicate calls
        if (currentLockpickGame != null)
        {
            currentLockpickGame.OnLockpickSuccess -= UnlockDoor;
        }
        // Proceed with the pending action if we have a player reference
        if (pendingPlayer != null)
        {
            StartCoroutine(TeleportOrLoad(pendingPlayer));
            pendingPlayer = null;
        }
    }

    // Helper to check for lockpick tool in inventory
    private bool inventoryHasLockpick()
    {
        var inventory = InventoryManager.Instance;
        if (inventory == null) return false;
        // Use HasItem with the lockpick tool's itemName (assumed "Lockpick")
        return inventory.HasItem("Lockpick");
    }

    IEnumerator TeleportOrLoad(Transform player)
    {
        playerMovement = player.GetComponent<Movement>();
        if (animator != null) animator.SetTrigger("FadeOut");
        if (playerMovement != null) playerMovement.canMove = false;
        yield return new WaitForSeconds(transitionDelay);

        if (loadSceneInstead)
        {
            if (!string.IsNullOrEmpty(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning("Scene name not set on DoorTeleport.");
                // If scene name is missing, re-enable movement to avoid soft lock
                Invoke("EnablePlayerMovement", 0.4f);
            }
        }
        else
        {
            if (destination != null)
            {
                Vector2 targetPosition = destination.position;
                targetPosition.y += (verticalOffset);
                targetPosition.x += (horizontalOffset);

                player.position = targetPosition;

                yield return new WaitForSeconds(transitionDelay);
                if (animator != null) animator.SetTrigger("FadeIn");
                Invoke("EnablePlayerMovement", 0.4f);
            }
            else
            {
                Debug.LogWarning("Destination not set on DoorTeleport.");
                if (animator != null) animator.SetTrigger("FadeIn");
                Invoke("EnablePlayerMovement", 0.4f);
            }
        }
    }
    void EnablePlayerMovement()
    {
        playerMovement.canMove = true;
    }
    
    public void ActivateDoor()
    {
        isActive = true;
    }

}