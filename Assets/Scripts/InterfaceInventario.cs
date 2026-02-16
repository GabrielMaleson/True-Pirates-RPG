using TMPro;
using UnityEngine;

public class InterfaceInventario : MonoBehaviour
{
    public SistemaInventario sistemaInventario;
    public Transform containerGrid;
    public GameObject prefabSlot;

    [Header("Economia")]
    public TextMeshProUGUI textoMoedas;

    private void Start()
    {
        //Inscrição no sistema de Eventos
        //Sempre que o inventário mudar, a iterface redesenha automaticamente
        sistemaInventario.onInventarioMudou += AtualizarInterface;

        //Atualiza o inventario ao começar o jogo
        AtualizarInterface();
    }

    public void AtualizarInterface()
    {
        //1. Atualiza as Moedas
        if(textoMoedas != null)
        {
            textoMoedas.text = "Ouro: " + sistemaInventario.moedas.ToString();
        }

        //2. Limpeza do grid
        foreach(Transform item in containerGrid)
        {
            Destroy(item.gameObject);
        }

        //3. Monta o inventário
        foreach(SlotInventario slot in sistemaInventario.inventario)
        {
            GameObject novoSlot = Instantiate(prefabSlot, containerGrid);
            novoSlot.GetComponent<SlotUI>().ConfigurarSlot(slot);
        }
    }
}
