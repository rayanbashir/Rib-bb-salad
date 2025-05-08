using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class Movement : MonoBehaviour
{
    public CinemachineVirtualCamera virtualCamera;

    public float moveSpeed;
    public float lastAngle;
    public float sprintMultiplier = 2f; // Sprint speed multiplier

    public bool canMove;
    Rigidbody2D rb;
    Animator animator;

    private InputAction moveAction;

    // Start is called before the first frame update
    void Start()
    {
        canMove = false;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        Invoke("EnablePlayerMovement", 0.1f);
        moveAction = InputSystem.actions.FindAction("Movement");
    }

    void Update()
    {
        // Get input from both PC and mobile
        Vector2 moveVector = moveAction.ReadValue<Vector2>();

        // Sprint logic: hold Left Shift to sprint
        float currentSpeed = moveSpeed;
        bool isSprinting = false;
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
        {
            currentSpeed *= sprintMultiplier;
            isSprinting = true;
        }

        // Camera zoom logic
        if (virtualCamera != null)
        {
            float targetOrthoSize = isSprinting ? 6f + 1f : 6f; // Example: base size 6, +1 when sprinting
            virtualCamera.m_Lens.OrthographicSize = targetOrthoSize;
        }

        // Apply movement speed
        Vector2 movement = moveVector * currentSpeed;

        if (canMove)
        {
            rb.velocity = movement;
        }
        else
        {
            rb.velocity = Vector2.zero;
        }

        // Animation parameters
        animator.SetFloat("Horizontal", moveVector.x);
        animator.SetFloat("Vertical", moveVector.y);
        animator.SetFloat("Speed", rb.velocity.sqrMagnitude);

        float angle = Mathf.Atan2(moveVector.x, moveVector.y) * Mathf.Rad2Deg;
        if (rb.velocity.sqrMagnitude > 0.01f)
        {
            lastAngle = angle;
        }

        // Set the idle direction parameter
        animator.SetFloat("IdleAngle", lastAngle);
    }

    void EnablePlayerMovement()
    {
        canMove = true;
    }

}

