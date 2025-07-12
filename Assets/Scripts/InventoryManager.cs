using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject itemPrefab;
    
    private List<string> inventory = new List<string>();
    private bool isInventoryOpen = false;
    private InputAction invAction;
    public PlayerProgress playerProgress;

    private void Awake()
    {
        invAction = InputSystem.actions.FindAction("Inventory");

        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (invAction.IsPressed())
        {
            ToggleInventory();
        }
    }

    public void AddItem(string itemName)
    {
        inventory.Add(itemName);
        Debug.Log($"Added {itemName} to inventory");
        UpdateInventoryUI();
        playerProgress.CollectItem(itemName);
    }

    public bool HasItem(string itemName)
    {
        return inventory.Contains(itemName);
    }

    public void RemoveItem(string itemName)
    {
        inventory.Remove(itemName);
        UpdateInventoryUI();
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        inventoryUI.SetActive(isInventoryOpen);
        UpdateInventoryUI();
    }

    private void UpdateInventoryUI()
    {
        if (!isInventoryOpen) return;

        // Clear existing items
        foreach (Transform child in itemContainer)
        {
            Destroy(child.gameObject);
        }

        // Create new item entries
        foreach (string item in inventory)
        {
            GameObject itemUI = Instantiate(itemPrefab, itemContainer);
            TextMeshProUGUI itemText = itemUI.GetComponentInChildren<TextMeshProUGUI>();
            if (itemText != null)
            {
                itemText.text = item;
            }
        }
    }
} 