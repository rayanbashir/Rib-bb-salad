using UnityEngine;
using System.Collections;
public class DoorTeleport : MonoBehaviour
{
    public Transform destination;    // The destination transform where player will teleport
    public float verticalOffset = -1f; // Optional offset to place player slightly in front of destination
    public float horizontalOffset = -0.5f;
    public float exitDirection = -180f; // 0 = up, 90 = right, 180 = down, -90 = left
    public Movement playerMovement; 
    public Animator animator;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(TeleportPlayer(other.transform, 1f));
        }
    }

    IEnumerator TeleportPlayer(Transform player, float delayTime)
    {
        playerMovement = player.GetComponent<Movement>();
        if (destination != null)
        {
            animator.SetTrigger("FadeOut");
            playerMovement.canMove = false; 
            yield return new WaitForSeconds(delayTime);
            Debug.Log("Baaaaaaakaaaaaaaaaaaaaaaa");
            Vector2 targetPosition = destination.position;
            targetPosition.y += (verticalOffset);
            targetPosition.x += (horizontalOffset);
            
            player.position = targetPosition;

            yield return new WaitForSeconds(delayTime);
            animator.SetTrigger("FadeIn");

            Invoke("EnablePlayerMovement", 0.4f);
        }
    }
    void EnablePlayerMovement()
    {
        playerMovement.canMove = true;
    }

}