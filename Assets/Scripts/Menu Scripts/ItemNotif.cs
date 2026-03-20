using UnityEngine;
using TMPro;

public class ControladorTextoDano : MonoBehaviour
{
    public float velocidadeMovimento = 50f;
    public float velocidadeFade = 2f;
    public float tempoDeVida = 1f;

    private TextMeshProUGUI textoDano;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        textoDano = GetComponent<TextMeshProUGUI>();
        Destroy(textoDano, tempoDeVida);
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.up * velocidadeMovimento * Time.deltaTime);
        textoDano.alpha = Mathf.MoveTowards(textoDano.alpha, 0, velocidadeFade * Time.deltaTime);
    }
}
