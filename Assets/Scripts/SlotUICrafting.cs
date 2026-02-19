using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlotUICrafting : MonoBehaviour
{
    public Image imagemIcone;
    public TextMeshProUGUI textoQuantidade;
    public Image imagemFundo; // Background image for color changes

    [HideInInspector]
    public SlotInventario slotReferencia;

    private Button botao;

    private void Awake()
    {
        // If imagemFundo is not set, try to get the Image component from this GameObject
        if (imagemFundo == null)
            imagemFundo = GetComponent<Image>();

        // Get or add Button component
        botao = GetComponent<Button>();
        if (botao == null)
        {
            botao = gameObject.AddComponent<Button>();
        }

        // Ensure the button's target graphic is set for visual feedback
        if (botao.targetGraphic == null)
        {
            botao.targetGraphic = imagemFundo;
        }

        // Make sure the button's navigation is set to auto for better UX
        botao.navigation = new Navigation { mode = Navigation.Mode.Automatic };
    }

    public void ConfigurarSlot(SlotInventario slot)
    {
        slotReferencia = slot;

        if (slot != null && slot.dadosDoItem != null)
        {
            // Set icon
            if (imagemIcone != null)
            {
                imagemIcone.enabled = true;
                imagemIcone.sprite = slot.dadosDoItem.icone;
                imagemIcone.preserveAspect = true; // Keep aspect ratio
            }

            // Set quantity text
            if (textoQuantidade != null)
            {
                if (slot.quantidade > 1 || slot.dadosDoItem.ehEmpilhavel)
                {
                    textoQuantidade.text = slot.quantidade.ToString();
                }
                else
                {
                    textoQuantidade.text = "";
                }
            }

            // Enable the button
            if (botao != null)
                botao.interactable = true;

            // Reset color to normal
            MudarCor(Color.white);
        }
        else
        {
            // Empty slot - disable the button
            if (imagemIcone != null)
                imagemIcone.enabled = false;

            if (textoQuantidade != null)
                textoQuantidade.text = "";

            if (botao != null)
                botao.interactable = false;

            slotReferencia = null;
        }
    }

    public void MudarCor(Color cor)
    {
        if (imagemFundo != null)
        {
            imagemFundo.color = cor;
        }
    }
}