using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Discards all remaining cards in hand, and deals damage to targets (e.g. all enemies)
/// scaled by the number of discarded cards.
/// </summary>
public class DiscardHandAndDamageEffect : Effect
{
    [SerializeField] private int damagePerCard = 6;
    [SerializeField] private int baseDamage = 0;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        int cardCount = 0;
        if (CardSystem.Instance != null)
        {
            cardCount = CardSystem.Instance.HandCount;
        }

        // First add action to discard all remaining cards
        ActionSystem.Instance.AddReaction(new DiscardAllCardsGA());

        int totalDamage = baseDamage + (cardCount * damagePerCard);

        if (CharacterStats.IsHeroAttack(caster, targets))
            totalDamage = CharacterStats.ScaleDamage(totalDamage);

        return new DealDamageGA(totalDamage, targets, caster);
    }
}

