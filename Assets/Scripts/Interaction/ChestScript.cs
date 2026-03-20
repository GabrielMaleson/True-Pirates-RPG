using UnityEngine;
using TMPro;

public class ChestScript : MonoBehaviour
{
    public DadosItem item; //ScriptableObject
    public int quantidade = 1;

    public SpriteRenderer spriteRenderer;
    public Sprite openchest;
    private bool ChestOpen = false;
    private bool playerInRange = false;

    // Reference to player's movement script to prevent jumping
    private MovimentacaoExploracao playerMovement;

    private void Update()
    {
        // Check for input in Update while player is in range and chest is not open
        if (playerInRange && !ChestOpen && Input.GetKeyDown(KeyCode.Space))
        {
            // Disable player jumping temporarily
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

            // Get reference to player's movement script
            if (playerMovement == null)
                playerMovement = collision.GetComponent<MovimentacaoExploracao>();

            if (!ChestOpen)
            {
                ShowPopup();
                Debug.Log("aperte space bar pra abrir o baú. no jogo final vai aparecer um icone mais relevante");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;

            // Clear reference
            playerMovement = null;

            HidePopup();
        }
    }

    private void TryOpenChest()
    {
        SistemaInventario inventario = FindFirstObjectByType<SistemaInventario>();

        if (inventario != null)
        {
            inventario.AdicionarItem(item, quantidade);
            spriteRenderer.sprite = openchest;
            ChestOpen = true;
            SistemaInventario inventory = SistemaInventario.Instance;
            inventory.AddProgress("piece1");
            Debug.Log("chest aberto");
            HidePopup();

            // Re-enable jumping
            if (playerMovement != null)
                playerMovement.SetCanJump(true);
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