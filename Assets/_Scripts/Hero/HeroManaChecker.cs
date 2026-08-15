using UnityEngine;
using UnityEngine.UI;

public class HeroManaChecker : MonoBehaviour
{
    [Header("Assign Mana System Here")]
    [SerializeField] private ManaSystem manaSystem;

    [Header("Mana Slider UI")]
    [SerializeField] private Slider manaSlider;
    [SerializeField] private float sliderSmoothSpeed = 5f;

    [Header("Mana Bar Visuals")]
    [SerializeField] private Image manaFillImage;
    [SerializeField] private Color normalColor = Color.cyan;
    [SerializeField] private Color lowManaColor = Color.red;
    [SerializeField] private float colorLerpSpeed = 5f;

    private int maxMana = 5; // same as ManaSystem MAX_MANA
    private int currentMana;
    private float manaPercentage;

    private void Start()
    {
        if (manaSlider != null)
        {
            manaSlider.maxValue = maxMana;
            manaSlider.value = GetHeroCurrentMana();
        }
    }

    private void Update()
    {
        if (manaSystem == null || manaSlider == null || manaFillImage == null)
            return;

        currentMana = GetHeroCurrentMana();
        manaPercentage = (float)currentMana / maxMana;

        // Smooth slider update
        manaSlider.value = Mathf.Lerp(manaSlider.value, currentMana, Time.deltaTime * sliderSmoothSpeed);

        // Smooth color change based on % mana
        Color targetColor = (currentMana <= 0) ? lowManaColor : normalColor;
        manaFillImage.color = Color.Lerp(manaFillImage.color, targetColor, Time.deltaTime * colorLerpSpeed);
    }

    public int GetHeroCurrentMana()
    {
        if (manaSystem == null) return 0;

        // Accessing private currentMana using reflection
        return typeof(ManaSystem)
            .GetField("currentMana", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(manaSystem) is int mana ? mana : 0;
    }
}
