using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Button interactButton;
    private InputAction interactAction;
    private InputAction movementAction;
    private InputAction inventoryAction;

    private bool isInteractingWithInventory = false;

    void Awake()
    {
        // Get the actions from the Input System
        interactAction = InputSystem.actions.FindAction("Interact");
        movementAction = InputSystem.actions.FindAction("Movement");
        inventoryAction = InputSystem.actions.FindAction("Inventory");
    }

    void Update()
    {
        // Check if player is moving or interacting with inventory
        bool isMoving = movementAction.ReadValue<Vector2>().magnitude > 0;
        bool isInventoryOpen = inventoryAction.IsPressed();

        // Disable interact action if player is moving or interacting with inventory
        if (isMoving || isInventoryOpen)
        {
            interactAction.Disable();
        }
        else
        {
            interactAction.Enable();
        }

        // Only check for interact when interact action is enabled
        if (interactAction.enabled && interactAction.IsPressed())
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 2f);
            foreach (Collider2D collider in colliders)
            {
                Interactable interactable = collider.GetComponent<Interactable>();
                if (interactable != null)
                {
                    Debug.Log("Interacted with " + interactable.name);
                    interactable.Interact();
                    break;
                }
            }
        }
    }
}
