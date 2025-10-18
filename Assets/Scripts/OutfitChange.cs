using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutfitChange : MonoBehaviour
{
    [Header("Fade Overlay (Animator Controller)")]
    [Tooltip("Animator component on the fullscreen fade object (Image/Canvas) where triggers will be set.")]
    public Animator fadeAnimatorTarget;
    [Tooltip("Animator Controller asset that contains 'FadeOut' and 'FadeIn' triggers. Optional—will be assigned to the target at runtime if provided.")]
    public RuntimeAnimatorController fadeAnimatorController;
    public string fadeOutTrigger = "FadeOut";
    public string fadeInTrigger = "FadeIn";

    [Header("Player")]
    [Tooltip("Optional explicit reference to the Player. If left empty, will try FindWithTag('Player').")]
    public GameObject player;
    [Header("Outfit Override Controller")]
    [Tooltip("AnimatorOverrideController to assign to the player's Animator when changing outfit.")]
    public AnimatorOverrideController outfitOverride;
    [Tooltip("(Optional fallback) Animator trigger to set on the player's Animator if no override is provided.")]
    public string playerOutfitAnimatorTrigger = "";

    [Header("Optional Outfit Objects Toggle")]
    [Tooltip("GameObjects to enable when the outfit changes (optional).")]
    public GameObject[] enableOnChange;
    [Tooltip("GameObjects to disable when the outfit changes (optional).")]
    public GameObject[] disableOnChange;

    [Header("Timing & Control")]
    [Tooltip("If true, locks Movement.canMove during the sequence.")]
    public bool lockPlayerMovement = true;
    [Tooltip("Wait time (seconds) after FadeOut before applying the outfit (let screen reach black).")]
    public float fadeOutWait = 0.6f;
    [Tooltip("Small delay after FadeIn before re-enabling movement (helps avoid visual pop).")]
    public float afterFadeInWait = 0.1f;
    [Tooltip("How long to keep the screen fully black before fading in.")]
    public float blackHold = 1.0f;

    private bool isRunning = false;
    private bool hasRun = false;

    [Header("Auto Trigger (2D)")]
    [Tooltip("If true, this will run when a 2D trigger collision occurs with the required tag.")]
    public bool triggerOnTriggerEnter2D = true;
    [Tooltip("Only react to colliders with this tag (usually 'Player'). Leave empty to react to any.")]
    public string requiredTag = "Player";
    [Tooltip("If true, only run once.")]
    public bool triggerOnce = true;

    // Call this to run the sequence: FadeOut -> Change Outfit -> FadeIn (with movement locked between)
    public void Trigger()
    {
        if (isRunning) return;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        isRunning = true;

        // Resolve player (if not assigned)
        if (player == null)
        {
            var found = GameObject.FindWithTag("Player");
            if (found != null) player = found;
        }

        Movement movement = null;
        Animator playerAnimator = null;
        if (player != null)
        {
            movement = player.GetComponent<Movement>();
            playerAnimator = player.GetComponent<Animator>();
        }

        // Lock movement
        if (lockPlayerMovement && movement != null)
        {
            movement.canMove = false;
        }

        // Ensure fade target has the provided controller (if any)
        if (fadeAnimatorTarget != null && fadeAnimatorController != null && fadeAnimatorTarget.runtimeAnimatorController != fadeAnimatorController)
        {
            fadeAnimatorTarget.runtimeAnimatorController = fadeAnimatorController;
            fadeAnimatorTarget.Rebind();
            fadeAnimatorTarget.Update(0f);
        }

        // Fade to black
        if (fadeAnimatorTarget != null && !string.IsNullOrEmpty(fadeOutTrigger))
        {
            fadeAnimatorTarget.SetTrigger(fadeOutTrigger);
        }

        // Give a moment for the screen to reach black
        if (fadeOutWait > 0f)
            yield return new WaitForSeconds(fadeOutWait);

        // Apply outfit change
        if (playerAnimator != null)
        {
            if (outfitOverride != null)
            {
                // Swap to the provided AnimatorOverrideController
                playerAnimator.runtimeAnimatorController = outfitOverride;
                // Ensure the animator rebinds to the new controller immediately
                playerAnimator.Rebind();
                playerAnimator.Update(0f);
            }
            else if (!string.IsNullOrEmpty(playerOutfitAnimatorTrigger))
            {
                // Fallback: use a trigger if no override controller is provided
                playerAnimator.SetTrigger(playerOutfitAnimatorTrigger);
            }
        }
        if (enableOnChange != null)
        {
            foreach (var go in enableOnChange)
                if (go != null) go.SetActive(true);
        }
        if (disableOnChange != null)
        {
            foreach (var go in disableOnChange)
                if (go != null) go.SetActive(false);
        }

        // Hold on black before fading in
        if (blackHold > 0f)
            yield return new WaitForSeconds(blackHold);

        // Fade back in
        if (fadeAnimatorTarget != null && !string.IsNullOrEmpty(fadeInTrigger))
        {
            fadeAnimatorTarget.SetTrigger(fadeInTrigger);
        }

        if (afterFadeInWait > 0f)
            yield return new WaitForSeconds(afterFadeInWait);

        // Unlock movement
        if (lockPlayerMovement && movement != null)
        {
            movement.canMove = true;
        }

        isRunning = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggerOnTriggerEnter2D) return;
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;
        if (triggerOnce && hasRun) return;

        // If player reference is not set, and the entering collider is the player, cache it
        if (player == null && other.CompareTag("Player"))
            player = other.gameObject;

        hasRun = true;
        Trigger();
    }
}
