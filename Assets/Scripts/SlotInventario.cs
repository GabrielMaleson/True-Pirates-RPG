using UnityEngine;

[System.Serializable] //OBRIGATÓRIO para visualizar no Inspector
public class SlotInventario
{
    public DadosItem dadosDoItem;
    public int quantidade;

    //Construtor
    public SlotInventario(DadosItem item, int qtd)
    {
        dadosDoItem = item;
        quantidade = qtd;
    }

    public void AdicionarQuantidade(int qtd)
    {
        quantidade += qtd;
    }

    public void SubtrairQuantidade(int qtd)
    {
        quantidade -= qtd;
    }
}
