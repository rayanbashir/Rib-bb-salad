using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    private static Canvas persistentInventoryCanvas; // Dedicated persistent canvas for Inventory UI only

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

    private List<Item> inventory = new List<Item>();
    private bool isInventoryOpen = false;
    private ItemType lastFilter = ItemType.Generic;
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
            // Re-parent inventory UI under a dedicated persistent canvas so the original scene canvas doesn't persist
            AttachInventoryUIToPersistentCanvas();
            // Persist only the Inventory UI object (not the original scene canvas hierarchy)
            if (inventoryUI != null)
                DontDestroyOnLoad(inventoryUI);
        }
        else
        {
            // A persistent InventoryManager already exists.
            // If this duplicate has its own inventory UI, destroy it to avoid duplicates.
            if (inventoryUI != null)
                Destroy(inventoryUI);
            Destroy(gameObject);
        }
    }

    private void AttachInventoryUIToPersistentCanvas()
    {
        if (inventoryUI == null) return;

        // Create or reuse a dedicated persistent canvas so we don't persist the scene's canvas
        if (persistentInventoryCanvas == null)
        {
            var existing = GameObject.Find("InventoryUI_PersistentCanvas");
            if (existing != null)
            {
                persistentInventoryCanvas = existing.GetComponent<Canvas>();
            }
            if (persistentInventoryCanvas == null)
            {
                var go = new GameObject("InventoryUI_PersistentCanvas");
                persistentInventoryCanvas = go.AddComponent<Canvas>();
                persistentInventoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                go.AddComponent<UnityEngine.UI.CanvasScaler>();
                go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                DontDestroyOnLoad(go);
            }
        }

        // Reparent inventory UI under the persistent canvas (preserve layout)
        if (inventoryUI.transform.parent != persistentInventoryCanvas.transform)
        {
            inventoryUI.transform.SetParent(persistentInventoryCanvas.transform, false);
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

    // Add a generic item with options
    public void AddGenericItem(string itemName, Sprite icon = null, string description = "")
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
        if (isInventoryOpen)
            ShowFilteredItems(lastFilter);
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
        // On default, show items only
        ShowFilteredItems(lastFilter);
    }

    public enum ItemType { All, Generic, Clue, Tool }

    public void ShowItemsOnly() {
        lastFilter = ItemType.Generic;
        ShowFilteredItems(ItemType.Generic);
    }
    public void ShowCluesOnly() {
        lastFilter = ItemType.Clue;
        ShowFilteredItems(ItemType.Clue);
    }
    public void ShowToolsOnly() {
        lastFilter = ItemType.Tool;
        ShowFilteredItems(ItemType.Tool);
    }

    private void ShowFilteredItems(ItemType type)
    {
        if (!isInventoryOpen) return;

        // Clear existing icons
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        // Filter items
        List<Item> filtered = new List<Item>();
        switch (type)
        {
            case ItemType.Generic:
                filtered = inventory.FindAll(i => !(i is Clue) && !(i is Tool));
                break;
            case ItemType.Clue:
                filtered = inventory.FindAll(i => i is Clue);
                break;
            case ItemType.Tool:
                filtered = inventory.FindAll(i => i is Tool);
                break;
            default:
                filtered = new List<Item>(inventory);
                break;
        }

        // Create new icon entries
        for (int i = 0; i < filtered.Count; i++)
        {
            int index = i; // local copy for lambda
            Item item = filtered[i];
            GameObject iconObj = Instantiate(iconPrefab, content);
            var iconImage = iconObj.GetComponent<UnityEngine.UI.Image>();
            if (iconImage != null)
                iconImage.sprite = item.icon;

            // Add button/select event
            var btn = iconObj.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => ShowFilteredItemDetails(filtered, index));
            }
        }

        // Show first item by default if any
        if (filtered.Count > 0)
            ShowFilteredItemDetails(filtered, 0);
        else
            ClearItemDetails();
    }

    private void ShowFilteredItemDetails(List<Item> filtered, int index)
    {
        if (index < 0 || index >= filtered.Count) { ClearItemDetails(); return; }
        Item item = filtered[index];
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
        // Removed itemAmountText
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
        // Removed itemAmountText
    }

    private void ClearItemDetails()
    {
        if (itemPanel != null) itemPanel.SetActive(false);
    }
}