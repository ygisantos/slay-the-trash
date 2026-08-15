using System;
using UnityEngine;

public abstract class PerkCondition
{
    [SerializeField] protected ReactionTiming reactionTiming;
    public abstract void SubscribeCondition(Action<GameAction> reaction);
    public abstract void UnSubscribeCondition(Action<GameAction> reaction);

    public abstract bool SubConditionsIsMet(GameAction gameAction);
}