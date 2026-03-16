using UnityEngine;
using Yarn.Unity;

public class FightProgress : MonoBehaviour
{
    public DialogueRunner dialogueRunner;
    public SistemaInventario sistemaInventario;
    public GameObject scamps;
    public GameObject foe;
    void Start()
    {
        dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        sistemaInventario = FindFirstObjectByType<SistemaInventario>();
    }

    private void Update()
    {
        if (sistemaInventario.HasProgress("mutantfought"))
        {
            NextMoment();
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            dialogueRunner.StartDialogue("cantprogressyet");
        }
    }

    private void NextMoment()
    {
        Destroy(scamps);
        foe.SetActive(true);
        Destroy(this);
    }

}
