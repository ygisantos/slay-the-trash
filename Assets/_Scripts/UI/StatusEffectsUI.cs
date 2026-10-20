using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;

public class StatusEffectsUI : MonoBehaviour
{
    [SerializeField] private StatusEffectUI statusEffectUIPrefab;
    [SerializeField] private Sprite armorSprite, burnSprite;

    public Sprite BurnSprite => burnSprite;
    public Sprite ArmorSprite => armorSprite;

    private Dictionary<StatusEffectType, StatusEffectUI> statusEffectUIs = new();

    public Slider armorSlider;
    public TMP_Text armorText;
    public int maxShield = 50;

    [Header("Shield Bar Polish")]
    [Tooltip("How long the bar takes to slide to a new value.")]
    [SerializeField] private float armorBarDuration = 0.28f;
    [SerializeField] private float armorPunchAmount = 0.10f;
    [SerializeField] private Color armorFlashColor = Color.white;
    [SerializeField] private float armorFlashDuration = 0.25f;

    private Vector3 armorBaseScale = Vector3.one;
    private bool armorBaseScaleCached;
    private Color armorBaseColor = Color.white;
    private Image armorFill;
    private bool armorFillCached;

    private bool armorUpdated;

    // Skip the reset if a starting shield was already applied before Start ran.
    private void Start()
    {
        if (!armorUpdated)
            UpdateArmorSlider(0);
    }

    public void UpdateStatusEffectUI(StatusEffectType statusEffectType, int stackCount)
    {
        int cappedValue = Mathf.Clamp(stackCount, 0, GetCappedByType(statusEffectType));

        if (statusEffectType == StatusEffectType.ARMOR)
        {
            armorUpdated = true;
            UpdateArmorSlider(cappedValue);
        }

        if (cappedValue == 0)
        {
            if (statusEffectUIs.ContainsKey(statusEffectType))
            {
                StatusEffectUI statusEffectUI = statusEffectUIs[statusEffectType];
                statusEffectUIs.Remove(statusEffectType);
                Destroy(statusEffectUI.gameObject);
            }
        }
        else
        {
            if (!statusEffectUIs.ContainsKey(statusEffectType))
            {
                StatusEffectUI statusEffectUI = Instantiate(statusEffectUIPrefab, transform);
                statusEffectUIs.Add(statusEffectType, statusEffectUI);
            }

            Sprite sprite = GetSpriteByType(statusEffectType);
            statusEffectUIs[statusEffectType].Set(sprite, cappedValue);
        }
    }


    private Sprite GetSpriteByType(StatusEffectType statusEffectType)
    {
        return statusEffectType switch
        {
            StatusEffectType.ARMOR => armorSprite,
            StatusEffectType.BURN => burnSprite,
            _ => null,
        };
    }
    private int GetCappedByType(StatusEffectType statusEffectType)
    {
        return statusEffectType switch
        {
            StatusEffectType.ARMOR => maxShield,
            // Add more capped effects here:
            // StatusEffectType.POISON => maxPoison,
            // StatusEffectType.BURN => maxBurnStacks,
            _ => 9999 // default uncapped value
        };
    }

    public void UpdateArmorSlider(int currentShield)
    {
        if (armorSlider == null) return;

        int target = Mathf.Clamp(currentShield, 0, maxShield);
        int previous = Mathf.RoundToInt(armorSlider.value);

        armorSlider.maxValue = maxShield;

        CacheArmorVisuals();

        // Slide to the new value; the label always shows the true number.
        armorSlider.DOKill();
        armorSlider.DOValue(target, armorBarDuration)
            .SetEase(Ease.OutQuad);

        if (armorText != null)
        {
            armorText.text = $"{target} / {maxShield}";
        }

        // Pop outward when shield is gained, squash inward when it breaks.
        if (target > previous)
            PunchArmor(armorPunchAmount, 0.30f);
        else if (target < previous && target == 0)
            PunchArmor(-armorPunchAmount, 0.35f);

        if (armorFill != null)
        {
            armorFill.DOKill();
            armorFill.color = armorFlashColor;
            armorFill.DOColor(armorBaseColor, armorFlashDuration);
        }
    }

    private void CacheArmorVisuals()
    {
        if (!armorBaseScaleCached)
        {
            armorBaseScale = armorSlider.transform.localScale;
            armorBaseScaleCached = true;
        }

        if (!armorFillCached)
        {
            if (armorSlider.fillRect != null &&
                armorSlider.fillRect.TryGetComponent(out Image fillImage))
            {
                armorFill = fillImage;
                armorBaseColor = fillImage.color;
            }

            armorFillCached = true;
        }
    }

    private void PunchArmor(float amount, float duration)
    {
        if (!armorBaseScaleCached) return;

        armorSlider.transform.DOKill();
        armorSlider.transform.localScale = armorBaseScale;
        armorSlider.transform
            .DOPunchScale(armorBaseScale * amount, duration, 9, 0.6f);
    }
}
