using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Yarn.Unity;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    [Header("Active Objectives")]
    public List<ObjectiveInstance> activeObjectives = new List<ObjectiveInstance>();
    public List<ObjectiveInstance> completedObjectives = new List<ObjectiveInstance>();

    [Header("UI References")]
    public TextMeshProUGUI questNameText;      // Displays current objective name
    public TextMeshProUGUI questDescriptionText; // Displays current objective description
    public GameObject objectiveUIPanel;        // Optional panel to show/hide objectives

    [Header("Objective Database")]
    public List<ObjectiveData> allObjectives;  // All available objectives for lookups

    [Header("Events")]
    public System.Action<ObjectiveInstance> onObjectiveAdded;
    public System.Action<ObjectiveInstance> onObjectiveCompleted;

    private DialogueRunner dialogueRunner;
    private SistemaInventario inventory;
    private ObjectiveInstance currentObjective;

    // Dictionary for faster lookups
    private Dictionary<string, ObjectiveData> objectiveDictionary;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            gameObject.tag = "Inventory";

            // Build lookup dictionary
            BuildObjectiveDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void BuildObjectiveDictionary()
    {
        objectiveDictionary = new Dictionary<string, ObjectiveData>();

        if (allObjectives != null)
        {
            foreach (var obj in allObjectives)
            {
                if (obj != null && !objectiveDictionary.ContainsKey(obj.objectiveName))
                {
                    objectiveDictionary.Add(obj.objectiveName, obj);
                }
            }
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

        // Check if already active
        foreach (var obj in activeObjectives)
        {
            if (obj.data == objectiveData) return;
        }

        // Check if already completed
        foreach (var obj in completedObjectives)
        {
            if (obj.data == objectiveData) return;
        }

        ObjectiveInstance instance = new ObjectiveInstance(objectiveData);
        instance.isActive = true;

        activeObjectives.Add(instance);

        // If this is the first objective, set it as current
        if (activeObjectives.Count == 1)
        {
            SetCurrentObjective(instance);
        }

        Debug.Log($"Objective added: {objectiveData.objectiveName}");
        onObjectiveAdded?.Invoke(instance);
    }

    public void AddObjectiveByName(string objectiveName)
    {
        if (objectiveDictionary == null)
            BuildObjectiveDictionary();

        if (objectiveDictionary.ContainsKey(objectiveName))
        {
            AddObjective(objectiveDictionary[objectiveName]);
        }
        else
        {
            Debug.LogError($"Objective '{objectiveName}' not found in database!");
        }
    }

    public void SetCurrentObjective(ObjectiveInstance objective)
    {
        currentObjective = objective;
        UpdateObjectiveUI();
    }

    public void SetCurrentObjectiveByName(string objectiveName)
    {
        // Look for objective in active objectives
        foreach (var obj in activeObjectives)
        {
            if (obj.data.objectiveName == objectiveName)
            {
                SetCurrentObjective(obj);
                return;
            }
        }

        Debug.LogWarning($"Objective '{objectiveName}' is not active!");
    }

    private void UpdateObjectiveUI()
    {
        if (currentObjective == null || currentObjective.data == null)
        {
            // No active objective, hide UI
            if (objectiveUIPanel != null)
                objectiveUIPanel.SetActive(false);
            return;
        }

        // Update UI texts
        if (questNameText != null)
            questNameText.text = currentObjective.data.objectiveName;

        if (questDescriptionText != null)
            questDescriptionText.text = currentObjective.data.objectiveDescription;

        // Show UI panel
        if (objectiveUIPanel != null)
            objectiveUIPanel.SetActive(true);
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

        Debug.Log($"Objective completed: {instance.data.objectiveName}");
        onObjectiveCompleted?.Invoke(instance);

        // Set next objective as current if it exists
        if (instance.data.nextObjective != null)
        {
            AddObjective(instance.data.nextObjective);
        }

        // If there are still active objectives, set the first one as current
        if (activeObjectives.Count > 0)
        {
            SetCurrentObjective(activeObjectives[0]);
        }
        else
        {
            // No more objectives, hide UI
            currentObjective = null;
            UpdateObjectiveUI();
        }
    }

    // Yarn Command to add objective
    [YarnCommand("add_objective")]
    public static void AddObjectiveCommand(string objectiveName)
    {
        if (Instance != null)
        {
            Debug.Log($"Adding objective: {objectiveName}");
            Instance.AddObjectiveByName(objectiveName);
        }
        else
        {
            Debug.LogError("ObjectiveManager instance not found!");
        }
    }

    // Yarn Command to set current objective
    [YarnCommand("set_objective")]
    public static void SetObjectiveCommand(string objectiveName)
    {
        if (Instance != null)
        {
            Debug.Log($"Setting current objective: {objectiveName}");
            Instance.SetCurrentObjectiveByName(objectiveName);
        }
    }

    // Yarn Command to check if objective is completed
    [YarnFunction("objective_completed")]
    public static bool ObjectiveCompleted(string objectiveName)
    {
        if (Instance == null) return false;

        foreach (var obj in Instance.completedObjectives)
        {
            if (obj.data.objectiveName == objectiveName)
                return true;
        }
        return false;
    }

    // Yarn Command to check if objective is active
    [YarnFunction("objective_active")]
    public static bool ObjectiveActive(string objectiveName)
    {
        if (Instance == null) return false;

        foreach (var obj in Instance.activeObjectives)
        {
            if (obj.data.objectiveName == objectiveName)
                return true;
        }
        return false;
    }

    // Yarn Command to clear current objective
    [YarnCommand("clear_objective")]
    public static void ClearObjectiveCommand()
    {
        if (Instance != null)
        {
            Instance.currentObjective = null;
            Instance.UpdateObjectiveUI();
        }
    }

    // Public method to manually update UI (call after progress changes)
    public void RefreshUI()
    {
        UpdateObjectiveUI();
    }

    private void OnValidate()
    {
        // Rebuild dictionary in editor if list changes
        if (Application.isPlaying)
            BuildObjectiveDictionary();
    }
}