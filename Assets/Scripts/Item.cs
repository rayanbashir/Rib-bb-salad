using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Item
{
    public string itemName;
    public Sprite icon;
    public string description;
    public int stackAmount = 1;
    public bool stackable = false;

    public Item(string name, Sprite icon = null, string description = "", int stackAmount = 1, bool stackable = false)
    {
        this.itemName = name;
        this.icon = icon;
        this.description = description;
        this.stackAmount = stackAmount;
        this.stackable = stackable;
    }
}

public class Clue : Item
{
    public string Source;

    public Clue(string name, string source, Sprite icon = null, string description = "", int stackAmount = 1, bool stackable = false)
        : base(name, icon, description, stackAmount, stackable)
    {
        this.Source = source;
    }
}

public class Tool : Item
{
    public string ToolType;

    public Tool(string name, string toolType, Sprite icon = null, string description = "", int stackAmount = 1, bool stackable = false)
        : base(name, icon, description, stackAmount, stackable)
    {
        this.ToolType = toolType;
    }
}