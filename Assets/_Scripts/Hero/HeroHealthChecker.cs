using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeroHealthChecker : MonoBehaviour
{
    [Header("Assign Hero GameObject Here")]
    [SerializeField] private HeroView heroView;

    [Header("UI Panel")]
    [SerializeField] private GameObject scorePanel;
    [SerializeField] private TextMeshProUGUI resultTitle;

    [Header("Health Bar UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private float sliderSmoothSpeed = 5f;

    [Header("Health Info (Read Only)")]
    [SerializeField] private int currentHealth;
    [SerializeField] private int maxHealth;
    [SerializeField] private float healthPercentage;

    [Header("Health Bar Visuals")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float colorLerpSpeed = 5f;

    [Header("Impact Polish")]
    [SerializeField] private Color damageFlashColor = Color.white;
    [SerializeField] private float damageFlashDuration = 0.18f;
    [Tooltip("Below this health fraction the bar starts pulsing.")]
    [SerializeField] private float lowHealthPulseThreshold = 0.3f;
    [SerializeField] private float lowHealthPulseSpeed = 6f;
    [SerializeField] private float lowHealthPulseAmount = 0.03f;

    private int lastHealth = -1;
    private float damageFlashTimer;
    private Vector3 sliderBaseScale = Vector3.one;
    private bool sliderBaseScaleCached;

    [Header("Health Bar Text")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private bool showMaxHealth = true; // toggle if you want "current / max" or just "current"
    private bool gameStart = false;

    private bool hasLost = false;

    private void Start()
    {
        resultTitle.SetText("Dungeon Cleared!");
        if (heroView != null)
        {
            maxHealth = heroView.MaxHealth;
            currentHealth = heroView.CurrentHealth;
        }

        if (healthSlider != null)
        {
            sliderBaseScale = healthSlider.transform.localScale;
            sliderBaseScaleCached = true;
            healthSlider.value = GetHeroHealthPercentage(); // just 0-1 normalized
        }
    }
    public void GameStart()
    {
        gameStart = true;
    }

    private void Update()
    {
        if (gameStart)
        {
            if (heroView != null)
            {
                currentHealth = heroView.CurrentHealth;
                maxHealth = heroView.MaxHealth;
                ScoreManager.Instance.healthCount = currentHealth;
                healthPercentage = GetHeroHealthPercentage();

                // Detect a health drop so the bar can flash.
                if (lastHealth >= 0 && currentHealth < lastHealth)
                    damageFlashTimer = damageFlashDuration;

                lastHealth = currentHealth;

                if (damageFlashTimer > 0f)
                    damageFlashTimer -= Time.deltaTime;

                // Smooth slider update (0-1)
                if (healthSlider != null)
                {
                    healthSlider.value = Mathf.Lerp(healthSlider.value, healthPercentage, Time.deltaTime * sliderSmoothSpeed);

                    // Heartbeat while the hero is low on health.
                    if (healthPercentage > 0f && healthPercentage <= lowHealthPulseThreshold)
                    {
                        float pulse =
                            1f + Mathf.Sin(Time.time * lowHealthPulseSpeed) * lowHealthPulseAmount;

                        healthSlider.transform.localScale = sliderBaseScale * pulse;
                    }
                    else if (sliderBaseScaleCached)
                    {
                        healthSlider.transform.localScale = sliderBaseScale;
                    }
                }

                // Update health text
                if (healthText != null)
                {
                    if (showMaxHealth)
                        healthText.text = currentHealth + " / " + maxHealth;
                    else
                        healthText.text = currentHealth.ToString();
                }

                // Smooth color change based on % HP, unless a hit just flashed the bar.
                if (healthFillImage != null)
                {
                    if (damageFlashTimer > 0f)
                    {
                        healthFillImage.color = damageFlashColor;
                    }
                    else
                    {
                        Color targetColor = (healthPercentage <= lowHealthPulseThreshold)
                            ? lowHealthColor
                            : normalColor;

                        healthFillImage.color = Color.Lerp(healthFillImage.color, targetColor, Time.deltaTime * colorLerpSpeed);
                    }
                }

                if (currentHealth <= 0 && !hasLost)
                {
                    OnHeroDead();
                }
            }
        }
    }

    public int GetHeroCurrentHealth()
    {
        return heroView != null ? heroView.CurrentHealth : 0;
    }

    public int GetHeroMaxHealth()
    {
        return heroView != null ? heroView.MaxHealth : 0;
    }

    public float GetHeroHealthPercentage()
    {
        if (heroView != null && heroView.MaxHealth > 0)
            return (float)heroView.CurrentHealth / heroView.MaxHealth;
        return 0f;
    }

    public bool IsHeroAlive()
    {
        return heroView != null && heroView.CurrentHealth > 0;
    }

    private void OnHeroDead()
    {
        hasLost = true;
        TurnBanner.Hide();

        MatchSetupSystem match = FindFirstObjectByType<MatchSetupSystem>();
        if (match != null)
            match.EndCombat();
        else
            CardSystem.Instance?.EndCombat();

        scorePanel.SetActive(true);
        if (resultTitle != null) resultTitle.text = "You Lost!";
    }
}
