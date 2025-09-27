using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
#endif

public partial class StoryManager : MonoBehaviour
{
    public TextMeshProUGUI ObjectiveText;
    public PlayerProgress playerProgress;

    [Header("Objective Setup")]
    public Objective[] allObjectives;

    private Objective _currentObjective;
    public Objective currentObjective
    {
        get { return _currentObjective; }
        set
        {
            _currentObjective = value;

            if (_currentObjective != null)
            {
                _currentObjective.isActive = true;
                AssignCompletionCondition(_currentObjective);
                UpdateObjectiveText();
            }
        }
    }

    void Start()
    {
        if (allObjectives.Length > 0)
        {
            currentObjective = allObjectives[0];
        }
    }

    void Update()
    {
        if (currentObjective != null &&
            !currentObjective.isCompleted &&
            currentObjective.completionCondition != null &&
            currentObjective.completionCondition())
        {
            CompleteObjective();
        }
    }

    void UpdateObjectiveText()
    {
        if (ObjectiveText == null)
        {
            // Avoid runtime NREs if the reference is missing in the scene.
            Debug.LogWarning("StoryManager.ObjectiveText is not assigned.");
            return;
        }

        if (currentObjective != null)
        {
            ObjectiveText.text = "Current Objective: " + currentObjective.title;
        }
        else
        {
            ObjectiveText.text = "All objectives completed!";
        }
    }

    public void CompleteObjective()
    {
        if (currentObjective == null || currentObjective.isCompleted)
            return;

        currentObjective.CompleteObjective(); // ✅ Actually calls the method below

        if (currentObjective.nextObjectives != null && currentObjective.nextObjectives.Length > 0)
        {
            currentObjective = currentObjective.nextObjectives[0];
        }
        else
        {
            currentObjective = null;
        }

        UpdateObjectiveText();
    }

    void AssignCompletionCondition(Objective obj)
    {
        switch (obj.conditionType)
        {
            case ObjectiveConditionType.TalkToNPC:
                obj.completionCondition = () => playerProgress.HasTalkedTo(obj.targetName);
                break;

            case ObjectiveConditionType.CollectItem:
                obj.completionCondition = () => playerProgress.HasItem(obj.targetName);
                break;

            case ObjectiveConditionType.EnterZone:
                obj.completionCondition = () => obj.targetZone != null && obj.targetZone.isPlayerInside;
                break;

            case ObjectiveConditionType.Custom:
                obj.completionCondition = null;
                break;

            default:
                obj.completionCondition = null;
                break;
        }
    }
}

[System.Serializable]
public class Objective
{
    [Header("Objective Info")]
    public string title;
    public string description;

    [Header("Status")]
    public bool isCompleted = false;
    public bool isActive = false;

    [Header("Flow")]
    public Objective[] nextObjectives;
    public Objective previousObjective;

    [Header("Events & Logic")]
    public UnityEvent onObjectiveCompleted;

    [Tooltip("Generic condition type.")]
    public ObjectiveConditionType conditionType = ObjectiveConditionType.None;

    [Tooltip("For TalkToNPC / CollectItem: target name (e.g. NPC name or item ID).")]
    public string targetName;

    [Tooltip("For EnterZone: assign the zone trigger manually.")]
    public ObjectiveZone targetZone;

    [NonSerialized] public Func<bool> completionCondition;

    public void CompleteObjective()
    {
        if (isCompleted) return;

        isCompleted = true;
        isActive = false;

        Debug.Log("✅ Objective completed: " + title);
        onObjectiveCompleted?.Invoke(); // ✅ This line was missing in your version
    }
}

public enum ObjectiveConditionType
{
    None,
    TalkToNPC,
    CollectItem,
    EnterZone,
    Custom
}

#if UNITY_EDITOR
// Editor-only helpers embedded here (no separate Editor script needed)
public partial class StoryManager : MonoBehaviour
{
    [SerializeField, Tooltip("If enabled, StoryManager will auto-clean BROKEN UnityEvent listeners on validate. Leave OFF to avoid deleting newly added empty listeners while editing.")]
    private bool autoCleanOnValidate = false;

    [SerializeField, Tooltip("When true, print a log message after cleaning during OnValidate.")]
    private bool verboseCleanupLogs = false;

    private void OnValidate()
    {
        if (!autoCleanOnValidate) return;
        // Proactively clean clearly broken UnityEvent listeners to prevent Inspector NREs
        try { CleanBrokenObjectiveEventsInternal(verboseCleanupLogs); }
        catch { /* ignore to avoid spamming console during domain reloads */ }
    }

