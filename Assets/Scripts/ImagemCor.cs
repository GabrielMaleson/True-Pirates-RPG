using UnityEngine;
using UnityEngine.UI;

public class ImagemSlot : MonoBehaviour
{
    private Image imagem;

    private void Awake()
    {
        imagem = GetComponent<Image>();
    }

    public void MudarCor(Color cor)
    {
        if (imagem != null)
        {
            imagem.color = cor;
        }
    }

    public void ResetarCor()
    {
        if (imagem != null)
        {
            imagem.color = Color.white;
        }
    }
}
