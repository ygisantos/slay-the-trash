using System;
using UnityEngine;

public class OnEnemyAttackCondition : PerkCondition
{
    public override bool SubConditionsIsMet(GameAction gameAction)
    {
      return true;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<AttackHeroGA>(reaction, reactionTiming);

    }

    public override void UnSubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnSubscribeReaction<AttackHeroGA> (reaction, reactionTiming);
    }
}
