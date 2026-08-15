using UnityEngine;

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
    // TOTAL SCORE CALCULATION
    // ─────────────────────────────────────────────

    public int GetTotalScore()
    {
        int total = 0;

        total += enemiesCount * enemiesBaseScore;
        total += bossCount * bossBaseScore;
        total += eventCount * eventBaseScore;
        total += healthCount * healthBaseScore;
        total += floorCount * floorBaseScore;   // NEW

        total = Mathf.RoundToInt(total * multiplier);

        return total;
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
