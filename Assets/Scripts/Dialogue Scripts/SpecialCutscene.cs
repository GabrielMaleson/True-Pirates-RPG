using System.Collections;
using UnityEngine;
using Yarn.Unity;
using UnityEngine.SceneManagement;

public class SpecialCutsceneScript : MonoBehaviour
{
    [Header("Waypoints")]
    public Transform pointA;
    public Transform pointB;
    public Transform pointC;
    public Transform pointD;
    public Transform pointE;
    public Transform playerPoint;
    public Transform playerPointB;

    [Header("Doors")]
    public GameObject doorClose;
    public GameObject doorOpen;

    [Header("Characters")]
    public GameObject simon;
    public GameObject timon;
    public GameObject momo;
    public GameObject player;

    [Header("UI")]
    public Sprite surpriseSprite;

    [Header("Encounter")]
    public EncounterFile encounterToStart; // Assign in inspector for the encounter command

    [Header("References")]
    public DialogueRunner dialogueRunner;

    private MovimentacaoExploracao playerMovement;
    private SpriteRenderer playerSpriteRenderer;
    private Animator playerAnimator;
    private Animator simonAnimator;
    private Animator timonAnimator;
    private Animator momoAnimator;
    private SpriteRenderer simonSpriteRenderer;
    private SpriteRenderer timonSpriteRenderer;
    private SpriteRenderer momoSpriteRenderer;
    private GameObject notificationObject;
    private SpriteRenderer notificationSpriteRenderer;

    // Reference to the EncounterStarter to reuse its functionality
    private EncounterStarter encounterStarter;

    private void Start()
    {
        if (dialogueRunner == null)
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();

        // Try to find an EncounterStarter in the scene (or create a temporary one)
        encounterStarter = FindFirstObjectByType<EncounterStarter>();

        FindNotificationObject();
        FindPlayerComponents();
        FindSimonComponents();
        FindTimonComponents();
        FindMomoComponents();

        if (doorClose != null) doorClose.SetActive(true);
        if (doorOpen != null) doorOpen.SetActive(false);

        RegisterYarnCommands();
    }

    private void FindNotificationObject()
    {
        if (player == null) return;

        notificationObject = GameObject.FindGameObjectWithTag("Notification");

        if (notificationObject != null)
        {
            notificationSpriteRenderer = notificationObject.GetComponent<SpriteRenderer>();
            Debug.Log($"Found Notification object: {notificationObject.name}");
        }
        else
        {
            Debug.LogError("Could not find Notification object as child of Player!");
        }
    }

    private void FindPlayerComponents()
    {
        if (player != null)
        {
            playerMovement = player.GetComponent<MovimentacaoExploracao>();
            playerSpriteRenderer = player.GetComponent<SpriteRenderer>();
            playerAnimator = player.GetComponent<Animator>();
        }
    }

    private void FindSimonComponents()
    {
        if (simon != null)
        {
            simonAnimator = simon.GetComponent<Animator>();
            simonSpriteRenderer = simon.GetComponent<SpriteRenderer>();
            simon.SetActive(false);
        }
    }

    private void FindTimonComponents()
    {
        if (timon != null)
        {
            timonAnimator = timon.GetComponent<Animator>();
            timonSpriteRenderer = timon.GetComponent<SpriteRenderer>();
            timon.SetActive(false);
        }
    }

    private void FindMomoComponents()
    {
        if (momo != null)
        {
            momoAnimator = momo.GetComponent<Animator>();
            momoSpriteRenderer = momo.GetComponent<SpriteRenderer>();
        }
    }

    private void RegisterYarnCommands()
    {
        if (dialogueRunner != null)
        {
            dialogueRunner.AddCommandHandler("surprise", Surprise);
            dialogueRunner.AddCommandHandler("runaway", Runaway);
            dialogueRunner.AddCommandHandler("dooropen", DoorOpen);
            dialogueRunner.AddCommandHandler("doorclose", DoorClose);
            dialogueRunner.AddCommandHandler("simonenter", SimonEnter);
            dialogueRunner.AddCommandHandler("timonenter", TimonEnter);
            dialogueRunner.AddCommandHandler("momogrr", MomoGrr);
            dialogueRunner.AddCommandHandler("simonflip", SimonFlip);
            dialogueRunner.AddCommandHandler("guysleave", GuysLeave);
            dialogueRunner.AddCommandHandler("playerphew", PlayerPhew);
            dialogueRunner.AddCommandHandler("playerflip", PlayerFlip);
            dialogueRunner.AddCommandHandler("startencounter", StartEncounter); // NEW COMMAND

            Debug.Log("Yarn commands registered successfully");
        }
    }

