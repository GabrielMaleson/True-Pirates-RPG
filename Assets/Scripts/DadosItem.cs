using UnityEngine;

[CreateAssetMenu(fileName = "Novo Item", menuName = "Sistema RPG/Item")]
public class DadosItem : ScriptableObject
{
    [Header("Identificação")]
    public string nomeDoItem;
    public string id;
    
    [Header("Visual")]
    public Sprite icone;
    
    [TextArea] 
    public string descricao;

    [Header("Economia e Logistica")]
    public int valorEmOuro;
    public bool ehEmpilhavel;
}