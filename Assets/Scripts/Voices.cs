using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voices : MonoBehaviour
{
    public DialogueManager dialogueManager; // Reference to the DialogueManager
    public Dialogue startingDialogue; // Reference to the dialogue to show at the start

    void Start()
    {
        Invoke("theVoices", 0.2f);
    }

    // Update is called once per frame
    void theVoices()
    {
        dialogueManager.StartDialogue(startingDialogue);
        Debug.Log("starting is starting");
    }
}
