using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class TouchDetector : MonoBehaviour
{
    public GameObject joystickUI;
    public PlayerInteraction PlayerInteraction;

    private InputAction tapAction;

    void Awake()
    {
        tapAction = new InputAction(type: InputActionType.PassThrough, binding: "<Pointer>/press");
        tapAction.performed += OnTouch;
    }

    void OnEnable()
    {
        tapAction.Enable();
    }

    void OnDisable()
    {
        tapAction.Disable();
    }

    private void OnTouch(InputAction.CallbackContext context)
    {
        Vector2 screenPosition = Pointer.current.position.ReadValue();

        if (!IsPointerOverUIExceptJoystick(screenPosition))
        {
            OnTouchDetected();
        }
        else
        {
            OnTouchRelease();
        }
    }

    void Update(){
        
    }

    bool IsPointerOverUIExceptJoystick(Vector2 screenPosition)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            if (result.gameObject == joystickUI || result.gameObject.transform.IsChildOf(joystickUI.transform))
            {
                return true; // Touching the joystick
            }
        }

        return false;
    }

    void OnTouchDetected()
    {
        Debug.Log("Touch outside joystick detected.");
        PlayerInteraction.TouchingJoystick = false;
    }

    void OnTouchRelease()
    {
        Debug.Log("Touch on joystick detected.");
        PlayerInteraction.TouchingJoystick = true;
    }
}
