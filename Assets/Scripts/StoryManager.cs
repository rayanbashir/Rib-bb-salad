using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class StoryManager : MonoBehaviour
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
