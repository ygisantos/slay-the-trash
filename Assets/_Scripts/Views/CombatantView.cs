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

    public void Damage(int damageAmount, bool isBurn = false)
    {
        int remainingDamage = damageAmount;
        int currentArmor = GetStatusEffectStacks(StatusEffectType.ARMOR);
        int absorbed = 0;
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
                absorbed = remainingDamage;
                remainingDamage = 0;
            }
            else
            {
                RemoveStatusEffect(StatusEffectType.ARMOR, currentArmor);
                absorbed = currentArmor;
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
        {
            Color hitColor = isBurn ? BurnColor : DamageColor;

            ShowFloatingText(
                (isBurn ? "Burn -" : "-") + remainingDamage,
                hitColor
            );
            Flash(hitColor);
            HitRing(hitColor);

            // A dying combatant is scaled away by its own removal animation.
            if (CurrentHealth > 0)
                Squish();

            if (absorbed > 0)
                ShowFloatingText("Block -" + absorbed, BlockColor, -0.8f);
        }
        else
        {
            ShowFloatingText("Blocked", BlockColor);
            HitRing(BlockColor);
            Bump();
        }
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
            ShowFloatingText("+" + stackCount + " Shield", BlockColor);
            Flash(new Color(0.5f, 0.85f, 1f));
            HitRing(BlockColor);
            SoundManager.Instance?.PlaySound(SoundType.ARMORUP);
        }
        else if (feedback && type == StatusEffectType.BURN && stackCount > 0)
        {
            ShowFloatingText("Burn +" + stackCount, BurnColor);
            Flash(BurnColor);
            HitRing(BurnColor);
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

    private static readonly Color DamageColor = new Color(1f, 0.35f, 0.35f);
    private static readonly Color BlockColor = new Color(0.45f, 0.85f, 1f);
    private static readonly Color BurnColor = new Color(1f, 0.6f, 0.2f);

    private readonly object fxId = new object();
    private Vector3? spriteBaseScale;
    private static Sprite ringSprite;

    // Stretch-and-squash on the sprite when it takes damage.
    private void Squish()
    {
        if (spriteRenderer == null)
            return;

        Transform t = spriteRenderer.transform;

        DOTween.Kill(fxId, true);
        if (!spriteBaseScale.HasValue)
            spriteBaseScale = t.localScale;

        Vector3 b = spriteBaseScale.Value;
        t.localScale = b;

        Sequence squish = DOTween.Sequence().SetId(fxId);
        squish.Append(t.DOScale(new Vector3(b.x * 1.3f, b.y * 0.7f, b.z), 0.07f).SetEase(Ease.OutQuad));
        squish.Append(t.DOScale(b, 0.4f).SetEase(Ease.OutElastic));
    }

    // Small puff-up when an attack is fully blocked.
    private void Bump()
    {
        if (spriteRenderer == null)
            return;

        Transform t = spriteRenderer.transform;

        DOTween.Kill(fxId, true);
        if (!spriteBaseScale.HasValue)
            spriteBaseScale = t.localScale;

        Vector3 b = spriteBaseScale.Value;
        t.localScale = b;

        Sequence bump = DOTween.Sequence().SetId(fxId);
        bump.Append(t.DOScale(b * 1.12f, 0.08f).SetEase(Ease.OutQuad));
        bump.Append(t.DOScale(b, 0.2f).SetEase(Ease.OutBack));
    }

    // Expanding ring that shows the type of hit (damage, block, burn).
    private void HitRing(Color color)
    {
        GameObject go = new GameObject("HitRing");
        go.transform.position = spriteRenderer != null
            ? spriteRenderer.bounds.center
            : transform.position;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetRingSprite();
        sr.color = color;
        sr.sortingOrder = 150;

        go.transform.localScale = Vector3.one * 0.6f;
        go.transform.DOScale(2.2f, 0.4f).SetEase(Ease.OutQuad);
        DOTween.To(() => sr.color, c => sr.color = c, new Color(color.r, color.g, color.b, 0f), 0.4f)
            .OnComplete(() => { if (go != null) Destroy(go); });
    }

    private static Sprite GetRingSprite()
    {
        if (ringSprite != null)
            return ringSprite;

        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / (size / 2f);
                float alpha = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.8f) / 0.15f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
        }

        texture.Apply();
        ringSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        return ringSprite;
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
    private void ShowFloatingText(string message, Color color, float yOffset = 0f)
    {
        GameObject go = new GameObject("FloatingText");
        go.transform.position = transform.position + new Vector3(0f, 1.6f + yOffset, 0f);

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
