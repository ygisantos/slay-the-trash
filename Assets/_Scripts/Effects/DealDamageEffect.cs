using UnityEngine;
using System.Collections.Generic;

public class DealDamageEffect : Effect
{
    [SerializeField] private int damageAmount;
    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        // Outgoing hero damage scales with the selected character. Enemy attacks and
        // cards that damage the hero itself are left untouched.
        int amount = CharacterStats.IsHeroAttack(caster, targets)
            ? CharacterStats.ScaleDamage(damageAmount)
            : damageAmount;

        DealDamageGA dealDamageGA = new(amount, targets, caster);
        return dealDamageGA;
    }
}
