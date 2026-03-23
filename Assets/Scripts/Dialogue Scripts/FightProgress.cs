using UnityEngine;
using System.Collections;
using Yarn.Unity;

public class FightProgress : MonoBehaviour
{
    public DialogueManager dialogueManager;
    public SistemaInventario sistemaInventario;
    public GameObject scamps;
    public GameObject foe1;
    public GameObject foe2;
    public GameObject Player;
    public Transform retreat;
    public GameObject Killer;
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
        if (sistemaInventario != null && sistemaInventario.HasProgress("mutanttime"))
        {
            NextMoment();
        }

        if (sistemaInventario != null && sistemaInventario.HasProgress("mutantbosstime"))
        {
            NextestMoment();
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
        if (!collision.gameObject.CompareTag("Player")) return;
        if (doingthing) return; // ignore re-entry from Rigidbody2D.WakeUp() re-trigger

        doingthing = true;
        if (dialogueManager != null)
            dialogueManager.StartDialogue("cantprogressyet");
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            doingthing = false;
    }

    private void NextMoment()
    {
        scamps.SetActive(true);
        if (foe1 != null)
            foe1.SetActive(true);
        if (Killer != null)
        {
            Destroy(this);
        }
    }

    private void NextestMoment()
    {
        if (scamps != null)
            Destroy(scamps);
        if (foe2 != null)
            foe2.SetActive(true);
        Destroy(this);
    }

}