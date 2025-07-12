using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class StoryManager : MonoBehaviour
{
    public TextMeshProUGUI ObjectiveText; // UI text to show current objective
    public PlayerProgress playerProgress;

    [Header("Objective Setup")]
    public Objective[] allObjectives; // List of all objectives (can be manually assigned)
    
    private Objective _currentObjective;
    public Objective currentObjective
    {
        get { return _currentObjective; }
        set
        {
            _currentObjective = value;
            UpdateObjectiveText();
        }
    }

    void Start()
    {
        if (allObjectives.Length > 0)
        {
            currentObjective = allObjectives[0]; // Start with the first objective
            currentObjective.isActive = true;

             allObjectives[0].completionCondition = () => playerProgress.HasTalkedTo("Chief");
        }
    }

    void Update()
    {
        // Check if current objective should be auto-completed
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

        currentObjective.CompleteObjective(); // Marks it completed and runs event

        // Move to the next objective, if any
        if (currentObjective.nextObjectives != null && currentObjective.nextObjectives.Length > 0)
        {
            currentObjective = currentObjective.nextObjectives[0]; // For now, pick first next
            currentObjective.isActive = true;
        }
        else
        {
            currentObjective = null; // No more objectives
        }

        UpdateObjectiveText();
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
    public Objective[] nextObjectives; // What comes next
    public Objective previousObjective; // What came before

    [Header("Events & Logic")]
    public UnityEvent onObjectiveCompleted; // Run this when completed
    [NonSerialized] public Func<bool> completionCondition; // Custom check function

    public void CompleteObjective()
    {
        if (isCompleted) return;

        isCompleted = true;
        isActive = false;
        onObjectiveCompleted?.Invoke();
        Debug.Log("Objective Complete: " + title);
    }
}
