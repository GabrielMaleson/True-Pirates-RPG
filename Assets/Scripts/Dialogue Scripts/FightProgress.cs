using UnityEngine;
using System.Collections;
using Yarn.Unity;

public class FightProgress : MonoBehaviour
{
    public DialogueManager dialogueManager;
    public SistemaInventario sistemaInventario;
    public GameObject scamps;
    public GameObject foe;
    public GameObject Player;
    public Transform retreat;
    public bool isBoss;
    public bool doingthing = false;

    // Freeze position variables
    private bool isPositionFrozen = false;
    private float frozenXPosition;
    private MovimentacaoExploracao playerMovement;
    private DialogueRunner dialogueRunner;
    private bool commandsRegistered = false;

    void Start()
    {
        // Find references
        if (dialogueManager == null)
            dialogueManager = FindFirstObjectByType<DialogueManager>();

        if (sistemaInventario == null)
            sistemaInventario = FindFirstObjectByType<SistemaInventario>();

        if (Player == null)
            Player = GameObject.FindGameObjectWithTag("Player");

        if (Player != null)
            playerMovement = Player.GetComponent<MovimentacaoExploracao>();

        // Try to register commands immediately
        RegisterCommands();
    }

    private void RegisterCommands()
    {
        if (commandsRegistered) return;

        // Get DialogueRunner from DialogueManager
        if (dialogueManager != null && dialogueManager.dialogueRunner != null)
        {
            dialogueRunner = dialogueManager.dialogueRunner;

            dialogueRunner.AddCommandHandler("unfreeze", UnfreezePlayer);
            commandsRegistered = true;
            Debug.Log("FightProgress: Registered unfreeze command successfully");
        }
        else
        {
            // Try again in a moment
            Invoke(nameof(RegisterCommands), 0.5f);
        }
    }

    private void Update()
    {
        if (sistemaInventario != null && sistemaInventario.HasProgress("mutantfought"))
        {
            NextMoment();
        }

        // Keep player at frozen X position if frozen
        if (isPositionFrozen && Player != null)
        {
            Vector3 pos = Player.transform.position;
            pos.x = frozenXPosition;
            Player.transform.position = pos;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            StartCoroutine(MoveToPoint(Player, retreat.position, true));
            if (dialogueManager != null)
                dialogueManager.StartDialogue("cantprogressyet");
        }
    }

    private void NextMoment()
    {
        if (scamps != null)
            Destroy(scamps);
        if (foe != null)
            foe.SetActive(true);
        Destroy(this);
    }

    private IEnumerator MoveToPoint(GameObject character, Vector3 target, bool useAnimator)
    {
        if (doingthing)
        {
            yield break;
        }
        doingthing = true;

        // Disable player movement script temporarily
        if (playerMovement != null)
            playerMovement.enabled = false;

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
            doingthing = false;
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

        // Ensure exact position
        character.transform.position = new Vector3(lockedTarget.x, originalY, character.transform.position.z);

        if (useAnimator && anim != null)
        {
            anim.SetBool("Andando", false);
        }

        // Freeze X position after reaching destination
        FreezePlayerPosition();

        doingthing = false;
    }

    private void FreezePlayerPosition()
    {
        if (Player != null)
        {
            isPositionFrozen = true;
            frozenXPosition = Player.transform.position.x;
        }
    }

    private void UnfreezePlayer()
    {
        isPositionFrozen = false;

        // Re-enable player movement
        if (playerMovement != null)
            playerMovement.enabled = true;

        Debug.Log("Player X position unfrozen");
    }

    // Yarn command handler - must match the signature expected by AddCommandHandler
    private void UnfreezePlayer(string[] parameters)
    {
        Debug.Log("Unfreeze command received with " + parameters.Length + " parameters");
        UnfreezePlayer();
    }

    private void OnDestroy()
    {
        // Unregister Yarn command
        if (dialogueRunner != null && commandsRegistered)
        {
            dialogueRunner.RemoveCommandHandler("unfreeze");
            Debug.Log("FightProgress: Unregistered unfreeze command");
        }

        // Cancel any pending invoke
        CancelInvoke();
    }
}