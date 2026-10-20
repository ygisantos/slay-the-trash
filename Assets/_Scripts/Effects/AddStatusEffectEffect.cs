using System.Collections.Generic;
using UnityEngine;

public class AddStatusEffectEffect : Effect
{
    [SerializeField] private StatusEffectType statusEffectType;
    [SerializeField] public int stackCount;
    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        int amount = stackCount;

        // Shield gained by the hero scales with the selected character.
        // Burn and enemy-applied effects are left untouched.
        if (statusEffectType == StatusEffectType.ARMOR && TargetsHero(targets))
            amount = CharacterStats.ScaleShield(stackCount);

        return new AddStatusEffectGA(statusEffectType, amount, targets);
    }

    private static bool TargetsHero(List<CombatantView> targets)
    {
        if (targets == null)
            return false;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] is HeroView)
                return true;
        }

        return false;
    }
}
