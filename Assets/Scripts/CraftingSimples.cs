using UnityEngine;

public class CraftingSimples : MonoBehaviour
{
    public SistemaInventario inventario;

    [Header("Itens Necessários")]
    public DadosItem madeira;

    [Header("Item craftado")]
    public DadosItem flecha;

    public int custo = 1;
    public int quantidadeProduzida = 5;

    public void CraftarFlechas()
    {
        if (inventario.TemItem(madeira, custo))
        {
            inventario.RemoverItem(madeira, custo);
            inventario.AdicionarItem(flecha, quantidadeProduzida);
            Debug.Log($"Sucesso! {quantidadeProduzida}x Flechas criadas!");
        }
        else
        {
            Debug.Log("Falha: Você não tem os itens necessários!");
        }
    }
}