    private void Surprise()
    {
        Debug.Log("Surprise command called");

        if (notificationSpriteRenderer != null && surpriseSprite != null)
        {
            notificationSpriteRenderer.sprite = surpriseSprite;
            Debug.Log($"Notification sprite changed to: {surpriseSprite.name}");
        }
        Debug.Log("tried enabling surprise");
        Color color = notificationSpriteRenderer.color;
        color.a = 1f;
        notificationSpriteRenderer.color = color;
    }

    private void Runaway()
    {
        if (player != null && playerPoint != null && playerMovement != null)
        {
            StartCoroutine(MoveToPoint(player, playerPoint.position, true));
        }
        StartCoroutine(MoveToPointAfterDelay(momo, playerPointB.position, 0.3f));
        if (notificationSpriteRenderer != null)
            notificationSpriteRenderer.gameObject.SetActive(false);
    }

    private void DoorOpen()
    {
        if (doorClose != null) doorClose.SetActive(false);
        if (doorOpen != null) doorOpen.SetActive(true);
    }

    private void DoorClose()
    {
        if (doorClose != null) doorClose.SetActive(true);
        if (doorOpen != null) doorOpen.SetActive(false);
    }

    private void SimonEnter()
    {
        if (simon != null && pointA != null)
        {
            simon.SetActive(true);
            StartCoroutine(MoveToPointAfterDelay(simon, pointA.position, 0.3f));
        }
    }

    private void TimonEnter()
    {
        if (timon != null && pointB != null)
        {
            timon.SetActive(true);
            StartCoroutine(MoveToPointAfterDelay(timon, pointB.position, 0.3f));
        }
    }

    private void MomoGrr()
    {
        StartCoroutine(MoveToPointAfterDelay(momo, pointD.position, 0.3f));
        StartCoroutine(MoveToPointAfterDelay(simon, pointE.position, 0.3f));
    }

    private void SimonFlip()
    {
        if (simonSpriteRenderer != null)
        {
            simonSpriteRenderer.flipX = !simonSpriteRenderer.flipX;
        }
    }

    private void PlayerFlip()
    {
        if (playerSpriteRenderer != null)
            playerSpriteRenderer.flipX = !playerSpriteRenderer.flipX;
    }

    private void GuysLeave()
    {
        if (simon != null && timon != null && pointC != null)
        {
            StartCoroutine(MoveBothToPointAndDeactivate());
        }
    }

    private void PlayerPhew()
    {
        if (player != null && pointD != null && playerMovement != null)
        {
            StartCoroutine(MoveToPoint(player, pointB.position, true));
        }
    }

    // NEW: Yarn command to start an encounter
    private void StartEncounter()
    {
        Debug.Log("StartEncounter command called");

        if (encounterToStart == null)
        {
            Debug.LogError("No encounter assigned to SpecialCutsceneScript! Please assign an EncounterFile in the inspector.");
            return;
        }

        // Disable player movement during transition
        if (playerMovement != null)
            playerMovement.enabled = false;

        // Use existing EncounterStarter functionality or create our own
        StartCoroutine(StartEncounterCoroutine());
    }

    // Overload that accepts an encounter name (if you want to specify which encounter)
    private void StartEncounter(string encounterName)
    {
        Debug.Log($"StartEncounter command called with: {encounterName}");

        // You could implement a dictionary of encounters here
        // For now, just use the assigned one
        StartEncounter();
    }

