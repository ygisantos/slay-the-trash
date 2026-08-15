using UnityEngine;
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

    void Update()
    {
        if (score == null) return;

        // BASE
        enemiesBaseText.text = "X" + score.enemiesBaseScore.ToString();
        bossBaseText.text = "X" + score.bossBaseScore.ToString();
        eventBaseText.text = "X" + score.eventBaseScore.ToString();
        healthBaseText.text = "X" + score.healthBaseScore.ToString();
        floorBaseText.text = "X" + score.floorBaseScore.ToString();

        // COUNTS
        enemiesCountText.text = score.enemiesCount.ToString();
        bossCountText.text = score.bossCount.ToString();
        eventCountText.text = score.eventCount.ToString();
        healthCountText.text = score.healthCount.ToString();
        floorCountText.text = score.floorCount.ToString();

        // TOTALS
        enemiesTotalText.text = (score.enemiesCount * score.enemiesBaseScore).ToString();
        bossTotalText.text = (score.bossCount * score.bossBaseScore).ToString();
        eventTotalText.text = (score.eventCount * score.eventBaseScore).ToString();
        healthTotalText.text = (score.healthCount * score.healthBaseScore).ToString();
        floorTotalText.text = (score.floorCount * score.floorBaseScore).ToString();

        // FINAL TOTAL
        totalScoreText.text = score.GetTotalScore().ToString();
    }
    public void HideCanva()
    {
        gameObject.SetActive(false);
    }
}
