using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BorderScript : MonoBehaviour
{

  public DialogueManager dialogueManager; // Reference to the DialogueManager
  public Dialogue startingDialogue; // Reference to the dialogue to show at the start

  public GameObject player; // Reference to the player GameObject

  // Update is called once per frame
  void OnCollisionEnter2D (Collision2D collider)
    {
        if (collider.gameObject.CompareTag("Player"))
        {


          dialogueManager.StartDialogue(startingDialogue);
          Debug.Log("starting is starting");

        }

    }
}
