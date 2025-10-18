using System.Collections;
using UnityEngine;
using UnityEngine.Serialization; // For maintaining old serialized field names after rename

/// <summary>
/// Handles triggering one of three endings. Each ending fades to black first, then shows its UI.
/// Assign the BlackFade object's Animator (with trigger "FadeOut") and three UI GameObjects in the inspector.
/// </summary>
public class EndingManager : MonoBehaviour
{
    [Header("Fade Settings")] 
    [Tooltip("Animator on the BlackFade GameObject that has a trigger parameter named 'FadeOut'.")]
    [SerializeField] private Animator blackFadeAnimator;

    [Tooltip("Seconds to wait after triggering FadeOut before showing the ending UI (should match fade animation length).")]
    [SerializeField] private float fadeDuration = 1f;

    [Header("Ending UI Roots")] 
    [FormerlySerializedAs("ending1UI")] [SerializeField] private GameObject badEndingUI;
    [FormerlySerializedAs("ending2UI")] [SerializeField] private GameObject goodEndingUI;
    [FormerlySerializedAs("ending3UI")] [SerializeField] private GameObject mafiaEndingUI;

    private Coroutine _currentRoutine;

    /// <summary>
    /// Loads the Main Menu scene immediately.
    /// </summary>
    public void LoadMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main Menu");
    }

    /// <summary>Play Bad Ending sequence.</summary>
    public void PlayBadEnding() => StartEnding(badEndingUI);
    /// <summary>Play Good Ending sequence.</summary>
    public void PlayGoodEnding() => StartEnding(goodEndingUI);
    /// <summary>Play Mafia Ending sequence.</summary>
    public void PlayMafiaEnding() => StartEnding(mafiaEndingUI);

    private void StartEnding(GameObject targetUI)
    {
        if (targetUI == null)
        {
            Debug.LogWarning("EndingManager: Target UI not assigned.");
            return;
        }

        // Stop any previous sequence to avoid overlap.
        if (_currentRoutine != null)
        {
            StopCoroutine(_currentRoutine);
            _currentRoutine = null;
        }

        _currentRoutine = StartCoroutine(PlayEndingSequence(targetUI));
    }

    private IEnumerator PlayEndingSequence(GameObject targetUI)
    {
        // Hide all ending UI first.
        HideAllEndingUI();

        // Trigger fade out.
        if (blackFadeAnimator != null)
        {
            blackFadeAnimator.ResetTrigger("FadeOut"); // ensure clean state
            blackFadeAnimator.SetTrigger("FadeOut");
        }
        else
        {
            Debug.LogWarning("EndingManager: Black Fade Animator not assigned.");
        }

        // Wait for the fade animation to complete.
        if (fadeDuration > 0f)
            yield return new WaitForSeconds(fadeDuration);

        // Show the specific ending UI.
        targetUI.SetActive(true);

        _currentRoutine = null; // Mark routine finished.
    }

    private void HideAllEndingUI()
    {
        if (badEndingUI) badEndingUI.SetActive(false);
        if (goodEndingUI) goodEndingUI.SetActive(false);
        if (mafiaEndingUI) mafiaEndingUI.SetActive(false);
    }

#if UNITY_EDITOR
    // Optional quick test context menu items (right-click component header in Inspector).
    [ContextMenu("Test Bad Ending")] private void ContextPlayBad() => PlayBadEnding();
    [ContextMenu("Test Good Ending")] private void ContextPlayGood() => PlayGoodEnding();
    [ContextMenu("Test Mafia Ending")] private void ContextPlayMafia() => PlayMafiaEnding();
#endif
}
