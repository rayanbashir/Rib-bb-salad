using UnityEngine;
using System.Collections;
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

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (!isActive) return; // If the door is not active, do nothing

            if (isLocked)
            {
                // Check if player has lockpick tool
                // Assume lockpick tool type is "Lockpick" (change if needed)
                bool hasLockpick = false;
                var inventory = InventoryManager.Instance;
                if (inventory != null)
                {
                    // Look for a Tool with ToolType == "Lockpick"
                    hasLockpick = inventoryHasLockpick();
                }
                if (hasLockpick)
                {
                    // Start lockpick minigame
                    LockpickGame lockpickGame = FindObjectOfType<LockpickGame>();
                    if (lockpickGame != null)
                    {
                        lockpickGame.StartGame();
                    }
                    else
                    {
                        Debug.LogWarning("No LockpickGame found in scene!");
                    }
                }
                else
                {
                    // Show dialogue: Door is locked
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

            StartCoroutine(TeleportPlayer(other.transform, 1.3f));
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

    IEnumerator TeleportPlayer(Transform player, float delayTime)
    {
        playerMovement = player.GetComponent<Movement>();
        if (destination != null)
        {
            animator.SetTrigger("FadeOut");
            playerMovement.canMove = false;
            yield return new WaitForSeconds(delayTime);
            Debug.Log("Baaaaaaakaaaaaaaaaaaaaaaa");
            Vector2 targetPosition = destination.position;
            targetPosition.y += (verticalOffset);
            targetPosition.x += (horizontalOffset);

            player.position = targetPosition;

            yield return new WaitForSeconds(delayTime);
            animator.SetTrigger("FadeIn");

            Invoke("EnablePlayerMovement", 0.4f);
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