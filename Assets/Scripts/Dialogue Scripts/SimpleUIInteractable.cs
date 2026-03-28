using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Yarn.Unity;

public class SimpleUIInteractable : MonoBehaviour
{
    [Header("UI Settings")]
    public string uiTitle = "Interact";
    public GameObject uiPanel; // The panel that contains your two buttons

    [Header("Buttons")]
    public Button actionButton; // The button that will perform the action
    public DadosItem itemToRemove; // The item to remove when button is pressed
    public string progressToAdd = "savedlife"; // The progress tag to add

    [Header("Button Text (Optional)")]
    public string buttonText = "Heal";

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.Space;
    public GameObject interactionPopup; // Optional popup to show when player is nearby

    [Header("UI Title Text (Optional)")]
    public TextMeshProUGUI titleText; // Optional: display title on the panel

    private bool playerInRange = false;
    private MovimentacaoExploracao playerMovement;
    private bool isUIOpen = false;
    public bool QuestDone = true;
    private SistemaInventario inventory;

    private void Start()
    {
        // Find inventory
        inventory = SistemaInventario.Instance;
        if (inventory == null)
        {
            Debug.LogError("SistemaInventario instance not found!");
        }

        // Hide UI panel initially
        if (uiPanel != null)
            uiPanel.SetActive(false);

        // Hide interaction popup initially
        if (interactionPopup != null)
            interactionPopup.SetActive(false);

        // Set up button
        if (actionButton != null)
        {
            // Update button text
            TextMeshProUGUI buttonTextComponent = actionButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonTextComponent != null)
            {
                buttonTextComponent.text = buttonText;
            }

            // Add click listener
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionButtonPressed);
        }
        else
        {
            Debug.LogError("Action Button not assigned in inspector!");
        }
    }

    private void Update()
    {
        // Check for input while player is in range and UI is not open and quest is NOT done
        if (playerInRange && !isUIOpen && !QuestDone && Input.GetKeyDown(interactKey))
        {
            // Disable player jumping temporarily while UI is open
            if (playerMovement != null)
                playerMovement.SetCanJump(false);

            OpenUI();
        }

        // Close UI with Escape key
        if (isUIOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseUI();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;

            // Get reference to player's movement script
            if (playerMovement == null)
                playerMovement = collision.GetComponent<MovimentacaoExploracao>();

            // Only show popup if quest is not done
            if (!QuestDone)
            {
                ShowPopup();
                Debug.Log($"Press {interactKey} to interact");
            }
            else
            {
                Debug.Log("Quest already completed - interaction disabled");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;

            // Close UI if it's open when player leaves
            if (isUIOpen)
                CloseUI();

            HidePopup();
            playerMovement = null;
        }
    }

    private void ShowPopup()
    {
        if (interactionPopup == null)
        {
            // Try to find popup by tag if not assigned
            interactionPopup = GameObject.FindWithTag("Notification");
        }

        if (interactionPopup != null)
        {
            interactionPopup.SetActive(true);

            // Update popup text
            TextMeshProUGUI popupText = interactionPopup.GetComponentInChildren<TextMeshProUGUI>();
            if (popupText != null)
            {
                popupText.text = $"Press {interactKey} to interact";
            }

            // Handle SpriteRenderer if that's what you're using
            SpriteRenderer spriteRend = interactionPopup.GetComponent<SpriteRenderer>();
            if (spriteRend != null)
            {
                Color color = spriteRend.color;
                color.a = 1f;
                spriteRend.color = color;
            }
        }
    }

    private void HidePopup()
    {
        if (interactionPopup == null)
        {
            interactionPopup = GameObject.FindWithTag("Notification");
        }

        if (interactionPopup != null)
        {
            // Handle SpriteRenderer if that's what you're using
            SpriteRenderer spriteRend = interactionPopup.GetComponent<SpriteRenderer>();
            if (spriteRend != null)
            {
                Color color = spriteRend.color;
                color.a = 0f;
                spriteRend.color = color;
            }
        }
    }

    public void OpenUI()
    {
        // Prevent opening UI if QuestDone is true
        if (QuestDone)
        {
            Debug.Log("Cannot open UI: Quest is already completed!");
            return;
        }

        if (uiPanel == null)
        {
            Debug.LogError("UI Panel not assigned!");
            return;
        }

        isUIOpen = true;
        uiPanel.SetActive(true);

        // Update title if assigned
        if (titleText != null && !string.IsNullOrEmpty(uiTitle))
        {
            titleText.text = uiTitle;
        }

        // Hide popup while UI is open
        HidePopup();

        Debug.Log($"UI opened: {uiTitle}");
    }

    public void CloseUI()
    {
        if (uiPanel == null) return;

        isUIOpen = false;
        uiPanel.SetActive(false);

        // Show popup again if player is still in range and quest is not done
        if (playerInRange && !QuestDone)
        {
            ShowPopup();
        }

        // Re-enable player movement when UI closes
        if (playerMovement != null)
            playerMovement.SetCanJump(true);

        Debug.Log("UI closed");
    }

    private void OnActionButtonPressed()
    {
        if (inventory == null)
        {
            Debug.LogError("Inventory not found! Cannot perform action.");
            return;
        }

        // Prevent action if quest is already done
        if (QuestDone)
        {
            Debug.Log("Quest already completed - cannot perform action");
            return;
        }

        Debug.Log($"Action button pressed: {buttonText}");

        // Check if player has the required item
        if (itemToRemove != null && inventory.TemItem(itemToRemove, 1))
        {
            // Remove the item
            inventory.RemoverItem(itemToRemove, 1);
            Debug.Log($"Removed 1x {itemToRemove.nomeDoItem}");

            // Add progress
            if (!string.IsNullOrEmpty(progressToAdd))
            {
                inventory.AddProgress(progressToAdd);
                Debug.Log($"Added progress: {progressToAdd}");
            }

            // Set quest as done BEFORE closing UI to prevent reopening
            QuestDone = true;

            // Optional: Close UI after successful action
            CloseUI();
        }
        else
        {
            uiTitle = "Você não tem uma poção!";

            // Update title text if assigned
            if (titleText != null)
            {
                titleText.text = uiTitle;
            }

            ShowFailureNotification();
        }
    }

    private void ShowSuccessNotification()
    {
        // Try to find notification object
        GameObject notification = GameObject.FindWithTag("Notification");
        if (notification != null)
        {
            SpriteRenderer spriteRend = notification.GetComponent<SpriteRenderer>();
            if (spriteRend != null)
            {
                // Flash green briefly or show success message
                Color originalColor = spriteRend.color;
                spriteRend.color = Color.green;
                Invoke(nameof(ResetNotificationColor), 0.5f);
            }

            // Update text if there's a TextMeshPro
            TextMeshProUGUI textComponent = notification.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = "Poção doada!";
                Invoke(nameof(ClearNotificationText), 1f);
            }
        }
    }

    private void ShowFailureNotification()
    {
        GameObject notification = GameObject.FindWithTag("Notification");
        if (notification != null)
        {
            SpriteRenderer spriteRend = notification.GetComponent<SpriteRenderer>();
            if (spriteRend != null)
            {
                // Flash red briefly
                Color originalColor = spriteRend.color;
                spriteRend.color = Color.red;
                Invoke(nameof(ResetNotificationColor), 0.5f);
            }

            // Update text if there's a TextMeshPro
            TextMeshProUGUI textComponent = notification.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = "Missing item!";
                Invoke(nameof(ClearNotificationText), 1f);
            }
        }
    }

    private void ResetNotificationColor()
    {
        GameObject notification = GameObject.FindWithTag("Notification");
        if (notification != null)
        {
            SpriteRenderer spriteRend = notification.GetComponent<SpriteRenderer>();
            if (spriteRend != null)
            {
                Color color = spriteRend.color;
                color.a = 0f;
                spriteRend.color = color;
            }
        }
    }

    private void ClearNotificationText()
    {
        GameObject notification = GameObject.FindWithTag("Notification");
        if (notification != null)
        {
            TextMeshProUGUI textComponent = notification.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = "";
            }
        }
    }

    // Check if UI is open (for other scripts)
    public bool IsUIOpen()
    {
        return isUIOpen;
    }

    // Check if player is in range (for other scripts)
    public bool IsPlayerInRange()
    {
        return playerInRange;
    }

    // Check if quest is done
    public bool IsQuestDone()
    {
        return QuestDone;
    }

    // Method to set quest status from other scripts
    public void SetQuestDone(bool value)
    {
        QuestDone = value;

        // If quest becomes done, close UI if open and hide popup
        if (QuestDone)
        {
            if (isUIOpen)
                CloseUI();

            HidePopup();
            Debug.Log("Quest marked as done - interaction disabled");
        }
        else
        {
            // If quest becomes not done, show popup if player is in range
            if (playerInRange)
                ShowPopup();
        }
    }

    // Yarn command to open UI
    [YarnCommand("open_interact_ui")]
    public void OpenUIYarn()
    {
        if (playerInRange && !QuestDone)
            OpenUI();
        else if (QuestDone)
            Debug.LogWarning("Cannot open UI: quest already completed");
        else
            Debug.LogWarning("Cannot open UI: player not in range");
    }

    // Yarn command to close UI
    [YarnCommand("close_interact_ui")]
    public void CloseUIYarn()
    {
        CloseUI();
    }

    // Yarn command to set quest status
    [YarnCommand("set_quest_done")]
    public void SetQuestDoneYarn(bool value)
    {
        SetQuestDone(value);
    }
}