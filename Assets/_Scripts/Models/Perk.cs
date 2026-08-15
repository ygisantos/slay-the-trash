using UnityEngine;
using System.Collections.Generic;

public class Perk
{
    public Sprite Image => data.Image;
    private readonly PerkData data;
    private readonly PerkCondition condition;
    private readonly AutoTargetEffect effect;
    public Perk(PerkData perkData)
    {
        data = perkData;
        condition = data.PerkCondition;
        effect = data.AutoTargetEffect;
    }
    public void OnAdd()
    {
        condition.SubscribeCondition(Reaction);
    }
    public void OnRemove()
    {
        condition.UnSubscribeCondition(Reaction);
    }
    private void Reaction(GameAction gameAction)
    {
        if (condition.SubConditionsIsMet(gameAction))
        {
            List<CombatantView> targets = new();
            if(data.UseActionCasterAsTarget && gameAction is IHaveCaster haveCaster)
            {
                targets.Add(haveCaster.Caster);
            }
            if(data.UseAutoTarget)
            {
                targets.AddRange(effect.TargetMode.GetTargets());
            }
            GameAction perkEffectAction = effect.Effect.GetGameAction(targets, HeroSystem.Instance.HeroView);
            ActionSystem.Instance.AddReaction(perkEffectAction);
        }
    }
}
