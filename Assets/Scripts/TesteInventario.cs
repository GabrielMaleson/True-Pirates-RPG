using UnityEngine;

public class TesteInventario : MonoBehaviour
{
    public SistemaInventario inventario;

    public DadosItem espada;

    private void Start()
    {
        inventario.AdicionarItem(espada, 1);
    }
}
