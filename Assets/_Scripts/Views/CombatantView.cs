using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatantView : MonoBehaviour
{
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Slider healthSlider;

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private StatusEffectsUI statusEffectsUI;
    

    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }

    private Dictionary<StatusEffectType, int> statusEffects = new();
    private SoundType hurtSound;

    protected void SetupBase(int health, Sprite image, SoundType sound = SoundType.SMALLENEMYHURT)
    {
        MaxHealth = CurrentHealth = health;
        spriteRenderer.sprite = image;
        hurtSound = sound;
        UpdateHealthText();
    }

    private void UpdateHealthText()
    {
        healthText.text = "HP: " + CurrentHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = MaxHealth;
            healthSlider.value = CurrentHealth;
        }
    }

    public void Damage(int damageAmount)
    {
        int remainingDamage = damageAmount;
        int currentArmor = GetStatusEffectStacks(StatusEffectType.ARMOR);
        SoundManager.PlaySound(hurtSound);
        if (transform != null)
        {
            transform.DOShakePosition(0.2f, 0.5f);
        }

        if (currentArmor > 0)
        {
            if (currentArmor >= damageAmount)
            {
                SoundManager.PlaySound(SoundType.ARMORHIT);
                RemoveStatusEffect(StatusEffectType.ARMOR, remainingDamage);
                remainingDamage = 0;
            }
            else
            {
                RemoveStatusEffect(StatusEffectType.ARMOR, currentArmor);
                remainingDamage -= currentArmor;
            }
        }

        if (remainingDamage > 0)
        {
            CurrentHealth -= remainingDamage;
            if (CurrentHealth < 0)
                CurrentHealth = 0;
        }
        UpdateHealthText();
    }
    public void AddHealth(int health)
    {
        CurrentHealth += health;
        MaxHealth += health;
    }

    public void AddStatusEffect(StatusEffectType type, int stackCount)
    {
        if (statusEffects.ContainsKey(type))
            statusEffects[type] += stackCount;
        else
            statusEffects.Add(type, stackCount);

        if (CurrentHealth <= 0) return;

        statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
    }

    public void RemoveStatusEffect(StatusEffectType type, int stackCount)
    {
        if (statusEffects.ContainsKey(type))
        {
            statusEffects[type] -= stackCount;
            if (statusEffects[type] <= 0)
                statusEffects.Remove(type);
        }

        if (CurrentHealth <= 0) return;

        statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
    }

    public int GetStatusEffectStacks(StatusEffectType type)
    {
        return statusEffects.ContainsKey(type) ? statusEffects[type] : 0;
    }
}
