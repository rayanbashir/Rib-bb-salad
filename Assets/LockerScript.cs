using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockerScript : MonoBehaviour
{
    public GameObject interactMark;
    public Animator lockerAnimator;
    public Animator markAnimator;
    private bool canInteract = false;
    private bool isPlayerInLocker = false;
    private GameObject player;
    private Collider2D playerCollider;
    private SpriteRenderer playerSprite;
    private Animator playerAnimator;
    private UnityEngine.InputSystem.InputAction interactAction;

    [SerializeField] private string enterLockerTrigger = "enterLocker";
    [SerializeField] private string exitLockerTrigger = "exit";

    void Start()
    {
        if (lockerAnimator != null)
            lockerAnimator.SetBool("inRange", false);
        if (markAnimator != null)
            markAnimator.SetBool("inRange", false);
        if (interactMark != null)
            interactMark.SetActive(false);
        interactAction = UnityEngine.InputSystem.InputSystem.actions.FindAction("Interact");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
            if (interactMark != null && !isPlayerInLocker)
                interactMark.SetActive(true);
            if (lockerAnimator != null)
                lockerAnimator.SetBool("inRange", true);
            if (markAnimator != null)
                markAnimator.SetBool("inRange", true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (lockerAnimator != null)
                lockerAnimator.SetBool("inRange", false);
            if (markAnimator != null)
                markAnimator.SetBool("inRange", false);
            canInteract = false;
            if (interactMark != null)
                interactMark.SetActive(false);
        }
    }

    void Update()
    {
        if (canInteract && !isPlayerInLocker && interactAction != null && interactAction.IsPressed())
        {
            InteractLocker();
        }

        if (isPlayerInLocker && PlayerIsTryingToMove())
        {
            ExitLocker();
        }
    }

    private void InteractLocker()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        playerCollider = player.GetComponent<Collider2D>();
        playerSprite = player.GetComponent<SpriteRenderer>();
        playerAnimator = player.GetComponent<Animator>();

        if (lockerAnimator != null) lockerAnimator.SetTrigger(enterLockerTrigger);
        StartCoroutine(HidePlayerAfterDelay(0.5f));

        isPlayerInLocker = true;
        if (interactMark != null)
            interactMark.SetActive(false);
    }

    private IEnumerator HidePlayerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (playerCollider != null) playerCollider.enabled = false;
        if (playerSprite != null) playerSprite.enabled = false;
        // Disable movement via Movement script
        var movement = player != null ? player.GetComponent<Movement>() : null;
        if (movement != null) movement.canMove = false;
    }

    private void ExitLocker()
    {
        if (lockerAnimator != null) lockerAnimator.SetTrigger(exitLockerTrigger);
        StartCoroutine(ShowPlayerAfterDelay(0.5f));
        isPlayerInLocker = false;
        if (canInteract && interactMark != null)
            interactMark.SetActive(true);
    }

    private IEnumerator ShowPlayerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (playerCollider != null) playerCollider.enabled = true;
        if (playerSprite != null) playerSprite.enabled = true;
        // Re-enable movement via Movement script
        var movement = player != null ? player.GetComponent<Movement>() : null;
        if (movement != null) movement.canMove = true;
    }

    private bool PlayerIsTryingToMove()
    {
        var movementAction = UnityEngine.InputSystem.InputSystem.actions.FindAction("Movement");
        if (movementAction != null)
        {
            Vector2 move = movementAction.ReadValue<Vector2>();
            return move.magnitude > 0.1f;
        }
        return false;
    }
}
