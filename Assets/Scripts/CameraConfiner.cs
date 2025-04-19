using UnityEngine;
using Cinemachine;

public class CameraConfiner : MonoBehaviour
{
    private CinemachineVirtualCamera virtualCamera;
    private CinemachineConfiner confiner;

    private void Start()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        confiner = virtualCamera.GetComponent<CinemachineConfiner>();

        if (confiner == null)
        {
            confiner = virtualCamera.gameObject.AddComponent<CinemachineConfiner>();
            confiner.m_ConfineMode = CinemachineConfiner.Mode.Confine2D;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Log the name of the object that entered the trigger
        Debug.Log("Trigger entered by: " + other.gameObject.name);

        // Attempt to get the PolygonCollider2D from the other GameObject
        PolygonCollider2D boundsCollider = other.gameObject.GetComponent<PolygonCollider2D>();

        // Check if the collider exists
        if (boundsCollider != null)
        {
            confiner.m_BoundingShape2D = boundsCollider;
            Debug.Log("Camera bounds set to: " + other.gameObject.name);
        }
        else
        {
            Debug.LogWarning("No PolygonCollider2D found on: " + other.gameObject.name);
        }
    }
} 