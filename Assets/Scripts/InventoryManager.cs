using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory UI References")]
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private Transform inventoryContent; // InventoryContent in hierarchy
    [SerializeField] private GameObject iconPrefab; // Prefab for item icon in inventory

    [Header("Selected Item Panel References")]
    [SerializeField] private GameObject itemPanel; // ItemPanel in hierarchy
    [SerializeField] private UnityEngine.UI.Image itemImage;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI itemSourceText;

    private List<Item> inventory = new List<Item>();
    private bool isInventoryOpen = false;
    private InputAction interactAction;
    public PlayerProgress playerProgress;

    private void Awake()
    {
        interactAction = InputSystem.actions.FindAction("Interact");

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
        if (interactAction != null && interactAction.WasPressedThisFrame())
        {
            ToggleInventory();
        }
    }

    // Add a generic Item
    public void AddItem(Item item)
    {
        inventory.Add(item);
        Debug.Log($"Added {item.itemName} to inventory");
        UpdateInventoryUI();
        playerProgress?.CollectItem(item.itemName);
    }

    // Add by name (for legacy support)
    public void AddItem(string itemName, Sprite icon = null, string description = "")
    {
        Item newItem = new Item(itemName, icon, description);
        AddItem(newItem);
    }

    // Add a Clue
    public void AddClue(string name, string source, Sprite icon = null, string description = "")
    {
        Clue newClue = new Clue(name, source, icon, description);
        AddItem(newClue);
    }

    // Add a Tool
    public void AddTool(string name, string toolType, Sprite icon = null, string description = "")
    {
        Tool newTool = new Tool(name, toolType, icon, description);
        AddItem(newTool);
    }

    public bool HasItem(string itemName)
    {
        return inventory.Exists(i => i.itemName == itemName);
    }

    public void RemoveItem(Item item)
    {
        inventory.Remove(item);
        UpdateInventoryUI();
    }

    public void RemoveItemByName(string itemName)
    {
        var item = inventory.Find(i => i.itemName == itemName);
        if (item != null)
        {
            RemoveItem(item);
        }
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

        // Clear existing icons
        foreach (Transform child in inventoryContent)
        {
            Destroy(child.gameObject);
        }

        // Create new icon entries
        for (int i = 0; i < inventory.Count; i++)
        {
            int index = i; // local copy for lambda
            Item item = inventory[i];
            GameObject iconObj = Instantiate(iconPrefab, inventoryContent);
            var iconImage = iconObj.GetComponent<UnityEngine.UI.Image>();
            if (iconImage != null)
                iconImage.sprite = item.icon;

            // Add button/select event
            var btn = iconObj.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => ShowItemDetails(index));
            }
        }

        // Show first item by default if any
        if (inventory.Count > 0)
            ShowItemDetails(0);
        else
            ClearItemDetails();
    }

    private void ShowItemDetails(int index)
    {
        if (index < 0 || index >= inventory.Count) { ClearItemDetails(); return; }
        Item item = inventory[index];
        if (itemPanel != null) itemPanel.SetActive(true);
        if (itemImage != null) itemImage.sprite = item.icon;
        if (itemNameText != null) itemNameText.text = item.itemName;
        if (itemDescriptionText != null) itemDescriptionText.text = string.IsNullOrEmpty(item.description) ? "" : item.description;
        if (itemSourceText != null)
        {
            if (item is Clue clue)
                itemSourceText.text = $"Source: {clue.Source}";
            else if (item is Tool tool)
                itemSourceText.text = $"Type: {tool.ToolType}";
            else
                itemSourceText.text = "";
        }
    }

    private void ClearItemDetails()
    {
        if (itemPanel != null) itemPanel.SetActive(false);
    }
}