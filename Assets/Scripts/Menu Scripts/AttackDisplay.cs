using UnityEngine;
using TMPro;
using UnityEngine.UI; // Add this for Image

public class AttackDisplay : MonoBehaviour
{
    [Header("UI Elements")]
    public Image iconImage; // NEW: Icon image
    public TextMeshProUGUI attackNameText;
    public TextMeshProUGUI apCostText;
    public TextMeshProUGUI damageText;
    public TextMeshProUGUI accuracyText;
    public TextMeshProUGUI descriptionText;

    public void Initialize(AttackFile attack)
    {
        // Set icon
        if (iconImage != null)
        {
            if (attack.icon != null)
            {
                iconImage.sprite = attack.icon;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                // Optionally hide or show a default icon
                iconImage.gameObject.SetActive(false);
            }
        }

        attackNameText.text = attack.attackName;
        apCostText.text = $"AP: {attack.actionPointCost}";

        // Set description
        if (descriptionText != null)
        {
            if (!string.IsNullOrEmpty(attack.description))
            {
                descriptionText.text = attack.description;
                descriptionText.gameObject.SetActive(true);
            }
            else
            {
                descriptionText.gameObject.SetActive(false);
            }
        }

        // Calculate total damage from effects
        int totalDamage = 0;

        foreach (var effect in attack.effects)
        {
            // Add to damage total
            if (effect.effectType == EffectType.Damage || effect.effectType == EffectType.Attack)
            {
                totalDamage += effect.value;
            }
        }

        damageText.text = $"DMG: {totalDamage}";

        // Show average accuracy
        if (attack.effects.Count > 0)
        {
            float avgAccuracy = 0;
            foreach (var effect in attack.effects)
            {
                avgAccuracy += effect.accuracy;
            }
            avgAccuracy /= attack.effects.Count;
            accuracyText.text = $"ACC: {avgAccuracy}%";
        }
    }
}