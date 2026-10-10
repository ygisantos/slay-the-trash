using UnityEngine;

public class HeroSystem : Singleton<HeroSystem>
{
    [field : SerializeField] public HeroView HeroView { get; private set; }
    void OnEnable()
    {
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
        ActionSystem.SubscribeReaction<PlayCardGA>(PlayCardPostReaction, ReactionTiming.POST);
    }
    void OnDisable()
    {
        ActionSystem.UnSubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.UnSubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
        ActionSystem.UnSubscribeReaction<PlayCardGA>(PlayCardPostReaction, ReactionTiming.POST);
    }
    public void Setup(HeroData heroData)
    {
        HeroView.Setup(heroData);

        GameplaySkills.Refresh();
        HeroView.AddHealth(GameplaySkills.BonusMaxHealth);

        if (GameplaySkills.StartShield > 0)
            HeroView.AddStatusEffect(StatusEffectType.ARMOR, GameplaySkills.StartShield);
    }

    private void PlayCardPostReaction(PlayCardGA playCardGA)
    {
        HeroView.Heal(GameplaySkills.HealthPerCard);
    }
    private void EnemyTurnPreReaction(EnemyTurnGA enemyTurnGA)
    {
        if (CardSystem.Instance == null || !CardSystem.Instance.IsCombatActive) return;
        DiscardAllCardsGA discardAllCardsGA = new();
        ActionSystem.Instance.AddReaction(discardAllCardsGA);
    }

    private void EnemyTurnPostReaction(EnemyTurnGA enemyTurnGA)
    {
        if (CardSystem.Instance == null || !CardSystem.Instance.IsCombatActive) return;

        int burnStacks = HeroView.GetStatusEffectStacks(StatusEffectType.BURN);
        if (burnStacks > 0)
        {
            ApplyBurnGA applyBurnGA = new(burnStacks, HeroView);
            ActionSystem.Instance.AddReaction(applyBurnGA);
        }
        DrawCardsGA drawCardsGA = new(GameplaySkills.HandSize);
        ActionSystem.Instance.AddReaction(drawCardsGA);
    }
}
