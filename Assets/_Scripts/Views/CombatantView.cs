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
        Shake.Instance.ShakeCamera();
        SoundManager.Instance.PlaySound(hurtSound);
        if (transform != null)
        {
            transform.DOShakePosition(0.2f, 0.5f);
        }

        if (currentArmor > 0)
        {
            if (currentArmor >= damageAmount)
            {
                SoundManager.Instance.PlaySound(SoundType.ARMORHIT);
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

        if (remainingDamage > 0)
            ShowFloatingText("-" + remainingDamage, new Color(1f, 0.35f, 0.35f));
        else
            ShowFloatingText("Blocked", new Color(0.45f, 0.85f, 1f));
    }
    public void AddHealth(int health)
    {
        CurrentHealth += health;
        MaxHealth += health;
        UpdateHealthText();
    }

    // Returns the amount actually restored.
    public int Heal(int amount, bool playSound = true)
    {
        if (amount <= 0 || CurrentHealth <= 0)
            return 0;

        int before = CurrentHealth;
        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
        UpdateHealthText();

        int healed = CurrentHealth - before;
        if (healed > 0)
        {
            ShowFloatingText("+" + healed, new Color(0.45f, 1f, 0.5f));
            Flash(new Color(0.55f, 1f, 0.6f));

            if (playSound)
                SoundManager.Instance?.PlaySound(SoundType.HEALTH);
        }

        return healed;
    }

    public void AddStatusEffect(StatusEffectType type, int stackCount, bool feedback = true)
    {
        if (statusEffects.ContainsKey(type))
            statusEffects[type] += stackCount;
        else
            statusEffects.Add(type, stackCount);

        if (CurrentHealth <= 0) return;

        statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));

        if (feedback && type == StatusEffectType.ARMOR && stackCount > 0)
        {
            ShowFloatingText("+" + stackCount + " Shield", new Color(0.45f, 0.85f, 1f));
            Flash(new Color(0.5f, 0.85f, 1f));
            SoundManager.Instance?.PlaySound(SoundType.ARMORUP);
        }
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

    private Tween flashTween;
    private Color? spriteBaseColor;

    // Briefly tints the sprite, then returns to its normal color.
    private void Flash(Color color)
    {
        if (spriteRenderer == null)
            return;

        if (flashTween != null && flashTween.IsActive())
            flashTween.Kill(true);

        if (!spriteBaseColor.HasValue)
            spriteBaseColor = spriteRenderer.color;

        spriteRenderer.color = spriteBaseColor.Value;
        flashTween = DOTween.To(
            () => spriteRenderer.color,
            c => spriteRenderer.color = c,
            color,
            0.15f
        ).SetLoops(2, LoopType.Yoyo);
    }

    // World-space number that rises above the combatant and fades out.
    private void ShowFloatingText(string message, Color color)
    {
        GameObject go = new GameObject("FloatingText");
        go.transform.position = transform.position + new Vector3(0f, 1.6f, 0f);

        TextMeshPro text = go.AddComponent<TextMeshPro>();
        text.text = message;
        text.fontSize = 8f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.sortingOrder = 200;
        text.rectTransform.sizeDelta = new Vector2(8f, 2f);

        go.transform.localScale = Vector3.one * 0.6f;
        go.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
        go.transform.DOMoveY(go.transform.position.y + 1.2f, 1f).SetEase(Ease.OutCubic);
        DOTween.To(() => text.alpha, a => text.alpha = a, 0f, 0.4f)
            .SetDelay(0.6f)
            .OnComplete(() => { if (go != null) Destroy(go); });
    }
}
