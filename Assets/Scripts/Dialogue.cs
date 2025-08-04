using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Dialogue
{
    public string name;

    [TextArea(3,10)]
    public string[] sentences;

    public bool LockPlayerMovement;

    public DialogueOption[] options;
}

[System.Serializable]
public class DialogueOption
{
    public string optionText;
    public Dialogue nextDialogue;
    public Dialogue subsequentDialogue; // Dialogue to show on next interaction
    public bool changesFutureDialogue = false; // Flag to control if this option changes future dialogue

    [Header("Requirements (leave blank for always available)")]
    public string requiredItem; // Name of item required for this option to appear
    public string requiredClue; // Name of clue required for this option to appear
    [Tooltip("If true, the required item will be consumed (removed from inventory) when this option is selected.")]
    public bool consumeRequiredItem = false;
    [Tooltip("If true, the required clue will be consumed (removed from inventory) when this option is selected.")]
    public bool consumeRequiredClue = false;
}


