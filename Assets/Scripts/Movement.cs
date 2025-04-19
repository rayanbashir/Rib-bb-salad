using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    public float moveSpeed;
    public float lastAngle;

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

        // Apply movement speed
        Vector2 movement = moveVector * moveSpeed;

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

