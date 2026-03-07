using UnityEngine;
using System.Collections.Generic;
using Yarn.Unity;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    [Header("Active Objectives")]
    public List<ObjectiveInstance> activeObjectives = new List<ObjectiveInstance>();
    public List<ObjectiveInstance> completedObjectives = new List<ObjectiveInstance>();

    [Header("Events")]
    public System.Action<ObjectiveInstance> onObjectiveAdded;
    public System.Action<ObjectiveInstance> onObjectiveCompleted;

    private DialogueRunner dialogueRunner;
    private SistemaInventario inventory;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            gameObject.tag = "Inventory";
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        inventory = SistemaInventario.Instance;
    }

    public void AddObjective(ObjectiveData objectiveData)
    {
        if (objectiveData == null) return;

        // Check if already added
        foreach (var obj in activeObjectives)
        {
            if (obj.data == objectiveData) return;
        }

        ObjectiveInstance instance = new ObjectiveInstance(objectiveData);
        instance.isActive = true;

        activeObjectives.Add(instance);
        onObjectiveAdded?.Invoke(instance);
    }

    // Call this whenever progress is added (from SistemaInventario)
    public void CheckAllObjectives()
    {
        if (inventory == null) return;

        List<ObjectiveInstance> toComplete = new List<ObjectiveInstance>();

        foreach (var instance in activeObjectives)
        {
            if (instance.CheckCompletion(inventory))
            {
                toComplete.Add(instance);
            }
        }

        foreach (var instance in toComplete)
        {
            CompleteObjective(instance);
        }
    }

    private void CompleteObjective(ObjectiveInstance instance)
    {
        instance.isActive = false;
        instance.isCompleted = true;

        activeObjectives.Remove(instance);
        completedObjectives.Add(instance);

        // Add progress rewards
        if (inventory != null)
        {
            foreach (string progressTag in instance.data.progressRewards)
            {
                inventory.AddProgress(progressTag);
            }

            inventory.ModificadorMoedas(instance.data.rewardGold);

            foreach (var item in instance.data.rewardItems)
            {
                inventory.AdicionarItem(item, 1);
            }
        }

        // Play completion dialogue
        if (instance.data.playDialogueOnComplete &&
            !string.IsNullOrEmpty(instance.data.completionDialogueNode) &&
            dialogueRunner != null)
        {
            dialogueRunner.StartDialogue(instance.data.completionDialogueNode);
        }

        onObjectiveCompleted?.Invoke(instance);

        // Add next objective if exists
        if (instance.data.nextObjective != null)
        {
            AddObjective(instance.data.nextObjective);
        }
    }

    // Yarn Command to add objective
    [YarnCommand("add_objective")]
    public static void AddObjectiveCommand(string objectiveName)
    {
        // This would need a way to look up objectives by name
        // You could use Resources.Load or a registry
        Debug.Log($"Add objective: {objectiveName}");
    }
}