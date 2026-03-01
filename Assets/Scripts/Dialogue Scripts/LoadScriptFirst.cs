using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity; // Assuming DialogueRunner is from Yarn Spinner

public class ProgressCheck : MonoBehaviour
{
    [System.Serializable]
    public class ConditionEntry
    {
        public string condition;
        public bool conditionMeansItDoesNotLoad = true;
    }

    [SerializeField] private List<ConditionEntry> conditions = new List<ConditionEntry>();
    [SerializeField] private string dialogueToLoad;
    [SerializeField] private DialogueRunner dialogueRunner;

    private void Start()
    {
        CheckConditionsAndLoadDialogue();
    }

    private void CheckConditionsAndLoadDialogue()
    {
        // If no dialogue specified, nothing to load
        if (string.IsNullOrEmpty(dialogueToLoad))
        {
            Debug.LogWarning("No dialogue specified to load.");
            return;
        }

        // Check all conditions
        bool shouldLoadDialogue = true;

        foreach (var conditionEntry in conditions)
        {
            bool hasCondition = SistemaInventario.Instance.GetGameProgress().Contains(conditionEntry.condition);

            // If condition means it does NOT load, and player has the condition, then don't load
            if (conditionEntry.conditionMeansItDoesNotLoad && hasCondition)
            {
                shouldLoadDialogue = false;
                break;
            }
            // If condition means it DOES load, and player doesn't have the condition, then don't load
            else if (!conditionEntry.conditionMeansItDoesNotLoad && !hasCondition)
            {
                shouldLoadDialogue = false;
                break;
            }
        }

        if (shouldLoadDialogue)
        {
            dialogueRunner.StartDialogue(dialogueToLoad);
            Debug.Log($"Started dialogue: {dialogueToLoad}");
        }
        else
        {
            Debug.Log($"Dialogue {dialogueToLoad} not loaded due to conditions not being met.");
        }
    }
}