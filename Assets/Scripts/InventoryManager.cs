using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory UI References")]
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private Transform content; // Content under InventoryContent/Viewport/Content
    [SerializeField] private GameObject iconPrefab; // Prefab for item icon in inventory

    [Header("Selected Item Panel References")]
    [SerializeField] private GameObject itemPanel; // ItemPanel in hierarchy
    [SerializeField] private UnityEngine.UI.Image itemImage;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI itemSourceText;
    [SerializeField] private TextMeshProUGUI itemAmountText;

    private List<Item> inventory = new List<Item>();
    private bool isInventoryOpen = false;
    private InputAction inventoryAction;
    public PlayerProgress playerProgress;

    private void Awake()
    {
        inventoryAction = InputSystem.actions.FindAction("Inventory");
        if (inventoryAction != null)
            inventoryAction.Enable();

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

    private void OnEnable()
    {
        if (inventoryAction != null)
            inventoryAction.performed += OnInventoryPerformed;
    }

    private void OnDisable()
    {
        if (inventoryAction != null)
            inventoryAction.performed -= OnInventoryPerformed;
    }

    private void OnInventoryPerformed(InputAction.CallbackContext context)
    {
        ToggleInventory();
    }

    // Add a generic Item
    public void AddItem(Item item)
    {
        // Stacking logic
        if (item.stackable)
        {
            var existing = inventory.Find(i => i.itemName == item.itemName && i.stackable);
            if (existing != null)
            {
                existing.stackAmount += item.stackAmount;
                UpdateInventoryUI();
                playerProgress?.CollectItem(item.itemName);
                return;
            }
        }
        inventory.Add(item);
        Debug.Log($"Added {item.itemName} to inventory");
        UpdateInventoryUI();
        playerProgress?.CollectItem(item.itemName);
    }

    // Add by name (for legacy support)
    public void AddItem(string itemName, Sprite icon = null, string description = "", int stackAmount = 1, bool stackable = false)
    {
        Item newItem = new Item(itemName, icon, description, stackAmount, stackable);
        AddItem(newItem);
    }

    // Add a generic item with options
    public void AddGenericItem(string itemName, Sprite icon = null, string description = "", bool stackable = false, int stackAmount = 1)
    {
        Item newItem = new Item(itemName, icon, description, stackAmount, stackable);
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

        // Lock/unlock player movement
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var movement = player.GetComponent<Movement>();
            if (movement != null)
                movement.enabled = !isInventoryOpen;
        }
    }

    private void UpdateInventoryUI()
    {
        if (!isInventoryOpen) return;

        // Clear existing icons
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        // Create new icon entries
        for (int i = 0; i < inventory.Count; i++)
        {
            int index = i; // local copy for lambda
            Item item = inventory[i];
            GameObject iconObj = Instantiate(iconPrefab, content);
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
        if (itemAmountText != null)
        {
            itemAmountText.text = $"x{item.stackAmount}";
        }
    }

    private void ClearItemDetails()
    {
        if (itemPanel != null) itemPanel.SetActive(false);
    }
}