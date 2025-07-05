using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class StoryManager : MonoBehaviour
{
    public TextMeshProUGUI ObjectiveText; // Text component to display the current objective    

    private Objective _currentObjective;
    public Objective currentObjective
    {
        get { return _currentObjective; }
        set
        {
            _currentObjective = value;
            UpdateObjectiveText(); // Call method when value changes
        }
    }
    public Objective[] objectivesList;

    void Start()
    {
        currentObjective = objectivesList[0]; // Set the first objective as the current one

    }

    void UpdateObjectiveText()
    {
        ObjectiveText.text = "Current Objective: " + currentObjective.title;

    }


    public void CompleteObjective()
    {
        currentObjective.CompleteObjective(); // Complete the current objective

    }
}

[System.Serializable]
public class Objective
{
    public string title;
    public string description;
    public bool isCompleted = false;
    public Objective[] nextObjectives; // Reference to all posssible next objectives
    public bool isActive = false; // Whether this objective is currently active
    public bool conditionsMet = false; // Whether this objective can be done
    public UnityEvent onObjectiveCompleted; // Event to trigger when the objective is completed
    public Objective previousObjective; // Reference to the previous objective
    public void CompleteObjective()
    {
        isCompleted = true;
        onObjectiveCompleted.Invoke(); // Trigger the event for objective completion
        isActive = false; // Deactivate the current objective
    }

}
