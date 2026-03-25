using UnityEngine;
using TMPro;

public class ChestScript : MonoBehaviour
{
    public DadosItem item;
    public int quantidade = 1;

    [Tooltip("Unique ID for this chest. Used to remember if it was opened after a scene reload.")]
    public string chestID;

    public SpriteRenderer spriteRenderer;
    public Sprite openchest;
    private bool ChestOpen = false;
    private bool playerInRange = false;

    private MovimentacaoExploracao playerMovement;

    private void Start()
    {
        // If this chest was already opened in a previous visit, restore that state
        if (!string.IsNullOrEmpty(chestID) && SistemaInventario.Instance != null)
        {
            if (SistemaInventario.Instance.GetGameProgress().Contains(chestID))
            {
                ChestOpen = true;
                if (spriteRenderer != null && openchest != null)
                    spriteRenderer.sprite = openchest;
            }
        }
    }

    private void Update()
    {
        if (playerInRange && !ChestOpen && Input.GetKeyDown(KeyCode.Space))
        {
            if (playerMovement != null)
                playerMovement.SetCanJump(false);

            TryOpenChest();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;

            if (playerMovement == null)
                playerMovement = collision.GetComponent<MovimentacaoExploracao>();

            if (!ChestOpen)
                ShowPopup();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
            playerMovement = null;
            HidePopup();
        }
    }

    private void TryOpenChest()
    {
        SistemaInventario inventory = SistemaInventario.Instance;

        if (inventory == null)
        {
            Debug.LogError("SistemaInventario não encontrado!");
            return;
        }

        SFXManager.Instance?.Play(SFXManager.Instance.chestDoorOpen);
        SFXManager.Instance?.Play(SFXManager.Instance.pieceCraftFound2);
        inventory.AdicionarItem(item, quantidade);

        if (spriteRenderer != null && openchest != null)
            spriteRenderer.sprite = openchest;

        ChestOpen = true;

        // Persist the opened state using a unique ID
        if (!string.IsNullOrEmpty(chestID))
            inventory.AddProgress(chestID);

        HidePopup();
        Debug.Log($"Baú {chestID} aberto.");
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
