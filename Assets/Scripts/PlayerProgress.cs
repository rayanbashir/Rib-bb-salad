using System.Collections.Generic;
using UnityEngine;

public class PlayerProgress : MonoBehaviour
{
    public List<string> talkedToNPCs = new List<string>();
    public List<string> collectedItems = new List<string>();

    public bool HasTalkedTo(string npcName)
    {
        return talkedToNPCs.Contains(npcName);
    }

    public void TalkToNPC(string npcName)
    {
        if (!talkedToNPCs.Contains(npcName))
            talkedToNPCs.Add(npcName);
    }

    public bool HasItem(string itemName)
    {
        return collectedItems.Contains(itemName);
    }

    public void CollectItem(string itemName)
    {
        if (!collectedItems.Contains(itemName))
            collectedItems.Add(itemName);
    }
}