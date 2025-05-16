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
    public bool hasOptions;

    [TextArea(3,10)]
    public string prompt; // Add this line for custom prompt
}

[System.Serializable]
public class DialogueOption
{
    public string optionText;
    public Dialogue nextDialogue;
   
}
