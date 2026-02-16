using UnityEngine;

public class MoedaColetavel : MonoBehaviour
{
    public int valor;

    private void Start()
    {
        //Sorteia um valor entre 1 e 9
        valor = Random.Range(1, 10);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            SistemaInventario inventario = FindFirstObjectByType<SistemaInventario>();

            if (inventario != null)
            {
                inventario.ModificadorMoedas(valor);
                Destroy(gameObject);
            }
        }
    }
}
