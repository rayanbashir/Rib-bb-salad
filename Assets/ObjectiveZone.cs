using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveZone : MonoBehaviour
{
    public bool isPlayerInside = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
        }
    }
}
