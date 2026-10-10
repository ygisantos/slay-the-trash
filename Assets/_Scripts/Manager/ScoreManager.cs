using UnityEngine;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Base Scores")]
    public int enemiesBaseScore = 10;
    public int bossBaseScore = 100;
    public int eventBaseScore = 50;
    public int healthBaseScore = 5;
    public int floorBaseScore = 200;   // NEW SCORE TYPE

    [Header("Counters")]
    public int enemiesCount = 0;
    public int bossCount = 0;
    public int eventCount = 0;
    public int healthCount = 0;
    public int floorCount = 0;         // NEW COUNTER

    [Header("Skill Tree Bonuses")]
    [Tooltip("Points skills: added to the count (e.g. 3+1).")]
    public int enemiesPointsBonus = 0;
    public int floorPointsBonus = 0;

    [Tooltip("Multiplier skills: added to the base value (e.g. X10+0.5).")]
    public float enemiesBonus = 0f;
    public float bossBonus = 0f;
    public float eventBonus = 0f;
    public float healthBonus = 0f;
    public float floorBonus = 0f;

    // Points only apply once at least one was earned.
    public int EnemiesScoreCount => enemiesCount > 0 ? enemiesCount + enemiesPointsBonus : 0;
    public int FloorScoreCount => floorCount > 0 ? floorCount + floorPointsBonus : 0;

    [Header("Multiplier")]
    public float multiplier = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ─────────────────────────────────────────────
    // ADD SCORE COUNTERS
    // ─────────────────────────────────────────────

    public void AddEnemyKill(int count = 1) => enemiesCount += count;
    public void AddBossKill(int count = 1) => bossCount += count;
    public void AddEventComplete(int count = 1) => eventCount += count;
    public void AddHealthBonus(int count = 1) => healthCount += count;
    public void AddFloorClear(int count = 1) => floorCount += count;   // NEW

    // ─────────────────────────────────────────────
    // SKILL TREE BONUSES (scoring skills)
    // ─────────────────────────────────────────────

    public void RefreshSkillBonuses()
    {
        enemiesBonus = bossBonus = eventBonus = healthBonus = floorBonus = 0f;
        enemiesPointsBonus = floorPointsBonus = 0;

        List<string> unlocked = FBAuthentication.Instance != null
            ? FBAuthentication.Instance.GetUnlockedSkillIds()
            : null;

        if (unlocked == null)
            return;

        if (unlocked.Contains("waste_collector")) enemiesPointsBonus += enemiesCount;
        if (unlocked.Contains("recycling_chain")) enemiesBonus += 0.5f;
        if (unlocked.Contains("clean_sweep")) floorBonus += 15f;
        if (unlocked.Contains("green_progress")) floorPointsBonus += 5;
        if (unlocked.Contains("eco_champion")) bossBonus += 15f;
        if (unlocked.Contains("eco_wisdom")) eventBonus += 15f;
        if (unlocked.Contains("healthy_planet")) healthBonus += 10f;
    }

    // ─────────────────────────────────────────────
    // TOTAL SCORE CALCULATION
    // ─────────────────────────────────────────────

    public int GetTotalScore()
    {
        float total = 0f;

        total += EnemiesScoreCount * (enemiesBaseScore + enemiesBonus);
        total += bossCount * (bossBaseScore + bossBonus);
        total += eventCount * (eventBaseScore + eventBonus);
        total += healthCount * (healthBaseScore + healthBonus);
        total += FloorScoreCount * (floorBaseScore + floorBonus);   // NEW

        return Mathf.RoundToInt(total * multiplier);
    }

    public void ResetScores()
    {
        enemiesCount = 0;
        bossCount = 0;
        eventCount = 0;
        healthCount = 0;
        floorCount = 0;   // RESET NEW TYPE
    }
}
