using UnityEngine;

public class ItemColetavel : MonoBehaviour
{
    public DadosItem item; //ScriptableObject
    public int quantidade = 1;

    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if(item != null)
        {
            spriteRenderer.sprite = item.icone;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            //Procura pelo inventario no gerenciador do jogo
            SistemaInventario inventario = FindFirstObjectByType<SistemaInventario>();

            if (inventario != null)
            {
                inventario.AdicionarItem(item, quantidade);
                Destroy(gameObject);
            }
        }
    }
}
