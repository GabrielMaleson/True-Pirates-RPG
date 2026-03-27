using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PartyMenuManager : MonoBehaviour
{
    [Header("Main Panels")]
    public GameObject partyMenuPanel;
    public GameObject menuContainer;   // Shared background container for all subpanels
    public GameObject characterPanel;  // Default group/character view
    public GameObject craftingPanel;
    public GameObject itemsPanel;
    public GameObject equipmentPanel;
    public GameObject MenuOpener;

    [Header("Menu Nav Buttons")]
    public TextMeshProUGUI characterNavText;
    public Image            characterNavBg;
    public TextMeshProUGUI craftingNavText;
    public Image            craftingNavBg;
    public TextMeshProUGUI itemsNavText;
    public Image itemsNavBg;
    public TextMeshProUGUI equipmentNavText;
    public Image equipmentNavBg;

    private static readonly Color NavTextSelected = Color.white;
    private static readonly Color NavTextDeselected = Color.gray;
    private static readonly Color NavBgSelected = new Color(0.19f, 0.19f, 0.19f); // ~#303030
    private static readonly Color NavBgDeselected = new Color(0.12f, 0.12f, 0.12f); // ~#1F1F1F
    private TextMeshProUGUI currentNavText;
    private Image currentNavBg;

    [Header("Canvas References")]
    public Transform partyMenuCanvas;

    private List<PartyMemberButton> partyMemberButtons = new List<PartyMemberButton>();

    [Header("Stats Display Area")]
    public Transform statsDisplayContainer;
    public GameObject statsDisplayPrefab;
    private Dictionary<PartyMemberState, PartyMemberStatsDisplay> statsDisplays =
        new Dictionary<PartyMemberState, PartyMemberStatsDisplay>();

    private PartyMemberState currentSelectedMember;

    [Header("Crafting Display")]
    public Transform craftingListParent;
    public GameObject craftingSlotPrefab;

    [Header("Equipment Display")]
    public Transform equipmentSlotParent;
    public GameObject equipmentSlotPrefab;
    private Dictionary<EquipmentSlot, EquipmentSlotUI> equipmentSlots =
        new Dictionary<EquipmentSlot, EquipmentSlotUI>();

    [Header("HUD Buttons")]
    public GameObject hudButtonsContainer; // Root that holds Group/Objectives/Settings buttons
    public GameObject objectivesButton;
    public CanvasGroup objectivesButtonCanvasGroup;
    public GameObject settingsButton;
    public CanvasGroup settingsButtonCanvasGroup;
    public CanvasGroup menuOpenerCanvasGroup;

    [Header("Gold Count")]
    public TextMeshProUGUI goldCount; // Always visible, root of Party Menu
    public GameObject goldContainer; // Parent GameObject of goldCount — toggled off during battle

    [Header("Inventory Display")]
    public Transform inventoryGrid;
    public GameObject itemSlotPrefab;
    public TextMeshProUGUI goldText;
    private List<SlotUI> inventorySlots = new List<SlotUI>();

    [Header("Item Details")]
    public GameObject itemDetailsPrefab;
    public GameObject partyMemberSelectorPrefab;

    [Header("References")]
    public SistemaInventario inventory;
    public ConfigSceneManager configSceneManager; // Reference to ConfigSceneManager

    [Header("Menu State")]
    public bool canOpenMenu = true; // Can be set to false during cutscenes

    private SlotUI currentlyHighlightedSlot;
    private bool isInBattle = false;

    private void Start()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<SistemaInventario>();

        if (partyMenuCanvas == null)
            partyMenuCanvas = transform;

        if (inventory != null)
        {
            inventory.onInventarioMudou += RefreshInventoryDisplay;
            inventory.onPartyUpdated += CreatePartyMemberDisplays;
        }

        // Find ConfigSceneManager if not assigned
        if (configSceneManager == null)
            configSceneManager = FindFirstObjectByType<ConfigSceneManager>();

        HideAllPanels();
        CreatePartyMemberDisplays();
        RefreshInventoryDisplay();
    }

    private void Update()
    {
        // Only allow menu opening if not in battle and menu can be opened
        if (!isInBattle && canOpenMenu)
        {
            // Toggle with Tab key
            if (Input.GetKeyDown(KeyCode.Tab))
                ToggleMenu();

            // Close with Escape
            if (Input.GetKeyDown(KeyCode.Escape) && partyMenuPanel.activeSelf)
            {
                CloseMenu();
            }

            // Toggle Config with Escape (when menu is closed)
            if (Input.GetKeyDown(KeyCode.Escape) && !partyMenuPanel.activeSelf)
            {
                ToggleSettings();
            }

            // Toggle Inventory with I key
            if (Input.GetKeyDown(KeyCode.I))
            {
                if (!partyMenuPanel.activeSelf)
                    OpenMenu();
                ShowItems();
            }

            // Toggle Party Menu with Tab key (already handled above)

            // Toggle Quests with O key
            if (Input.GetKeyDown(KeyCode.O))
            {
                ToggleObjectives();
            }

            // Toggle Crafting with C key
            if (Input.GetKeyDown(KeyCode.C))
            {
                // Add your crafting toggle logic here
                // For now, this is just a placeholder as requested
            }
        }
    }

    // Called by combat system when battle starts/ends
    public void SetBattleState(bool inBattle)
    {
        isInBattle = inBattle;
        if (inBattle && partyMenuPanel.activeSelf)
            CloseMenu();
        if (hudButtonsContainer != null)
            hudButtonsContainer.SetActive(!inBattle);
        if (goldContainer != null)
            goldContainer.SetActive(!inBattle);
    }

    // Called by cutscene system
    public void SetCanOpenMenu(bool canOpen)
    {
        canOpenMenu = canOpen;
        if (!canOpen && partyMenuPanel.activeSelf)
        {
            CloseMenu();
        }
    }

    private void HideAllPanels()
    {
        if (partyMenuPanel != null) partyMenuPanel.SetActive(false);
        if (craftingPanel != null) craftingPanel.SetActive(false);
        if (itemsPanel != null) itemsPanel.SetActive(false);
        if (equipmentPanel != null) equipmentPanel.SetActive(false);
    }

    private CanvasGroup GetObjectiveCanvasGroup()
    {
        if (ObjectiveManager.Instance == null) return null;
        return ObjectiveManager.Instance.GetComponent<CanvasGroup>();
    }

    public void ToggleObjectives()
    {
        CanvasGroup cg = GetObjectiveCanvasGroup();
        if (cg == null) return;
        bool willShow = cg.alpha <= 0.5f;
        cg.alpha = willShow ? 1f : 0f;
        cg.interactable = willShow;
        cg.blocksRaycasts = willShow;

        // Hide the HUD objectives button while the panel is open (stays in layout — no SetActive)
        if (objectivesButtonCanvasGroup != null)
        {
            objectivesButtonCanvasGroup.alpha = willShow ? 0f : 1f;
            objectivesButtonCanvasGroup.interactable = !willShow;
            objectivesButtonCanvasGroup.blocksRaycasts = !willShow;
        }

        if (willShow && ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.RefreshQuestList();
    }

    private void CloseObjectivesPanel()
    {
        CanvasGroup cg = GetObjectiveCanvasGroup();
        if (cg != null) cg.alpha = 0f;

        // Restore the HUD objectives button
        if (objectivesButtonCanvasGroup != null)
        {
            objectivesButtonCanvasGroup.alpha = 1f;
            objectivesButtonCanvasGroup.interactable = true;
            objectivesButtonCanvasGroup.blocksRaycasts = true;
        }
    }

    public void ToggleSettings()
    {
        if (configSceneManager == null)
        {
            Debug.LogError("ConfigSceneManager não encontrado!");
            return;
        }

        if (configSceneManager.IsConfigLoaded)
        {
            SaveLoadManager.Instance?.SaveGame();
            configSceneManager.DeleteConfigScene();
        }
        else
        {
            configSceneManager.LoadConfigScene();
        }
    }

    private void CreatePartyMemberDisplays()
    {
        foreach (var btn in partyMemberButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        partyMemberButtons.Clear();

        foreach (var display in statsDisplays.Values)
        {
            if (display != null) Destroy(display.gameObject);
        }
        statsDisplays.Clear();

        foreach (var member in inventory.partyMembers)
        {
            if (member != null)
            {
                if (statsDisplayPrefab != null && statsDisplayContainer != null)
                {
                    GameObject displayObj = Instantiate(statsDisplayPrefab, statsDisplayContainer);
                    displayObj.transform.localScale = Vector3.one;
                    PartyMemberStatsDisplay display = displayObj.GetComponent<PartyMemberStatsDisplay>();
                    display.Initialize(member);
                    statsDisplays[member] = display;
                    displayObj.SetActive(true);
                }
            }
        }

        if (inventory.partyMembers.Count > 0 && inventory.partyMembers[0] != null)
        {
            OnPartyMemberSelected(inventory.partyMembers[0]);
        }
    }

    public void OnPartyMemberSelected(PartyMemberState member)
    {
        currentSelectedMember = member;

        if (equipmentPanel.activeSelf)
            UpdateEquipmentDisplay();
    }

    private static void SetCanvasGroupVisible(CanvasGroup cg, bool visible)
    {
        if (cg == null) return;
        cg.alpha          = visible ? 1f : 0f;
        cg.interactable   = visible;
        cg.blocksRaycasts = visible;
    }

    private void SelectNav(TextMeshProUGUI navText, Image navBg)
    {
        if (currentNavText != null) currentNavText.color = NavTextDeselected;
        if (currentNavBg != null) currentNavBg.color = NavBgDeselected;
        currentNavText = navText;
        currentNavBg = navBg;
        if (currentNavText != null) currentNavText.color = NavTextSelected;
        if (currentNavBg != null) currentNavBg.color = NavBgSelected;
    }

    public void ShowCharacter()
    {
        if (currentNavText == characterNavText) return;
        HideSubPanels();
        if (characterPanel != null) characterPanel.SetActive(true);
        SelectNav(characterNavText, characterNavBg);
    }

    public void ShowCrafting()
    {
        if (currentNavText == craftingNavText) return;
        HideSubPanels();
        craftingPanel.SetActive(true);
        SelectNav(craftingNavText, craftingNavBg);
    }

    public void ShowItems()
    {
        if (currentNavText == itemsNavText) return;
        HideSubPanels();
        itemsPanel.SetActive(true);
        SelectNav(itemsNavText, itemsNavBg);
        RefreshInventoryDisplay();
    }

    public void ShowEquipment()
    {
        if (currentNavText == equipmentNavText) return;
        HideSubPanels();
        equipmentPanel.SetActive(true);
        SelectNav(equipmentNavText, equipmentNavBg);

        if (currentSelectedMember != null)
            UpdateEquipmentDisplay();
    }

    // Hides only the content sub-panels
    private void HideSubPanels()
    {
        if (characterPanel != null) characterPanel.SetActive(false);
        if (craftingPanel   != null) craftingPanel.SetActive(false);
        if (itemsPanel     != null) itemsPanel.SetActive(false);
        if (equipmentPanel != null) equipmentPanel.SetActive(false);
    }


    public void UpdateEquipmentDisplay()
    {
        foreach (Transform child in equipmentSlotParent)
            Destroy(child.gameObject);
        equipmentSlots.Clear();

        if (currentSelectedMember == null) return;

        GameObject weaponSlot = Instantiate(equipmentSlotPrefab, equipmentSlotParent);
        EquipmentSlotUI weaponUI = weaponSlot.GetComponent<EquipmentSlotUI>();
        weaponUI.Initialize(EquipmentSlot.Arma, currentSelectedMember.weapon, this);
        equipmentSlots[EquipmentSlot.Arma] = weaponUI;

        GameObject armorSlot = Instantiate(equipmentSlotPrefab, equipmentSlotParent);
        EquipmentSlotUI armorUI = armorSlot.GetComponent<EquipmentSlotUI>();
        armorUI.Initialize(EquipmentSlot.Armadura, currentSelectedMember.armor, this);
        equipmentSlots[EquipmentSlot.Armadura] = armorUI;
    }

    public void RefreshInventoryDisplay()
    {
        if (inventory == null) return;

        string goldStr = "Ouro: " + inventory.moedas.ToString();
        if (goldText != null) goldText.text = goldStr;
        if (goldCount != null) goldCount.text = goldStr;

        foreach (Transform child in inventoryGrid)
        {
            Destroy(child.gameObject);
        }
        inventorySlots.Clear();

        foreach (SlotInventario slot in inventory.inventario)
        {
            GameObject newSlot = Instantiate(itemSlotPrefab, inventoryGrid);
            SlotUI slotUI = newSlot.GetComponent<SlotUI>();

            slotUI.partyMenuManager = this;
            slotUI.itemDetailsPrefab = itemDetailsPrefab;
            slotUI.partyMemberSelectorPrefab = partyMemberSelectorPrefab;

            slotUI.ConfigurarSlot(slot);
            inventorySlots.Add(slotUI);
        }

        currentlyHighlightedSlot = null;
    }

    public void HighlightSlot(SlotUI selectedSlot)
    {
        if (currentlyHighlightedSlot != null)
        {
            currentlyHighlightedSlot.SetHighlight(false);
        }

        currentlyHighlightedSlot = selectedSlot;
        if (currentlyHighlightedSlot != null)
        {
            currentlyHighlightedSlot.SetHighlight(true);
        }
    }

    public void UnequipItem(DadosItem item, EquipmentSlot slot)
    {
        if (currentSelectedMember == null) return;

        if (slot == EquipmentSlot.Arma)
        {
            currentSelectedMember.UnequipWeapon();
        }
        else if (slot == EquipmentSlot.Armadura)
        {
            currentSelectedMember.UnequipArmor();
        }

        if (inventory != null)
        {
            inventory.AdicionarItem(item, 1);
            RefreshInventoryDisplay();
        }

        UpdateEquipmentDisplay();
        UpdateCharacterStats(currentSelectedMember);
    }

    public PartyMemberState GetCurrentSelectedMember()
    {
        return currentSelectedMember;
    }

    public void UpdateCharacterStats(PartyMemberState member)
    {
        if (statsDisplays.ContainsKey(member))
        {
            statsDisplays[member].UpdateDisplay();
        }
    }

    public void ToggleMenu()
    {
        bool isOpen = partyMenuPanel != null && partyMenuPanel.activeSelf;
        Debug.Log($"ToggleMenu chamado — painel estava {(isOpen ? "aberto" : "fechado")}");
        if (isOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    public void OpenMenu()
    {
        if (isInBattle || !canOpenMenu)
        {
            Debug.Log($"OpenMenu bloqueado — isInBattle={isInBattle}, canOpenMenu={canOpenMenu}");
            return;
        }
        SFXManager.Instance?.Play(SFXManager.Instance.uiForward);

        CloseObjectivesPanel();
        HideAllPanels();
        SetCanvasGroupVisible(objectivesButtonCanvasGroup, false);
        SetCanvasGroupVisible(settingsButtonCanvasGroup, false);
        SetCanvasGroupVisible(menuOpenerCanvasGroup, false);

        Debug.Log("OpenMenu: ativando partyMenuPanel");
        partyMenuPanel.SetActive(true);

        CanvasGroup canvasGroup = partyMenuPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        if (menuContainer != null) menuContainer.SetActive(true);

        CreatePartyMemberDisplays();
        RefreshInventoryDisplay();

        // Default to character/group view
        currentNavText = null;
        ShowCharacter();
    }

    public void CloseMenu()
    {
        Debug.Log("CloseMenu chamado");
        SFXManager.Instance?.Play(SFXManager.Instance.uiBackward);
        if (menuContainer != null) menuContainer.SetActive(false);
        currentNavText = null;
        HideAllPanels();
        SetCanvasGroupVisible(objectivesButtonCanvasGroup, true);
        SetCanvasGroupVisible(settingsButtonCanvasGroup, true);
        SetCanvasGroupVisible(menuOpenerCanvasGroup, true);
    }

    public void CloseGame()
    {
        SaveLoadManager.Instance?.SaveGame();
        StartCoroutine(QuitAfterDelay(0.2f));
    }

    public void SaveGame()
    {
        SaveLoadManager.Instance?.SaveGame();
    }

    private IEnumerator QuitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene("TitleScreen");
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.onInventarioMudou -= RefreshInventoryDisplay;
        }
    }
}