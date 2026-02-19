using UnityEngine;
using TMPro;

public class ChestScript : MonoBehaviour
{
    public DadosItem item; //ScriptableObject
    public int quantidade = 1;

    private SpriteRenderer spriteRenderer;
    public Sprite openchest;
    private bool ChestOpen = false;
    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (item != null)
        {
            spriteRenderer.sprite = item.icone;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            GameObject popup = GameObject.FindWithTag("Notification");
            SpriteRenderer spriteRend = popup.GetComponent<SpriteRenderer>();
            if (spriteRend != null)
            {
                Color color = spriteRend.color;
                color.a = 1f;
                spriteRend.color = color;
                return;
            }

            Debug.Log("aperte space bar pra abrir o baú. no jogo final vai aparecer um icone mais relevante");
            //Procura pelo inventario no gerenciador do jogo
            SistemaInventario inventario = FindFirstObjectByType<SistemaInventario>();

            if (inventario != null && Input.GetKeyDown(KeyCode.Space))
            {
                if (!ChestOpen)
                { 
                inventario.AdicionarItem(item, quantidade);
                spriteRenderer.sprite = openchest;
                ChestOpen = true;
                }
            }
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            GameObject popup = GameObject.FindWithTag("Notification");
            SpriteRenderer spriteRend = popup.GetComponent<SpriteRenderer>();
            if (spriteRend != null)
            {
                Color color = spriteRend.color;
                color.a = 0f;
                spriteRend.color = color;
                return;
            }
        }
    }

}
