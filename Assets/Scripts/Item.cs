using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Item
{
    public string itemName;
    public Sprite icon;
    public string description;

    public Item(string name, Sprite icon = null, string description = "")
    {
        this.itemName = name;
        this.icon = icon;
        this.description = description;
    }
}

public class Clue : Item
{
    public string Source;

    public Clue(string name, string source, Sprite icon = null, string description = "")
        : base(name, icon, description)
    {
        this.Source = source;
    }
}

public class Tool : Item
{
    public string ToolType;

    public Tool(string name, string toolType, Sprite icon = null, string description = "")
        : base(name, icon, description)
    {
        this.ToolType = toolType;
    }
}