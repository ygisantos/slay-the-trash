using UnityEngine;
using System.Collections.Generic;

public class PerformEffectGA : GameAction
{
    public Effect Effect { get; set; }
    public List<CombatantView> Targets { get; set; }
    public PerformEffectGA(Effect effect, List<CombatantView> targets)
    {
        Effect = effect;
        Targets = targets == null ? null : new(targets);
    }
}
