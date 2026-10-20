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

    [Header("Impact Polish")]
    [SerializeField] private Color manaFlashColor = Color.white;
    [SerializeField] private float manaFlashDuration = 0.2f;
    [Tooltip("How much the bar bumps when mana changes.")]
    [SerializeField] private float manaPulseAmount = 0.06f;
    [SerializeField] private float manaPulseDuration = 0.22f;

    private int maxMana = 5; // same as ManaSystem MAX_MANA
    private int currentMana;
    private float manaPercentage;

    private int lastMana = -1;
    private float manaFlashTimer;
    private float manaPulseTimer;
    private Vector3 sliderBaseScale = Vector3.one;
    private bool sliderBaseScaleCached;

    private void Start()
    {
        if (manaSlider != null)
        {
            sliderBaseScale = manaSlider.transform.localScale;
            sliderBaseScaleCached = true;

            manaSlider.maxValue = maxMana;
            manaSlider.value = GetHeroCurrentMana();
        }
    }

    private void Update()
    {
        if (manaSystem == null || manaSlider == null || manaFillImage == null)
            return;

        currentMana = GetHeroCurrentMana();
        maxMana = manaSystem.MaxMana;
        if (manaSlider.maxValue != maxMana)
            manaSlider.maxValue = maxMana;
        manaPercentage = (float)currentMana / maxMana;

        // Any change in mana (gain or spend) triggers a flash and a bump.
        if (lastMana >= 0 && currentMana != lastMana)
        {
            manaFlashTimer = manaFlashDuration;
            manaPulseTimer = manaPulseDuration;
        }

        lastMana = currentMana;

        if (manaFlashTimer > 0f)
            manaFlashTimer -= Time.deltaTime;

        if (manaPulseTimer > 0f)
            manaPulseTimer -= Time.deltaTime;

        // Smooth slider update
        manaSlider.value = Mathf.Lerp(manaSlider.value, currentMana, Time.deltaTime * sliderSmoothSpeed);

        // One smooth bump per change.
        if (sliderBaseScaleCached)
        {
            if (manaPulseTimer > 0f)
            {
                float t = Mathf.Clamp01(manaPulseTimer / Mathf.Max(0.0001f, manaPulseDuration));
                float bump = 1f + Mathf.Sin(t * Mathf.PI) * manaPulseAmount;

                manaSlider.transform.localScale = sliderBaseScale * bump;
            }
            else
            {
                manaSlider.transform.localScale = sliderBaseScale;
            }
        }

        // Flash white on change, otherwise settle to normal / empty colour.
        if (manaFlashTimer > 0f)
        {
            manaFillImage.color = manaFlashColor;
        }
        else
        {
            Color targetColor = (currentMana <= 0) ? lowManaColor : normalColor;
            manaFillImage.color = Color.Lerp(manaFillImage.color, targetColor, Time.deltaTime * colorLerpSpeed);
        }
    }

    public int GetHeroCurrentMana()
    {
        if (manaSystem == null) return 0;
        return manaSystem.CurrentMana;
    }
}
