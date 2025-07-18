using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Dialogue
{
    public string name;
    public bool showBusts = false;
    public Sprite leftBust;  // npc character bust
    public Sprite rightBust; // player bust

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
}

