using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlotUI : MonoBehaviour
{
    public Image imagemIcone;
    public TextMeshProUGUI textoQuantidade;

    public void ConfigurarSlot(SlotInventario slot)
    {
        if(slot != null && slot.dadosDoItem != null)
        {
            //1. Liga o icone e define a imagem correta
            imagemIcone.enabled = true;
            imagemIcone.sprite = slot.dadosDoItem.icone;

            //2.Define a quantidade
            if(slot.quantidade > 1)
            {
                textoQuantidade.text = slot.quantidade.ToString();
            }
            else
            {
                //Não mostra o valor se não for empilhavel
                textoQuantidade.text = "";
            }
        }
        else
        {
            //Se o slot estiver vazio
            imagemIcone.enabled = false;
            textoQuantidade.text = "";
        }
    }
}
