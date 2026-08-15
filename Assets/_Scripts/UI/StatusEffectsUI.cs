using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class StatusEffectsUI : MonoBehaviour
{
    [SerializeField] private StatusEffectUI statusEffectUIPrefab;
    [SerializeField] private Sprite armorSprite, burnSprite;

    private Dictionary<StatusEffectType, StatusEffectUI> statusEffectUIs = new();

    public Slider armorSlider;
    public TMP_Text armorText;
    public int maxShield = 50;

    private void Start()
    {
        UpdateArmorSlider(0);
    }

    public void UpdateStatusEffectUI(StatusEffectType statusEffectType, int stackCount)
    {
        int cappedValue = Mathf.Clamp(stackCount, 0, GetCappedByType(statusEffectType));

        if (statusEffectType == StatusEffectType.ARMOR)
        {
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

        armorSlider.maxValue = maxShield;
        armorSlider.value = Mathf.Clamp(currentShield, 0, maxShield);

        if (armorText != null)
        {
            armorText.text = $"{armorSlider.value} / {maxShield}";
        }
    }
}
