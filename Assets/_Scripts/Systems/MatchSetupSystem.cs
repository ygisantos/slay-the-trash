using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchSetupSystem : MonoBehaviour
{
    [SerializeField] private HeroData heroData;
    [SerializeField] private PerkData perkData;

    [SerializeField] private List<EnemyData> enemyDatas;
    public HeroHealthChecker hero;
    public EnemyBoardView enemy;
    private void Start()
    {
        heroData = PlayerManager.Instance.GetHeroData();
        HeroSystem.Instance.Setup(heroData);
        hero.GameStart();
        perkData = PlayerManager.Instance.GetPerkData();
        PerkSystem.Instance.AddPerk(new Perk(perkData));
    }

    public void StartGame()
    {
        EnemyWaves();
        CardSystem.Instance.BeginCombat(CardManager.Instance.GetCardDataList(10));
        DrawCardsGA drawCardsGA = new(5);
        ActionSystem.Instance.Perform(drawCardsGA);
    }

    public void EndCombat()
    {
        if (enemy != null)
            enemy.gameStart = false;
        if (CardSystem.Instance != null)
            CardSystem.Instance.EndCombat();
    }
    public void EnemyWaves()
    {
        enemyDatas = EnemyManager.Instance.GetEnemyDataList(MapManager.Instance.level);
        EnemySystem.Instance.Setup(enemyDatas);
        if (enemy != null)
            enemy.PrepareForCombat();
    }

}
