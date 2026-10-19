using DG.Tweening;
using System.Collections;
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

    private readonly List<Tween> activeFeedbackTweens = new();

    private void TrackFeedbackTween(Tween tween)
    {
        if (tween != null && tween.IsActive())
            activeFeedbackTweens.Add(tween);
    }

    public IEnumerator WaitForDamageFeedback()
    {
        // Remove completed or inactive tweens.
        activeFeedbackTweens.RemoveAll(
            tween => tween == null || !tween.IsActive()
        );

        // Wait for every feedback animation currently being tracked.
        Tween[] pending = activeFeedbackTweens.ToArray();

        foreach (Tween tween in pending)
        {
            if (tween != null && tween.IsActive())
                yield return tween.WaitForCompletion();
        }

        activeFeedbackTweens.RemoveAll(
            tween => tween == null || !tween.IsActive()
        );
    }

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

    // Step slightly toward the target, then ease back. Impact should happen at the peak.
    
    public IEnumerator PlayAttackLunge(Vector3 towardWorld)
    {
        if (CurrentHealth <= 0)
            yield break;

        Vector3 rest = transform.position;

        float dir = Mathf.Sign(towardWorld.x - rest.x);

        if (Mathf.Abs(towardWorld.x - rest.x) < 0.05f)
            dir = this is HeroView ? 1f : -1f;

        const float distance = 0.55f;
        const float outTime = 0.7f;
        const float backTime = 0.75f;

        transform.DOKill();

        Tween lunge = transform
            .DOMoveX(rest.x + dir * distance, outTime)
            .SetEase(Ease.OutCubic);

        yield return lunge.WaitForCompletion();

        if (this == null)
            yield break;

        Tween returnTween = transform
            .DOMove(rest, backTime)
            .SetEase(Ease.InOutCubic);

        yield return returnTween.WaitForCompletion();
    }

    public void Damage(int damageAmount, bool isBurn = false)
    {
        int remainingDamage = damageAmount;
        int currentArmor = GetStatusEffectStacks(StatusEffectType.ARMOR);
        int absorbed = 0;

        if (isBurn)
        {
            // Pokémon-style screen burn overlay: flash screen with fiery heat wave
            ScreenBurnOverlay.Show();
            SoundManager.Instance?.PlaySound(hurtSound, 0.85f);
        }
        else
        {
            Shake.Instance?.ShakeCamera();
            SoundManager.Instance?.PlaySound(hurtSound);

            // Zoom toward the combatant that is taking damage (enemy or player).
            Shake.Instance?.ZoomTo(transform.position);
        }

        if (transform != null)
        {
            if (isBurn)
            {
                // Rapid status shudder / trembling reaction (Pokémon style)
                TrackFeedbackTween(
                    transform.DOShakePosition(0.35f, new Vector3(0.14f, 0.03f, 0f), 24, 0f)
                );
            }
            else
            {
                TrackFeedbackTween(
                    transform.DOShakePosition(0.2f, 0.5f)
                );
            }
        }

        if (currentArmor > 0)
        {
            if (currentArmor >= damageAmount)
            {
                SoundManager.Instance?.PlaySound(SoundType.ARMORHIT);
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

            if (isBurn)
            {
                // Play Pokémon-style burn sprite and rising ember particles on combatant
                PlayBurnEffect();
            }
            else
            {
                HitRing(hitColor);

                // A dying combatant is scaled away by its own removal animation.
                if (CurrentHealth > 0)
                    Squish();
            }

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

        squish.Append(
            t.DOScale(
                new Vector3(b.x * 1.3f, b.y * 0.7f, b.z),
                0.07f
            ).SetEase(Ease.OutQuad)
        );

        squish.Append(
            t.DOScale(b, 0.4f).SetEase(Ease.OutElastic)
        );

        TrackFeedbackTween(squish);
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

        bump.Append(
            t.DOScale(b * 1.12f, 0.08f).SetEase(Ease.OutQuad)
        );

        bump.Append(
            t.DOScale(b, 0.2f).SetEase(Ease.OutBack)
        );

        TrackFeedbackTween(bump);
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

        Sequence ring = DOTween.Sequence();

        ring.Join(
            go.transform.DOScale(2.2f, 0.4f)
                .SetEase(Ease.OutQuad)
        );

        ring.Join(
            DOTween.To(
                () => sr.color,
                c => sr.color = c,
                new Color(color.r, color.g, color.b, 0f),
                0.4f
            )
        );

        ring.OnComplete(() =>
        {
            if (go != null)
                Destroy(go);
        });

        TrackFeedbackTween(ring);
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

    // Procedurally generated flame sprite for status burn animations
    private static Sprite flameSprite;
    private static Sprite GetFlameSprite()
    {
        if (flameSprite != null)
            return flameSprite;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        float centerX = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        {
            float ny = (float)y / size;
            float maxWidth;
            if (ny < 0.2f)
                maxWidth = Mathf.Lerp(0.18f, 0.42f, ny / 0.2f);
            else if (ny < 0.55f)
                maxWidth = Mathf.Lerp(0.42f, 0.35f, (ny - 0.2f) / 0.35f);
            else
                maxWidth = Mathf.Lerp(0.35f, 0.0f, (ny - 0.55f) / 0.45f);

            for (int x = 0; x < size; x++)
            {
                float nx = Mathf.Abs((x - centerX) / (size / 2f));
                if (nx < maxWidth && ny < 0.95f)
                {
                    float edgeDist = nx / maxWidth;
                    float alpha = Mathf.Clamp01(1f - edgeDist * edgeDist);
                    alpha *= Mathf.Clamp01((ny - 0.02f) / 0.12f);
                    alpha *= Mathf.Clamp01((0.95f - ny) / 0.15f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        flameSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        return flameSprite;
    }

    // Pokémon-style burn damage visual: status icon pops up and rises with ember sparks
    private void PlayBurnEffect()
    {
        Vector3 center = spriteRenderer != null ? spriteRenderer.bounds.center : transform.position;
        int baseSortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder : 10;

        // 1. Central Burn / Status Sprite Popup
        Sprite spriteToUse = (statusEffectsUI != null && statusEffectsUI.BurnSprite != null)
            ? statusEffectsUI.BurnSprite
            : GetFlameSprite();

        GameObject iconObj = new GameObject("BurnStatusIconFX");
        iconObj.transform.position = center;
        iconObj.transform.localScale = Vector3.one * 0.2f;

        SpriteRenderer iconSr = iconObj.AddComponent<SpriteRenderer>();
        iconSr.sprite = spriteToUse;
        iconSr.sortingOrder = baseSortingOrder + 25;
        iconSr.color = new Color(1f, 0.55f, 0.15f, 1f);

        Sequence iconSeq = DOTween.Sequence();
        iconSeq.Join(
            iconObj.transform.DOScale(1.4f, 0.12f).SetEase(Ease.OutBack)
        );
        iconSeq.Join(
            iconObj.transform.DOMoveY(center.y + 0.85f, 0.48f).SetEase(Ease.OutQuad)
        );
        iconSeq.Insert(
            0.15f,
            DOTween.To(() => iconSr.color, c => iconSr.color = c, new Color(1f, 0.25f, 0.05f, 0f), 0.33f)
                .SetEase(Ease.InQuad)
        );
        iconSeq.OnComplete(() =>
        {
            if (iconObj != null)
                Destroy(iconObj);
        });
        TrackFeedbackTween(iconSeq);

        // 2. Fiery Embers dancing and rising around the combatant
        for (int i = 0; i < 4; i++)
        {
            GameObject emberObj = new GameObject($"BurnEmberFX_{i}");
            float offsetX = Random.Range(-0.45f, 0.45f);
            float offsetY = Random.Range(-0.35f, 0.25f);
            emberObj.transform.position = center + new Vector3(offsetX, offsetY, 0f);
            emberObj.transform.localScale = Vector3.one * 0.1f;

            SpriteRenderer emberSr = emberObj.AddComponent<SpriteRenderer>();
            emberSr.sprite = GetFlameSprite();
            emberSr.sortingOrder = baseSortingOrder + 24;
            emberSr.color = (i % 2 == 0)
                ? new Color(1f, 0.75f, 0.15f, 0.9f)
                : new Color(1f, 0.35f, 0.05f, 0.9f);

            float floatHeight = Random.Range(0.6f, 1.1f);
            float driftX = Random.Range(-0.25f, 0.25f);
            float delay = i * 0.04f;

            Sequence emberSeq = DOTween.Sequence();
            if (delay > 0f)
                emberSeq.AppendInterval(delay);

            emberSeq.Join(
                emberObj.transform.DOScale(Random.Range(0.4f, 0.65f), 0.12f).SetEase(Ease.OutBack)
            );
            emberSeq.Join(
                emberObj.transform.DOMove(
                    emberObj.transform.position + new Vector3(driftX, floatHeight, 0f),
                    0.42f
                ).SetEase(Ease.OutQuad)
            );
            emberSeq.Insert(
                delay + 0.15f,
                DOTween.To(() => emberSr.color, c => emberSr.color = c, new Color(emberSr.color.r, emberSr.color.g, emberSr.color.b, 0f), 0.27f)
                    .SetEase(Ease.InQuad)
            );
            emberSeq.OnComplete(() =>
            {
                if (emberObj != null)
                    Destroy(emberObj);
            });
            TrackFeedbackTween(emberSeq);
        }
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

        TrackFeedbackTween(flashTween);
    }

    // World-space number that rises above the combatant and fades out.
    private void ShowFloatingText(
        string message,
        Color color,
        float yOffset = 0f)
    {
        GameObject go = new GameObject("FloatingText");

        go.transform.position =
            transform.position + new Vector3(0f, 1.6f + yOffset, 0f);

        TextMeshPro text = go.AddComponent<TextMeshPro>();
        text.text = message;
        text.fontSize = 8f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.sortingOrder = 200;
        text.rectTransform.sizeDelta = new Vector2(8f, 2f);

        go.transform.localScale = Vector3.one * 0.6f;

        Sequence floatingText = DOTween.Sequence();

        floatingText.Join(
            go.transform.DOScale(1f, 0.25f)
                .SetEase(Ease.OutBack)
        );

        floatingText.Join(
            go.transform.DOMoveY(
                go.transform.position.y + 1.2f,
                1f
            ).SetEase(Ease.OutCubic)
        );

        floatingText.Insert(
            0.6f,
            DOTween.To(
                () => text.alpha,
                a => text.alpha = a,
                0f,
                0.4f
            )
        );

        floatingText.OnComplete(() =>
        {
            if (go != null)
                Destroy(go);
        });

        TrackFeedbackTween(floatingText);
    }
}
