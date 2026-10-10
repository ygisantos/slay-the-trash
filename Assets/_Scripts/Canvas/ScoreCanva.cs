using UnityEngine;
using System.Globalization;
using TMPro;

public class ScoreCanvas : MonoBehaviour
{
    [Header("Score Breakdown Text Fields")]
    public TMP_Text floorBaseText;
    public TMP_Text floorCountText;
    public TMP_Text floorTotalText;

    public TMP_Text enemiesBaseText;
    public TMP_Text enemiesCountText;
    public TMP_Text enemiesTotalText;

    public TMP_Text bossBaseText;
    public TMP_Text bossCountText;
    public TMP_Text bossTotalText;

    public TMP_Text eventBaseText;
    public TMP_Text eventCountText;
    public TMP_Text eventTotalText;

    public TMP_Text healthBaseText;
    public TMP_Text healthCountText;
    public TMP_Text healthTotalText;

    [Header("Multiplier & Total")]
    public TMP_Text multiplierText;
    public TMP_Text totalScoreText;

    private ScoreManager score => ScoreManager.Instance;

    void OnEnable()
    {
        score?.RefreshSkillBonuses();
    }

    // Shows the skill bonus next to the base, e.g. "X10+0.5".
    private static string FormatBase(int baseScore, float bonus)
    {
        return bonus > 0f
            ? "X" + baseScore + "+" + bonus.ToString("0.##", CultureInfo.InvariantCulture)
            : "X" + baseScore;
    }

    private static string FormatCount(int count, int pointsBonus)
    {
        return pointsBonus > 0 && count > 0
            ? count + "+" + pointsBonus
            : count.ToString();
    }

    private static string FormatTotal(int scoreCount, int baseScore, float bonus)
    {
        return Mathf.RoundToInt(scoreCount * (baseScore + bonus)).ToString();
    }

    void Update()
    {
        if (score == null) return;

        // BASE
        enemiesBaseText.text = FormatBase(score.enemiesBaseScore, score.enemiesBonus);
        bossBaseText.text = FormatBase(score.bossBaseScore, score.bossBonus);
        eventBaseText.text = FormatBase(score.eventBaseScore, score.eventBonus);
        healthBaseText.text = FormatBase(score.healthBaseScore, score.healthBonus);
        floorBaseText.text = FormatBase(score.floorBaseScore, score.floorBonus);

        // COUNTS
        enemiesCountText.text = FormatCount(score.enemiesCount, score.enemiesPointsBonus);
        bossCountText.text = score.bossCount.ToString();
        eventCountText.text = score.eventCount.ToString();
        healthCountText.text = score.healthCount.ToString();
        floorCountText.text = FormatCount(score.floorCount, score.floorPointsBonus);

        // TOTALS
        enemiesTotalText.text = FormatTotal(score.EnemiesScoreCount, score.enemiesBaseScore, score.enemiesBonus);
        bossTotalText.text = FormatTotal(score.bossCount, score.bossBaseScore, score.bossBonus);
        eventTotalText.text = FormatTotal(score.eventCount, score.eventBaseScore, score.eventBonus);
        healthTotalText.text = FormatTotal(score.healthCount, score.healthBaseScore, score.healthBonus);
        floorTotalText.text = FormatTotal(score.FloorScoreCount, score.floorBaseScore, score.floorBonus);

        // FINAL TOTAL
        totalScoreText.text = score.GetTotalScore().ToString();
    }
    public void HideCanva()
    {
        // gameObject.SetActive(false);
        SoundManager.Instance.PlaySound(SoundType.CLICK);
        ScoreManager.Instance?.ResetScores();
        Transitioner.Instance.TransitionToScene("MainMenuScene"); 
    }
}
