using UnityEngine;
using System.Collections;
using System.Globalization;
using DG.Tweening;
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

    [Header("Animation")]
    [SerializeField] private float startDelay = 0.4f;
    [SerializeField] private float rowInterval = 0.5f;
    [SerializeField] private float countDuration = 2f;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float cameraShakeDuration = 0.2f;
    [SerializeField] private float cameraShakeMagnitude = 0.12f;
    [SerializeField] private float textShakeStrength = 12f;

    private ScoreManager score => ScoreManager.Instance;

    private class Row
    {
        public TMP_Text baseText;
        public TMP_Text countText;
        public TMP_Text totalText;
        public string baseString;
        public int count;
        public int pointsBonus;
        public int total;
    }

    private readonly System.Collections.Generic.List<Row> rows = new System.Collections.Generic.List<Row>();
    private readonly System.Collections.Generic.Dictionary<Transform, Vector3> baseScales =
        new System.Collections.Generic.Dictionary<Transform, Vector3>();
    private Coroutine sequence;

    void OnEnable()
    {
        score?.RefreshSkillBonuses();

        if (sequence != null)
            StopCoroutine(sequence);

        sequence = StartCoroutine(PlaySequence());
    }

    void OnDisable()
    {
        sequence = null;
        ResetTweens();
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

    private static int CalculateTotal(int scoreCount, int baseScore, float bonus)
    {
        return Mathf.RoundToInt(scoreCount * (baseScore + bonus));
    }

    private void BuildRows()
    {
        rows.Clear();

        rows.Add(new Row
        {
            baseText = floorBaseText, countText = floorCountText, totalText = floorTotalText,
            baseString = FormatBase(score.floorBaseScore, score.floorBonus),
            count = score.floorCount, pointsBonus = score.floorPointsBonus,
            total = CalculateTotal(score.FloorScoreCount, score.floorBaseScore, score.floorBonus)
        });

        rows.Add(new Row
        {
            baseText = enemiesBaseText, countText = enemiesCountText, totalText = enemiesTotalText,
            baseString = FormatBase(score.enemiesBaseScore, score.enemiesBonus),
            count = score.enemiesCount, pointsBonus = score.enemiesPointsBonus,
            total = CalculateTotal(score.EnemiesScoreCount, score.enemiesBaseScore, score.enemiesBonus)
        });

        rows.Add(new Row
        {
            baseText = bossBaseText, countText = bossCountText, totalText = bossTotalText,
            baseString = FormatBase(score.bossBaseScore, score.bossBonus),
            count = score.bossCount,
            total = CalculateTotal(score.bossCount, score.bossBaseScore, score.bossBonus)
        });

        rows.Add(new Row
        {
            baseText = eventBaseText, countText = eventCountText, totalText = eventTotalText,
            baseString = FormatBase(score.eventBaseScore, score.eventBonus),
            count = score.eventCount,
            total = CalculateTotal(score.eventCount, score.eventBaseScore, score.eventBonus)
        });

        rows.Add(new Row
        {
            baseText = healthBaseText, countText = healthCountText, totalText = healthTotalText,
            baseString = FormatBase(score.healthBaseScore, score.healthBonus),
            count = score.healthCount,
            total = CalculateTotal(score.healthCount, score.healthBaseScore, score.healthBonus)
        });
    }

    private IEnumerator PlaySequence()
    {
        if (score == null)
            yield break;

        ResetTweens();
        BuildRows();

        // Start hidden, with every number at 0.
        foreach (Row row in rows)
        {
            SetAlpha(row.baseText, 0f);
            SetAlpha(row.countText, 0f);
            SetAlpha(row.totalText, 0f);
            SetText(row.baseText, row.baseString);
            SetText(row.countText, "0");
            SetText(row.totalText, "0");
        }

        SetAlpha(totalScoreText, 0f);
        SetText(totalScoreText, "0");

        yield return new WaitForSecondsRealtime(startDelay);

        Coroutine lastCount = null;

        foreach (Row row in rows)
        {
            Row current = row;

            Shake.Instance?.ShakeCamera(cameraShakeDuration, cameraShakeMagnitude);
            SoundManager.Instance?.PlaySound(SoundType.CLICK);

            StartCoroutine(Reveal(current.baseText));
            StartCoroutine(Reveal(current.countText));
            StartCoroutine(Reveal(current.totalText));

            // Counts and totals tick up together while the next rows appear.
            StartCoroutine(CountUp(current.countText, current.count, n => FormatCount(n, current.pointsBonus)));
            lastCount = StartCoroutine(CountUp(current.totalText, current.total, n => n.ToString()));

            yield return new WaitForSecondsRealtime(rowInterval);
        }

        if (lastCount != null)
            yield return lastCount;

        // Final total gets the biggest moment.
        Shake.Instance?.ShakeCamera(cameraShakeDuration * 2f, cameraShakeMagnitude * 2f);
        SoundManager.Instance?.PlaySound(SoundType.CLICK);

        StartCoroutine(Reveal(totalScoreText));
        yield return CountUp(totalScoreText, score.GetTotalScore(), n => n.ToString());

        if (totalScoreText != null)
        {
            totalScoreText.transform.DOKill(true);
            totalScoreText.transform.DOPunchScale(Vector3.one * 0.25f, 0.4f, 6, 0.6f).SetUpdate(true);
        }

        sequence = null;
    }

    // Fades the text in with a pop and a small shake.
    private IEnumerator Reveal(TMP_Text text)
    {
        if (text == null)
            yield break;

        Transform t = text.transform;
        if (!baseScales.TryGetValue(t, out Vector3 scale))
        {
            scale = t.localScale;
            baseScales[t] = scale;
        }

        t.DOKill(true);
        t.localScale = scale * 0.6f;
        t.DOScale(scale, 0.35f).SetEase(Ease.OutBack).SetUpdate(true);
        t.DOShakePosition(0.3f, new Vector3(textShakeStrength, textShakeStrength, 0f), 20, 90f)
            .SetUpdate(true);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(text, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        SetAlpha(text, 1f);
    }

    // Counts from 0 up to the target with an ease-out curve.
    private IEnumerator CountUp(TMP_Text text, int target, System.Func<int, string> format)
    {
        if (text == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < countDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / countDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            text.text = format(Mathf.RoundToInt(target * eased));
            yield return null;
        }

        text.text = format(target);
    }

    private static void SetAlpha(TMP_Text text, float alpha)
    {
        if (text != null)
            text.alpha = alpha;
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    // Finishes any running tweens so every text returns to its resting position and scale.
    private void ResetTweens()
    {
        foreach (Row row in rows)
        {
            row.baseText?.transform.DOKill(true);
            row.countText?.transform.DOKill(true);
            row.totalText?.transform.DOKill(true);
        }

        totalScoreText?.transform.DOKill(true);

        foreach (var pair in baseScales)
            if (pair.Key != null)
                pair.Key.localScale = pair.Value;
    }

    public void HideCanva()
    {
        // gameObject.SetActive(false);
        SoundManager.Instance.PlaySound(SoundType.CLICK);
        ScoreManager.Instance?.ResetScores();
        Transitioner.Instance.TransitionToScene("MainMenuScene"); 
    }
}
