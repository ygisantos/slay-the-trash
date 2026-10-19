using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Effect that grants a specified amount of energy (mana) to the hero when the card is played.
/// Used by E-Waste cards such as EWASTE_BATTERY.
/// </summary>
public class GainManaEffect : Effect
{
    [SerializeField] private int manaAmount;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new GainManaGA(manaAmount);
    }
}

