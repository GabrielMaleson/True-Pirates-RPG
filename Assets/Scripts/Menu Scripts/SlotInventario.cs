using UnityEngine;

[System.Serializable] //OBRIGAT�RIO para visualizar no Inspector
public class SlotInventario
{
    public DadosItem dadosDoItem;
    public int quantidade;

    // Runtime only — which party member has this item equipped (null = not equipped)
    [System.NonSerialized] public PartyMemberState equippedTo;

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
