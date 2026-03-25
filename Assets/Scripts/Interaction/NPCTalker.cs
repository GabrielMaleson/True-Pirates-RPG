using UnityEngine;
using System.Collections.Generic;
using Yarn.Unity;

public class NPCTalker : MonoBehaviour
{
    public DialogueManager dialogue;
    public bool hasstarted = false;
    private bool playerInRange = false;
    public string thething;
    public bool joodiescript = false;
    public bool DestroyThe = false;
    public GameObject Activator;
    public GameObject Activator2;
    public GameObject Killer;
    public bool compassquest = false;
    public bool chestquest = false;

    [Header("Itens para dar via Yarn")]
    public List<DadosItem> giveableItems = new List<DadosItem>();

    // Static list populated just before dialogue starts so the command works after Destroy(this)
    private static List<DadosItem> activeItems = new List<DadosItem>();

    [YarnCommand("additem")]
    public static void AddItem(string itemId, int quantidade = 1)
    {
        DadosItem found = activeItems.Find(i => i != null && (i.id == itemId || i.nomeDoItem == itemId));
        if (found == null)
        {
            Debug.LogWarning($"additem: item '{itemId}' não encontrado na lista do NPC.");
            return;
        }
        if (SistemaInventario.Instance != null)
            SistemaInventario.Instance.AdicionarItem(found, quantidade);
    }
    void Start()
    {
        dialogue = FindFirstObjectByType<DialogueManager>();

        SistemaInventario inventory = SistemaInventario.Instance;
    }

    void Update()
    {
        if (!hasstarted && playerInRange && !chestquest && !compassquest)
        {
            ShowPopup();
        }

        if (SistemaInventario.Instance.HasProgress("compass"))
        {
            compassquest = false;
        }

        if (SistemaInventario.Instance.HasProgress("chested"))
        {
            chestquest = false;
        }
        if (!hasstarted && !chestquest && !compassquest && playerInRange && Input.GetKeyDown(KeyCode.Space))
        {
            hasstarted = true;
            activeItems.Clear();
            activeItems.AddRange(giveableItems);
            MovimentacaoExploracao.StopForDialogue();
            dialogue.StartDialogue(thething);
            HidePopup();
            if (Activator != null)
            {
                Activator.SetActive(true);

            }
            if (Activator2 != null)
            {
                Activator2.SetActive(true);
            }
            if (Killer != null)
            {
                Killer.SetActive(false);
                Destroy(this);
            }
        }

        if (!hasstarted && playerInRange && joodiescript)
        {
            hasstarted = true;
            activeItems.Clear();
            activeItems.AddRange(giveableItems);
            MovimentacaoExploracao.StopForDialogue();
            dialogue.StartDialogue(thething);
            if (Activator != null)
            {
                Activator.SetActive(true);

            }
            if (Activator2 != null)
            {
                Activator2.SetActive(true);
            }
            if (Killer != null)
            {
                Killer.SetActive(false);
                Destroy(this);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
            HidePopup();
        }
    }

    private void ShowPopup()
    {
        GameObject popup = GameObject.FindWithTag("Notification");
        if (popup != null)
        {
            SpriteRenderer spriteRend = popup.GetComponent<SpriteRenderer>();
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
        GameObject popup = GameObject.FindWithTag("Notification");
        if (popup != null)
        {
            SpriteRenderer spriteRend = popup.GetComponent<SpriteRenderer>();
            if (spriteRend != null)
            {
                Color color = spriteRend.color;
                color.a = 0f;
                spriteRend.color = color;
            }
        }
    }

}