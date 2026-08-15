using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeroHealthChecker : MonoBehaviour
{
    [Header("Assign Hero GameObject Here")]
    [SerializeField] private HeroView heroView;

    [Header("UI Panel")]
    [SerializeField] private GameObject losePanel;
    [SerializeField] private GameObject scorePanel;

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

    [Header("Health Bar Text")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private bool showMaxHealth = true; // toggle if you want "current / max" or just "current"
    private bool gameStart = false;

    private bool hasLost = false;

    private void Start()
    {
        if (heroView != null)
        {
            maxHealth = heroView.MaxHealth;
            currentHealth = heroView.CurrentHealth;
        }

        if (healthSlider != null)
        {
            healthSlider.value = GetHeroHealthPercentage(); // just 0-1 normalized
        }
    }
    public void GameStart()
    {
        gameStart = true;
    }

    private void Update()
    {
        if ((gameStart))
        {
            if (heroView != null)
            {
                currentHealth = heroView.CurrentHealth;
                maxHealth = heroView.MaxHealth;
                ScoreManager.Instance.healthCount = currentHealth;
                healthPercentage = GetHeroHealthPercentage();

                // Smooth slider update (0-1)
                if (healthSlider != null)
                {
                    healthSlider.value = Mathf.Lerp(healthSlider.value, healthPercentage, Time.deltaTime * sliderSmoothSpeed);
                }

                // Update health text
                if (healthText != null)
                {
                    if (showMaxHealth)
                        healthText.text = currentHealth + " / " + maxHealth;
                    else
                        healthText.text = currentHealth.ToString();
                }

                // Smooth color change based on % HP
                if (healthFillImage != null)
                {
                    Color targetColor = (healthPercentage <= 0.3f) ? lowHealthColor : normalColor;
                    healthFillImage.color = Color.Lerp(healthFillImage.color, targetColor, Time.deltaTime * colorLerpSpeed);
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
        Debug.Log("Hero has died!");

        if (losePanel != null)
        {
            losePanel.SetActive(true);
            scorePanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Lose Panel is not assigned in the Inspector!");
        }
    }
}
