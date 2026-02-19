using UnityEngine;

public class TesteInventario : MonoBehaviour
{
    public SistemaInventario inventario;

    public DadosItem espada;
    public DadosItem escudo;
    public DadosItem livro;
    public DadosItem pote;

    private void Start()
    {
        inventario.AdicionarItem(espada, 1);
        inventario.AdicionarItem(escudo, 1);
        inventario.AdicionarItem(livro, 1);
        inventario.AdicionarItem(pote, 1);
    }
}
