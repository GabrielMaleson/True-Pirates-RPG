using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemDetails : MonoBehaviour
{
    [Header("UI Elements")]
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    public TextMeshProUGUI itemStatsText;

    public void Initialize(DadosItem item)
    {
        if (item == null) return;

        if (itemIcon != null)
        {
            itemIcon.sprite = item.icone;
            itemIcon.gameObject.SetActive(item.icone != null);
        }

        if (itemNameText != null)
            itemNameText.text = item.nomeDoItem;

        if (itemDescriptionText != null)
            itemDescriptionText.text = item.descricao;

        if (itemStatsText != null)
        {
            if (item.ehEquipavel && item.modificadoresStats.Count > 0)
            {
                string stats = "";
                foreach (var mod in item.modificadoresStats)
                {
                    string sign = mod.valorModificador > 0 ? "+" : "";
                    string percent = mod.tipoModificador == ModifierType.Percentual ? "%" : "";
                    stats += $"{mod.statType}: {sign}{mod.valorModificador}{percent}\n";
                }
                if (item.nivelRequerido > 0)
                    stats += $"Nível requerido: {item.nivelRequerido}";
                itemStatsText.text = stats.TrimEnd();
                itemStatsText.gameObject.SetActive(true);
            }
            else
            {
                itemStatsText.gameObject.SetActive(false);
            }
        }
    }
}