    private IEnumerator StartEncounterCoroutine()
    {
        // Get or create EncounterData
        EncounterData encounterData = FindFirstObjectByType<EncounterData>();
        if (encounterData == null)
        {
            GameObject dataObj = new GameObject("EncounterData");
            DontDestroyOnLoad(dataObj);
            encounterData = dataObj.AddComponent<EncounterData>();
        }

        // Get player inventory
        SistemaInventario inventory = FindFirstObjectByType<SistemaInventario>();
        if (inventory == null)
        {
            Debug.LogError("No player inventory found!");
            yield break;
        }

        // Store encounter starter reference (this game object)
        encounterData.encounterStarterObject = gameObject;

        // Store player inventory
        encounterData.playerInventory = inventory;

        // Store the encounter file
        encounterData.encounterFile = encounterToStart;

        // Store player's current party state
        encounterData.playerPartyMembers = inventory.GetPartyMembersForCombat();

        // Store enemy data from encounter file
        encounterData.enemyPartyMembers.Clear();
        encounterData.enemyPrefabs.Clear();

        foreach (var enemyData in encounterToStart.enemies)
        {
            if (enemyData.characterData != null)
            {
                PartyMemberState enemyState = new PartyMemberState(enemyData.characterData, enemyData.level);
                if (enemyData.overrideHP > 0)
                {
                    enemyState.currentHP = enemyData.overrideHP;
                }
                encounterData.enemyPartyMembers.Add(enemyState);
                encounterData.enemyPrefabs.Add(enemyData.enemyPrefab);
            }
        }

        encounterData.CalculateRewards();

        // Check if combat scene exists in build
        if (!Application.CanStreamedLevelBeLoaded("Combat"))
        {
            Debug.LogError("Combat scene is not in Build Settings!");
            yield break;
        }

        // Create scene manager and load combat
        GameObject sceneObj = new GameObject("PreviousScene");
        sceneObj.AddComponent<PreviousScene>();
        sceneObj.GetComponent<PreviousScene>().UnloadScene();

        // Load combat scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Combat", LoadSceneMode.Additive);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Debug.Log("Combat scene loaded successfully");
    }

    private IEnumerator MoveToPointAfterDelay(GameObject character, Vector3 target, float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(MoveToPoint(character, target, true));
    }

    private IEnumerator MoveToPoint(GameObject character, Vector3 target, bool useAnimator)
    {
        Animator anim = character.GetComponent<Animator>();
        SpriteRenderer sprite = character.GetComponent<SpriteRenderer>();
        float speed = 5f;

        float originalY = character.transform.position.y;
        Vector3 lockedTarget = new Vector3(target.x, originalY, target.z);

        if (useAnimator && anim != null)
        {
            anim.SetBool("Andando", true);
        }

        Vector3 startPos = character.transform.position;
        float distance = Mathf.Abs(lockedTarget.x - startPos.x);
        float duration = distance / speed;
        float elapsed = 0f;

        if (distance < 0.01f)
        {
            if (useAnimator && anim != null)
                anim.SetBool("Andando", false);
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float newX = Mathf.Lerp(startPos.x, lockedTarget.x, t);
            character.transform.position = new Vector3(newX, originalY, character.transform.position.z);

            if (sprite != null)
            {
                float direction = lockedTarget.x - character.transform.position.x;
                if (Mathf.Abs(direction) > 0.1f)
                {
                    sprite.flipX = direction < 0;
                }
            }

            yield return null;
        }

        character.transform.position = new Vector3(lockedTarget.x, originalY, character.transform.position.z);

        if (useAnimator && anim != null)
        {
            anim.SetBool("Andando", false);
        }
    }

    private IEnumerator MoveBothToPointAndDeactivate()
    {
        Coroutine timonMove = StartCoroutine(MoveToPoint(timon, pointC.position, true));
        StartCoroutine(MoveToPointAfterDelay(simon, pointB.position, 0.3f));

        yield return timonMove;

        yield return new WaitForSeconds(0.4f);

        if (timon != null) timon.SetActive(false);
    }

    private void OnDestroy()
    {
        if (dialogueRunner != null)
        {
            dialogueRunner.RemoveCommandHandler("surprise");
            dialogueRunner.RemoveCommandHandler("runaway");
            dialogueRunner.RemoveCommandHandler("dooropen");
            dialogueRunner.RemoveCommandHandler("doorclose");
            dialogueRunner.RemoveCommandHandler("simonenter");
            dialogueRunner.RemoveCommandHandler("timonenter");
            dialogueRunner.RemoveCommandHandler("simonflip");
            dialogueRunner.RemoveCommandHandler("guysleave");
            dialogueRunner.RemoveCommandHandler("playerphew");
            dialogueRunner.RemoveCommandHandler("playerflip");
            dialogueRunner.RemoveCommandHandler("startencounter");
            dialogueRunner.RemoveCommandHandler("momogrr");
        }
    }
}