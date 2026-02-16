using UnityEngine;
using System.Collections.Generic;
using System;

public class SistemaInventario : MonoBehaviour
{
    public List<SlotInventario> inventario = new List<SlotInventario>();

    [Header("Economia")]
    public int moedas = 0;

    //Evento que indica alteração no inventário
    public event Action onInventarioMudou;

    public void AdicionarItem(DadosItem itemParaAdicionar, int quantidade)
    {
        //1. Verificar se o item é empilhavel
        if (itemParaAdicionar.ehEmpilhavel)
        {
            //1.1 Verifica se a mochila possui um item desse tipo
            for (int i = 0; i < inventario.Count; i++) 
            {
                if (inventario[i].dadosDoItem == itemParaAdicionar)
                {
                    inventario[i].AdicionarQuantidade(quantidade);
                    Debug.Log($"Adicionado + {quantidade} ao item {itemParaAdicionar.nomeDoItem}");

                    //Informa a Unity que ocorreu uma alteração no inventário
                    if(onInventarioMudou != null)
                    {
                        onInventarioMudou.Invoke();
                    }

                    return;
                }
            }
        }

        //2. Item não empilhavel ou ainda não possui um igual
        //Criando um novo slot
        SlotInventario novoSlot = new SlotInventario(itemParaAdicionar, quantidade);

        //Adicionado o slot ao inventario
        inventario.Add(novoSlot);

        //Informa a Unity que ocorreu uma alteração no inventário
        if (onInventarioMudou != null)
        {
            onInventarioMudou.Invoke();
        }
    }

    public void RemoverItem(DadosItem item, int quantidade)
    {
        //1. Verifica se o item existe no inventário
        foreach(SlotInventario slot in inventario)
        {
            if (slot.dadosDoItem == item)
            {
                //1.1 Subtrai a quantidade desejada de itens
                slot.SubtrairQuantidade(quantidade);
                Debug.Log($"Subtraido - {quantidade} ao item {item.nomeDoItem}. Total: {slot.quantidade}");

                //Informa a Unity que ocorreu uma alteração no inventário
                if (onInventarioMudou != null)
                {
                    onInventarioMudou.Invoke();
                }


                if (slot.quantidade <= 0)
                {
                    //Remove o item do inventario
                    inventario.Remove(slot);
                    Debug.Log($"Slot removido: {item.nomeDoItem}");

                    //Informa a Unity que ocorreu uma alteração no inventário
                    if (onInventarioMudou != null)
                    {
                        onInventarioMudou.Invoke();
                    }

                    return;
                }
            }
        }
    }

    public void ModificadorMoedas(int valor)
    {
        moedas += valor;

        if(moedas < 0)
        {
            moedas = 0;
        }

        //Informa a Unity que ocorreu uma alteração no inventário
        if (onInventarioMudou != null)
        {
            onInventarioMudou.Invoke();
        }
    }

    public bool TemItem(DadosItem item, int qtd)
    {
        //Verifica se tem o item e quantidade necessaria dentro do inventario
        foreach(SlotInventario slot in inventario)
        {
            if(slot.dadosDoItem == item && slot.quantidade >= qtd)
            {
                return true;
            }
        }

        return false;
    }

    //Quando ocorreu alguma alteração pela Unity, o jogo atualiza o inventario
    private void OnValidate()
    {
        if(Application.isPlaying && onInventarioMudou != null)
        {
            onInventarioMudou.Invoke();
        }
    }
}

