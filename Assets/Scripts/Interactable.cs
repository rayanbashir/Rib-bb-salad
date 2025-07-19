using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Interactable : MonoBehaviour
{
    [Header("Inventory Item Settings")]
    [SerializeField] private bool isInventoryItem = false;
    [SerializeField] private string itemName = "Item";
    [SerializeField] private Sprite icon;
    [SerializeField] [TextArea] private string description;
    [SerializeField] private ItemType itemType = ItemType.Generic;
    [SerializeField] private string clueSource; // For Clue
    [SerializeField] private string toolType;   // For Tool

    public InventoryManager inventoryManager;

    private bool hasInteracted = false;
    public bool canInteract = true;

    public enum ItemType { Generic, Clue, Tool }

    void Start() { }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
            if (!hasInteracted)
            {
                Interact();
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
        }
    }

    public void Interact()
    {
        if (hasInteracted) return;

        if (isInventoryItem)
        {
            // Add the correct item type to inventory
            switch (itemType)
            {
                case ItemType.Clue:
                    inventoryManager.AddClue(itemName, clueSource, icon, description);
                    break;
                case ItemType.Tool:
                    inventoryManager.AddTool(itemName, toolType, icon, description);
                    break;
                default:
                    inventoryManager.AddItem(itemName, icon, description);
                    break;
            }
            Destroy(gameObject);
        }

        hasInteracted = true;
    }

    void Update() { }
}
