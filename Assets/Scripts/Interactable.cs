using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Interactable : MonoBehaviour
{
    [SerializeField] private bool isInventoryItem = false;
    [SerializeField] private string itemName = "Item";
    [SerializeField] private Animator animator;
    [SerializeField] private string animationTriggerName = "Interact";
    [SerializeField] private TextMeshProUGUI interactionPrompt;
    [SerializeField] private string promptMessage = "Press E to interact";
    
    private bool hasInteracted = false;
    public bool canInteract = true;

    // Start is called before the first frame update
    void Start()
    {
        // Get animator component if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
            if (!hasInteracted)
            {
                interactionPrompt.text = promptMessage;
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

    public void Interact()
    {
        if (hasInteracted) return;

        // Play animation if there's an animator
        if (animator != null)
        {
            animator.SetTrigger(animationTriggerName);
        }

        // Add to inventory if it's an inventory item
        if (isInventoryItem)
        {
            // Add to inventory
            InventoryManager.Instance.AddItem(itemName);
            // Optionally destroy the object
            if (interactionPrompt != null)
            {
                interactionPrompt.gameObject.SetActive(false);
            }
            Destroy(gameObject);
        }

        hasInteracted = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
