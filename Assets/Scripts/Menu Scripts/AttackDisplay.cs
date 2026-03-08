using UnityEngine;
using TMPro;

public class AttackDisplay : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI attackNameText;
    public TextMeshProUGUI apCostText;
    public TextMeshProUGUI damageText;
    public TextMeshProUGUI accuracyText;

    public void Initialize(AttackFile attack)
    {
        attackNameText.text = attack.attackName;
        apCostText.text = $"AP: {attack.actionPointCost}";

        // Calculate total damage from effects
        int totalDamage = 0;
        foreach (var effect in attack.effects)
        {
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