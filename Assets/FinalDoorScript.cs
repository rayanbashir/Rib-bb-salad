using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Triggers a UnityEvent when the Player collides with this GameObject (non-trigger collider).
/// Works with both 2D and 3D physics. Add listeners to <see cref="onPlayerCollision"/> in the Inspector.
/// </summary>
/// <remarks>
/// Requirements:
/// - This object must have a Collider2D (for 2D) or Collider (for 3D) that is NOT marked as "Is Trigger".
/// - The Player must have a Rigidbody2D (or Rigidbody for 3D) and a Collider.
/// </remarks>
public class FinalDoorScript : MonoBehaviour
{
    [Header("Collision Event")]
    [Tooltip("Event invoked the first time (or every time) the player collides with this object.")]
    public UnityEvent onPlayerCollision;

    [Header("Settings")]
    [Tooltip("Tag used to identify the player object.")]
    public string playerTag = "Player";

    [Tooltip("If true, the event will only fire once.")]
    public bool oneShot = true;

    [Tooltip("Log debug information when collisions happen.")]
    public bool debugLogs = true;

    private bool _hasFired;

    /// <summary>
    /// Common logic to invoke the event (respects oneShot flag).
    /// </summary>
    private void FireEventIfAllowed(GameObject playerObj, string via)
    {
        if (oneShot && _hasFired) return;
        _hasFired = true;
        if (debugLogs)
            Debug.Log($"[FinalDoorScript] Player collision detected via {via} with {name}. Invoking event.");
        onPlayerCollision?.Invoke();
    }

    // 2D physics collision
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!string.IsNullOrEmpty(playerTag) && !collision.collider.CompareTag(playerTag)) return;
        FireEventIfAllowed(collision.collider.gameObject, "OnCollisionEnter2D");
    }
}