    [ContextMenu("Clean Broken Objective Events")]
    private void CleanBrokenObjectiveEvents()
    {
        CleanBrokenObjectiveEventsInternal(true);
    }

    private void CleanBrokenObjectiveEventsInternal(bool logResult)
    {
        int totalRemoved = 0;

        // Pass 1: Try serialized property pruning (fast path)
        try
        {
            var so = new SerializedObject(this);
            var objectivesProp = so.FindProperty("allObjectives");
            if (objectivesProp != null && objectivesProp.isArray)
            {
                for (int i = 0; i < objectivesProp.arraySize; i++)
                {
                    var objectiveProp = objectivesProp.GetArrayElementAtIndex(i);
                    if (objectiveProp == null) continue;
                    CleanObjectivePropertyRecursively(objectiveProp, ref totalRemoved);
                }
                if (totalRemoved > 0)
                {
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(this);
                }
            }
        }
        catch { /* fall through to pass 2 */ }

        // Pass 2: Use UnityEventTools to ensure no broken persistent listeners remain (robust path)
        if (allObjectives != null)
        {
            for (int i = 0; i < allObjectives.Length; i++)
            {
                CleanObjectiveInstanceRecursively(allObjectives[i], ref totalRemoved);
            }

            if (totalRemoved > 0)
            {
                EditorUtility.SetDirty(this);
            }
        }

        if (logResult && totalRemoved > 0)
        {
            Debug.Log($"StoryManager cleaned {totalRemoved} broken Objective.onObjectiveCompleted listener(s).");
        }
    }

    // SerializedProperty-based recursive cleanup
    private void CleanObjectivePropertyRecursively(SerializedProperty objectiveProp, ref int totalRemoved)
    {
        if (objectiveProp == null) return;

        // Clean this objective's event (only when clearly corrupt)
        var eventProp = objectiveProp.FindPropertyRelative("onObjectiveCompleted");
        if (eventProp != null)
        {
            var calls = eventProp.FindPropertyRelative("m_PersistentCalls.m_Calls");
            if (calls != null && calls.isArray)
            {
                for (int c = calls.arraySize - 1; c >= 0; c--)
                {
                    var call = calls.GetArrayElementAtIndex(c);
                    if (call == null)
                    {
                        calls.DeleteArrayElementAtIndex(c);
                        totalRemoved++;
                        continue;
                    }

                    var targetProp = call.FindPropertyRelative("m_Target");
                    var methodProp = call.FindPropertyRelative("m_MethodName");
                    var argsProp = call.FindPropertyRelative("m_Arguments");

                    // Only remove when required sub-properties are missing (corrupt entry)
                    if (targetProp == null || methodProp == null || argsProp == null)
                    {
                        calls.DeleteArrayElementAtIndex(c);
                        totalRemoved++;
                        continue;
                    }

                    // Do NOT remove entries just because target is null or method empty — that is common while user is mid-edit.
                }
            }
        }

        // Recurse into children nextObjectives
        var nextArray = objectiveProp.FindPropertyRelative("nextObjectives");
        if (nextArray != null && nextArray.isArray)
        {
            for (int i = 0; i < nextArray.arraySize; i++)
            {
                var child = nextArray.GetArrayElementAtIndex(i);
                if (child != null)
                {
                    CleanObjectivePropertyRecursively(child, ref totalRemoved);
                }
            }
        }
    }

    // Instance-based recursive cleanup
    private void CleanObjectiveInstanceRecursively(Objective obj, ref int totalRemoved)
    {
        if (obj == null) return;

        var evt = obj.onObjectiveCompleted;
        if (evt == null)
        {
            obj.onObjectiveCompleted = new UnityEvent();
        }
        else
        {
            int count = evt.GetPersistentEventCount();
            for (int idx = count - 1; idx >= 0; idx--)
            {
                var target = evt.GetPersistentTarget(idx);
                var method = evt.GetPersistentMethodName(idx);
                // Only remove if method is set but the target is missing (clearly broken),
                // otherwise keep to allow user to finish editing.
                if (target == null && !string.IsNullOrEmpty(method))
                {
                    UnityEventTools.RemovePersistentListener(evt, idx);
                    totalRemoved++;
                }
            }
        }

        if (obj.nextObjectives != null)
        {
            foreach (var child in obj.nextObjectives)
            {
                CleanObjectiveInstanceRecursively(child, ref totalRemoved);
            }
        }
    }
}
#endif
