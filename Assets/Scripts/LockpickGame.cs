using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI; // At the top if not already

public class LockpickGame : MonoBehaviour
{
    private bool movementCancelEnabled = false;
    private InputAction movementAction;
    public float movementCancelThreshold = 0.2f; // How much movement cancels the minigame

    [SerializeField] private Canvas joystick;
    public GameObject panel;        // Assign in inspector
    public RectTransform pointer;      // Assign in inspector
    public RectTransform bar;          // Assign in inspector
    public RectTransform greenZone;    // Assign in inspector

    public float defaultPointerSpeed = 200f;  // Pixels per second
    private float pointerSpeed; // Current speed of the pointer
    private bool movingRight = true;

    private bool gameActive = false;

    private InputAction interactAction;

    public int attemptsRequired = 3; // How many successes are needed
    private int attempts = 0;        // Current number of successes
    public float greenZoneSize = 300f;

    public Image[] attemptCircles; // Assign in inspector

    public Color successColor = Color.green;
    public Color failColor = Color.black;

    public void StartGame()
    {
        joystick.enabled = false;
        gameActive = true;
        greenZone.anchoredPosition = new Vector2(Random.Range(-bar.rect.width / 2, bar.rect.width / 2), greenZone.anchoredPosition.y);
        pointerSpeed = defaultPointerSpeed;
        greenZone.sizeDelta = new Vector2(greenZoneSize, greenZone.sizeDelta.y); // Reset size

        // Lock player movement for 1 second
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var movement = player.GetComponent<Movement>();
            if (movement != null)
            {
                movement.canMove = false;
                StartCoroutine(EnableMovementCancelAfterDelay(movement, 1f));
            }
        }
        movementCancelEnabled = false;
    }

    private IEnumerator EnableMovementCancelAfterDelay(Movement movement, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (movement != null)
            movement.canMove = true;
        movementCancelEnabled = true;
    }

    void OnEnable()
    {
        movementAction = InputSystem.actions.FindAction("Movement");
        if (movementAction != null)
            movementAction.Enable();
        interactAction = InputSystem.actions.FindAction("Interact");
        if (interactAction != null)
            interactAction.Enable();
    }

    void OnDisable()
    {
        if (movementAction != null)
            movementAction.Disable();
        if (interactAction != null)
            interactAction.Disable();
    }

    void Update()
    {
        // Cancel lockpick if player moves too much (after initial lock period)
        if (gameActive && movementCancelEnabled && movementAction != null && movementAction.ReadValue<Vector2>().magnitude > movementCancelThreshold)
        {
            Debug.Log("Lockpick minigame cancelled due to player movement.");
            gameActive = false;
            panel.SetActive(false);
            joystick.enabled = true;
            return;
        }
        if (!gameActive)
        {
            panel.SetActive(false);
            return;
        }
        else
        {
            panel.SetActive(true);
        }

        // Move pointer back and forth
        float move = pointerSpeed * Time.deltaTime * (movingRight ? 1 : -1);
        pointer.anchoredPosition += new Vector2(move, 0);

        // Reverse direction at bar edges
        float halfBar = bar.rect.width / 2;
        if (pointer.anchoredPosition.x > halfBar)
        {
            pointer.anchoredPosition = new Vector2(halfBar, pointer.anchoredPosition.y);
            movingRight = false;
        }
        else if (pointer.anchoredPosition.x < -halfBar)
        {
            pointer.anchoredPosition = new Vector2(-halfBar, pointer.anchoredPosition.y);
            movingRight = true;
        }

        if (interactAction != null && interactAction.WasPressedThisFrame())
        {
            if (IsPointerInGreenZone())
            {
                attempts++;
                Debug.Log($"Success! {attempts}/{attemptsRequired}");
                UpdateAttemptCircles(); // Update circles on success

                if (attempts >= attemptsRequired)
                {
                    Debug.Log("Lockpick minigame complete!");
                    // Trigger win logic here
                    gameActive = false;
                    joystick.enabled = true;
                }
                else
                {
                    greenZone.anchoredPosition = new Vector2(Random.Range(-bar.rect.width / 2, bar.rect.width / 2), greenZone.anchoredPosition.y);
                    greenZone.sizeDelta = new Vector2(greenZone.sizeDelta.x / (attempts + 1), greenZone.sizeDelta.y);
                    pointerSpeed += 250; // Increase speed for next attempt
                }
            }
            else
            {
                Debug.Log("Missed!");
                // Optionally reset attempts or give feedback
                attempts = 0; // Reset on fail, or remove this line if you want to keep progress
                ResetAttemptCircles(); // Reset circles on fail
                greenZone.sizeDelta = new Vector2(greenZoneSize, greenZone.sizeDelta.y);
                greenZone.anchoredPosition = new Vector2(Random.Range(-bar.rect.width / 2, bar.rect.width / 2), greenZone.anchoredPosition.y);
                pointerSpeed = defaultPointerSpeed; // Reset speed
            }
        }
    }

    bool IsPointerInGreenZone()
    {
        float pointerLeft = pointer.anchoredPosition.x - pointer.rect.width / 2;
        float pointerRight = pointer.anchoredPosition.x + pointer.rect.width / 2;
        float greenLeft = greenZone.anchoredPosition.x - greenZone.rect.width / 2;
        float greenRight = greenZone.anchoredPosition.x + greenZone.rect.width / 2;

        return pointerRight > greenLeft && pointerLeft < greenRight;
    }

    // Call this whenever attempts change
    private void UpdateAttemptCircles()
    {
        for (int i = 0; i < attemptCircles.Length; i++)
        {
            if (i < attempts)
                attemptCircles[i].color = successColor;
            else
                attemptCircles[i].color = failColor;
        }
    }

    // Call this when the player fails
    private void ResetAttemptCircles()
    {
        for (int i = 0; i < attemptCircles.Length; i++)
        {
            attemptCircles[i].color = failColor;
        }
    }
}

   