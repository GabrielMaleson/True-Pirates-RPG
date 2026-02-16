using UnityEngine;

public class TesteInventario : MonoBehaviour
{
    public SistemaInventario inventario;

    public DadosItem espada;
    public DadosItem pocao;
    public DadosItem biscoito;

    private void Start()
    {
        inventario.AdicionarItem(espada, 1);
        
        inventario.AdicionarItem(pocao, 3);
        inventario.AdicionarItem(pocao, 5);

        inventario.AdicionarItem(espada, 1);
        inventario.AdicionarItem(biscoito, 1);

        inventario.RemoverItem(pocao, 2);
    }
}
