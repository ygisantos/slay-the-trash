using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;

public class CardCollectionSceneScript : MonoBehaviour
{
    public GameObject noCards;
    public Vector2 cardDistance;
    public float bottomPad = 20f;
    public int yOffset = 334;
    public RectTransform canvasParent;
    public List<string> cardNames;
    public List<GameObject> actualCards;

    [Header("Card Source")]
    [Tooltip("Single card UI used for every card; filled from CardData.")]
    public GameObject cardTemplate;
    [Tooltip("Maps card names to their CardData assets.")]
    public CardLibrary library;

    [Header("Card Frames")]
    public Sprite paperFrame;
    public Sprite plasticFrame;
    public Sprite bioFrame;
    public Sprite otherFrame;
    public Sprite metalFrame;
    public Sprite ewasteFrame;

    [Header("Rarity Badges")]
    public Sprite bronzeBadge;
    public Sprite silverBadge;
    public Sprite goldBadge;
    public Vector2 badgeSize = new Vector2(80f, 80f);
    [Tooltip("Offset from the card's center.")]
    public Vector2 badgeOffset = new Vector2(110f, -110f);

    [Header("Disable Card")]
    [Tooltip("Overlay icon drawn on top of a card (Assets/User Interface/disable.png).")]
    public Sprite disableIcon;
    [Tooltip("Size of the overlay icon relative to the card width.")]
    public float disableIconScale = 0.62f;
    [Tooltip("First click: the card is armed to be disabled.")]
    public Color armDisableColor = new Color(1f, 0.78f, 0.2f, 0.95f);
    [Tooltip("Card is disabled and will not appear in battle.")]
    public Color disabledColor = new Color(0.90f, 0.25f, 0.25f, 0.95f);
    [Tooltip("First click on a disabled card: armed to be re-enabled.")]
    public Color armEnableColor = new Color(0.35f, 0.90f, 0.45f, 0.95f);
    [Tooltip("How long the armed state waits for the confirming second click.")]
    public float confirmWindow = 3f;

    private List<string> cards;
    private Coroutine buildRoutine;
    private readonly List<GameObject> headers = new List<GameObject>();

    private string username;
    private readonly HashSet<string> disabledCards = new HashSet<string>();
    private readonly Dictionary<string, Image> disableOverlays = new Dictionary<string, Image>();
    private readonly Dictionary<string, RectTransform> overlayRects = new Dictionary<string, RectTransform>();
    private readonly Dictionary<string, RectTransform> cardRects = new Dictionary<string, RectTransform>();
    private readonly Dictionary<string, Vector3> cardBaseScales = new Dictionary<string, Vector3>();
    private readonly Dictionary<string, Quaternion> cardBaseRotations = new Dictionary<string, Quaternion>();
    private readonly Dictionary<string, string> displayNames = new Dictionary<string, string>();

    private string armedCardKey;
    private float armedAt;

    [Button("Refresh UI")]
    private void RefreshUI()
    {
        if (!Application.isPlaying || cards == null)
        {
            Debug.LogWarning("Refresh UI only works in play mode after the collection has loaded.");
            return;
        }

        if (buildRoutine != null)
            StopCoroutine(buildRoutine);

        foreach (GameObject card in actualCards)
            if (card != null) Destroy(card);
        foreach (GameObject header in headers)
            if (header != null) Destroy(header);

        actualCards.Clear();
        headers.Clear();
        cardNames.Clear();
        disableOverlays.Clear();
        overlayRects.Clear();
        cardRects.Clear();
        cardBaseScales.Clear();
        cardBaseRotations.Clear();
        displayNames.Clear();
        armedCardKey = null;
        if (noCards != null) noCards.SetActive(false);

        buildRoutine = StartCoroutine(ConvertToPrefabs());
    }

    void Start()
    {
        username = GetCurrentUsername();
        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogWarning("Cannot load card collection without a logged-in username.");
            return;
        }

        // Load the disabled set first, so every card is built with the right overlay.
        FBCardCollection.Instance.GetDisabledCards(
            username,
            names =>
            {
                SetDisabledCards(names);
                LoadCards();
            },
            error =>
            {
                Debug.LogError($"Failed to load disabled cards: {error}");
                LoadCards();
            }
        );

        _ = DynamicPopupToast.Instance;
    }

    private void LoadCards()
    {
        FBCardCollection.Instance.GetCards(
            username,
            entries =>
            {
                // Keep the "trashType:cardName:rarity" shape ConvertToPrefabs already expects.
                cards = entries
                    .Select(entry => $"{entry.trashType}:{entry.cardName}:{entry.rarity}")
                    .ToList();

                buildRoutine = StartCoroutine(ConvertToPrefabs());
            },
            error => Debug.LogError($"Failed to load card collection: {error}")
        );
    }

    private string GetCurrentUsername()
    {
        Dictionary<string, object> profile =
            FBAuthentication.Instance != null
                ? FBAuthentication.Instance.CurrentProfile
                : null;

        if (profile == null && DataManager.Instance != null)
            profile = DataManager.Instance.GetProfile();

        return FirebaseDataHelper.GetString(profile, "username");
    }

    // Card names must match CardLibrary entries; groups are shown in this order, each under its own header.
    private static readonly (string category, string typeLabel, string[] names)[] Categories =
    {
        ("Paper", "Paper", new[] { "Crumpled", "Paper Shuriken", "Mache Plateguard", "Cardboard Spire", "Box Slam", "Origami Shredfall", "Shredstorm Hurricane" }),
        ("Plastic", "Plastic", new[] { "Bottle Cap Barrage", "Eco-Edge Sword", "Bottle Blaster", "Bottle Barrage", "Recycled Polywall", "Plastic Wave" }),
        ("Food Waste", "Bio", new[] { "Banana Splitter", "Popping Wrapper", "Croissant", "The Bonkstick" }),
        ("E-Waste", "E-Waste", new[] { "Recharge", "Static Surge", "Overclock" }),
        ("Metal", "Metal", new[] { "Tin Carapace", "Scrap Cleaver", "Corrosive Edge", "Steel Katana" }),
        ("Other", "Utility", new[] { "Scavenge" }),
    };

    private const int Columns = 5;
    private const float HeaderRows = 0.6f;
    private const float LockedBrightness = 0.35f;

    private IEnumerator ConvertToPrefabs()
    {
        if (cardTemplate == null || library == null)
        {
            Debug.LogWarning("Card Template or Card Library is missing! Cards will not be shown.");
            noCards.SetActive(true);
            yield break;
        }

        HashSet<string> owned = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> bestRarity = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
        foreach (string card in cards)
        {
            string[] c = card.Split(':');
            string rawName = c[1];
            owned.Add(rawName);
            string normName = rawName.Trim().Replace(" ", "").Replace("_", "").ToLowerInvariant();
            owned.Add(normName);
            cardNames.Add(rawName);

            int.TryParse(c.Length > 2 ? c[2] : "0", out int rarity);
            if (!bestRarity.TryGetValue(rawName, out int best) || rarity > best)
                bestRarity[rawName] = rarity;
            if (!bestRarity.TryGetValue(normName, out int bestNorm) || rarity > bestNorm)
                bestRarity[normName] = rarity;
        }

        float totalRows = 0f;
        foreach (var group in Categories)
            totalRows += HeaderRows + Mathf.Ceil(group.names.Length / (float)Columns);

        float startingY = cardDistance.y * totalRows / 2f - yOffset;
        canvasParent.sizeDelta = new Vector2(canvasParent.sizeDelta.x, cardDistance.y * totalRows + bottomPad);

        yield return new WaitForSeconds(.2f);

        float cursor = 0f;
        List<(RectTransform card, int rarity)> pendingBadges = new List<(RectTransform, int)>();
        foreach (var group in Categories)
        {
            CreateHeader(group.category, startingY - cursor * cardDistance.y);
            cursor += HeaderRows;

            for (int i = 0; i < group.names.Length; i++)
            {
                CardData data = FindCardData(group.names[i]);
                if (data == null)
                {
                    Debug.LogWarning($"No CardData in the Card Library for '{group.names[i]}'.");
                    continue;
                }

                int x = (i % Columns) - 2;
                float row = cursor + (i / Columns);

                GameObject card = Instantiate(cardTemplate, canvasParent);
                actualCards.Add(card);
                card.GetComponent<RectTransform>().anchoredPosition =
                    new Vector2(x * cardDistance.x, startingY - row * cardDistance.y);
                Populate(card, group.names[i], data, group.typeLabel, FrameFor(group.category));

                string dispName = group.names[i];
                string normDispName = dispName.Trim().Replace(" ", "").Replace("_", "").ToLowerInvariant();
                string dataName = data != null ? data.name : "";
                string normDataName = dataName.Trim().Replace(" ", "").Replace("_", "").ToLowerInvariant();

                bool isOwned = owned.Contains(dispName) || owned.Contains(normDispName) ||
                               (!string.IsNullOrEmpty(dataName) && (owned.Contains(dataName) || owned.Contains(normDataName)));

                if (!isOwned)
                {
                    GreyOut(card);
                }
                else
                {
                    int rarity = 0;
                    if (!bestRarity.TryGetValue(dispName, out rarity))
                        if (!bestRarity.TryGetValue(normDispName, out rarity))
                            if (!string.IsNullOrEmpty(dataName) && !bestRarity.TryGetValue(dataName, out rarity))
                                bestRarity.TryGetValue(normDataName, out rarity);

                    pendingBadges.Add((card.GetComponent<RectTransform>(), rarity));

                    // Only cards the player owns can be switched off.
                    SetupDisableOverlay(card, dispName);
                }

                yield return new WaitForSeconds(.05f);
            }

            cursor += Mathf.Ceil(group.names.Length / (float)Columns);
        }

        // Cards overlap, so badges are created last to stay on top.
        foreach (var (cardRect, rarity) in pendingBadges)
            AddBadge(cardRect, rarity);
    }

    private CardData FindCardData(string cardName)
    {
        if (library == null || library.cards == null) return null;

        string cleanTarget = cardName?.Trim().Replace(" ", "").Replace("_", "").ToLowerInvariant();

        foreach (NamedCard entry in library.cards)
        {
            if (string.Equals(entry.Name, cardName, StringComparison.OrdinalIgnoreCase))
                return entry.Data;

            if (entry.Data != null && string.Equals(entry.Data.name, cardName, StringComparison.OrdinalIgnoreCase))
                return entry.Data;

            string cleanEntry = entry.Name?.Trim().Replace(" ", "").Replace("_", "").ToLowerInvariant();
            if (cleanEntry == cleanTarget)
                return entry.Data;

            if (entry.Data != null)
            {
                string cleanDataName = entry.Data.name?.Trim().Replace(" ", "").Replace("_", "").ToLowerInvariant();
                if (cleanDataName == cleanTarget)
                    return entry.Data;
            }
        }

        return null;
    }

    private Sprite FrameFor(string category)
    {
        switch (category)
        {
            case "Paper": return paperFrame;
            case "Plastic": return plasticFrame;
            case "Food Waste": return bioFrame;
            case "Metal": return metalFrame != null ? metalFrame : otherFrame;
            case "E-Waste": return ewasteFrame != null ? ewasteFrame : otherFrame;
            default: return otherFrame;
        }
    }

    // Child names follow the template hierarchy: Image/{Title, Description, Mana, Cardtype, CardImage}.
    //
    // Every CardData.Image is a COMPLETE card face: it already contains the frame,
    // the illustration and the category label, and leaves the title banner, mana box
    // and description panel empty. So the card face is data.Image itself and only
    // the text has to be overlaid. The category frame is kept purely as a fallback
    // for a card that has no image yet.
    private void Populate(GameObject card, string displayName, CardData data, string typeLabel, Sprite frame)
    {
        Transform frameTransform = card.transform.Find("Image");
        if (frameTransform == null)
        {
            Debug.LogWarning("Card template has no 'Image' child; cannot populate it.");
            return;
        }

        bool hasCardFace = data != null && data.Image != null;

        if (frameTransform.TryGetComponent(out Image frameImage))
        {
            frameImage.sprite = hasCardFace ? data.Image : frame;
            frameImage.color = Color.white;

            // Fill the existing card rect without preserving aspect. The baked
            // banner / mana box / panel positions then scale together with the
            // overlaid text, so everything stays aligned.
            frameImage.preserveAspect = false;
        }

        SetText(frameTransform, "Title", displayName);
        SetText(frameTransform, "Description", data.Description);
        SetText(frameTransform, "Mana", data.Mana.ToString());

        // The category label is painted into the card face, so the separate label
        // widget and the separate art slot are both redundant now.
        Transform typeText = frameTransform.Find("Cardtype");
        if (typeText != null)
            typeText.gameObject.SetActive(false);

        Transform art = frameTransform.Find("CardImage");
        if (art != null)
            art.gameObject.SetActive(false);
    }

    private static void SetText(Transform parent, string childName, string value)
    {
        Transform child = parent.Find(childName);
        if (child != null && child.TryGetComponent(out TMPro.TMP_Text text))
            text.text = value;
    }

    private void CreateHeader(string title, float y)
    {
        GameObject header = new GameObject(title + " Header", typeof(RectTransform));
        headers.Add(header);
        header.transform.SetParent(canvasParent, false);

        RectTransform rect = header.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(cardDistance.x * Columns, cardDistance.y * HeaderRows);
        rect.anchoredPosition = new Vector2(0f, y);

        TMPro.TextMeshProUGUI text = header.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = title;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.fontSize = 48;
        text.raycastTarget = false;
    }

    private void AddBadge(RectTransform card, int rarity)
    {
        Sprite sprite = rarity switch
        {
            0 => bronzeBadge,
            1 => silverBadge,
            2 => goldBadge,
            _ => null
        };

        if (sprite == null)
        {
            Debug.LogWarning($"No badge sprite assigned for rarity {rarity}.");
            return;
        }

        GameObject badge = new GameObject("Rarity Badge", typeof(RectTransform), typeof(Image));
        headers.Add(badge);
        badge.transform.SetParent(canvasParent, false);

        RectTransform rect = badge.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = badgeSize;
        rect.anchoredPosition = card.anchoredPosition + badgeOffset;

        Image image = badge.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private void GreyOut(GameObject card)
    {
        foreach (Graphic graphic in card.GetComponentsInChildren<Graphic>(true))
        {
            Color c = graphic.color;
            graphic.color = new Color(c.r * LockedBrightness, c.g * LockedBrightness, c.b * LockedBrightness, c.a);
        }
    }

    // ================================================================
    // DISABLE / ENABLE A CARD
    //
    // Double click toggles a card. The first click arms the card and shows the
    // overlay tinted with the pending action; the confirming click applies it.
    // Disabled cards keep the overlay visible and are left out of the battle deck.
    // ================================================================

    private void SetDisabledCards(IEnumerable<string> names)
    {
        disabledCards.Clear();

        if (names == null)
            return;

        foreach (string name in names)
        {
            string key = DeckManager.NormalizeCardName(name);

            if (!string.IsNullOrEmpty(key))
                disabledCards.Add(key);
        }
    }

    private void SetupDisableOverlay(GameObject card, string cardName)
    {
        if (disableIcon == null)
        {
            Debug.LogWarning("Disable icon is not assigned; card disabling will not be shown.");
            return;
        }

        Transform frame = card.transform.Find("Image");
        if (frame == null)
            return;

        string key = DeckManager.NormalizeCardName(cardName);
        if (string.IsNullOrEmpty(key))
            return;

        displayNames[key] = cardName;

        // The card face has to receive pointer clicks.
        if (frame.TryGetComponent(out Image frameImage))
            frameImage.raycastTarget = true;

        GameObject overlayObject = new GameObject(
            "Disable Overlay",
            typeof(RectTransform),
            typeof(Image)
        );

        overlayObject.transform.SetParent(frame, false);

        RectTransform frameRect = frame as RectTransform;
        float cardWidth = frameRect != null && frameRect.rect.width > 1f
            ? frameRect.rect.width
            : 247.9f;

        float side = Mathf.Max(16f, cardWidth * Mathf.Clamp01(disableIconScale));

        RectTransform rect = overlayObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(side, side);
        rect.localRotation = Quaternion.identity;

        Image overlay = overlayObject.GetComponent<Image>();
        overlay.sprite = disableIcon;
        overlay.preserveAspect = true;
        overlay.raycastTarget = false;
        overlay.enabled = false;

        disableOverlays[key] = overlay;
        overlayRects[key] = rect;

        if (card.transform is RectTransform cardRect)
        {
            cardRects[key] = cardRect;
            cardBaseScales[key] = cardRect.localScale;
            cardBaseRotations[key] = cardRect.localRotation;
        }

        CardClickRelay relay = frame.gameObject.GetComponent<CardClickRelay>();
        if (relay == null)
            relay = frame.gameObject.AddComponent<CardClickRelay>();

        relay.Setup(this, key);

        ApplyOverlay(key, instant: true);
    }

    private void ApplyOverlay(string key, bool instant = false)
    {
        if (string.IsNullOrEmpty(key) ||
            !disableOverlays.TryGetValue(key, out Image overlay) ||
            overlay == null)
        {
            return;
        }

        bool disabled = disabledCards.Contains(key);
        bool armed = key == armedCardKey;

        bool shouldShow = disabled || armed;
        Color targetColor = disabled
            ? (armed ? armEnableColor : disabledColor)
            : armDisableColor;

        RectTransform rect = overlayRects.TryGetValue(key, out RectTransform stored)
            ? stored
            : overlay.rectTransform;

        rect.DOKill();
        overlay.DOKill();

        if (instant)
        {
            overlay.enabled = shouldShow;
            overlay.color = targetColor;
            rect.localScale = Vector3.one;
            return;
        }

        if (shouldShow)
        {
            bool wasVisible = overlay.enabled;

            overlay.enabled = true;

            if (!wasVisible)
            {
                // Pop in: grow from small while fading up.
                overlay.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);
                rect.localScale = Vector3.one * 0.55f;

                rect.DOScale(Vector3.one, 0.24f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);

                overlay.DOColor(targetColor, 0.16f).SetUpdate(true);
            }
            else
            {
                // Already showing: just cross-fade to the new state colour.
                overlay.DOColor(targetColor, 0.2f).SetUpdate(true);
            }

            return;
        }

        if (!overlay.enabled)
            return;

        // Shrink and fade away.
        Color fade = new Color(overlay.color.r, overlay.color.g, overlay.color.b, 0f);

        overlay.DOColor(fade, 0.14f).SetUpdate(true);

        rect.DOScale(Vector3.one * 0.5f, 0.14f)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (overlay != null)
                    overlay.enabled = false;
            });
    }

    // Small impact so toggling a card feels physical.
    private void PunchCard(string key, bool disabling)
    {
        if (!cardRects.TryGetValue(key, out RectTransform card) || card == null)
            return;

        Vector3 baseScale = cardBaseScales.TryGetValue(key, out Vector3 storedScale)
            ? storedScale
            : Vector3.one;

        Quaternion baseRotation = cardBaseRotations.TryGetValue(key, out Quaternion storedRot)
            ? storedRot
            : Quaternion.identity;

        card.DOKill();

        // Reset first, so killing a punch mid-flight cannot leave the card offset.
        card.localScale = baseScale;
        card.localRotation = baseRotation;

        card.DOPunchScale(baseScale * 0.06f, 0.32f, 9, 0.6f).SetUpdate(true);

        if (disabling)
        {
            card.DOPunchRotation(new Vector3(0f, 0f, 3f), 0.32f, 8, 0.5f)
                .SetUpdate(true);
        }
    }

    // Called by CardClickRelay when a card is clicked.
    public void HandleCardClicked(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        bool confirming = key == armedCardKey &&
                          Time.unscaledTime - armedAt <= confirmWindow;

        if (confirming)
        {
            armedCardKey = null;
            ToggleDisabled(key);
            return;
        }

        string previouslyArmed = armedCardKey;
        armedCardKey = key;
        armedAt = Time.unscaledTime;

        if (!string.IsNullOrEmpty(previouslyArmed) && previouslyArmed != key)
            ApplyOverlay(previouslyArmed);

        ApplyOverlay(key);
    }

    private void ToggleDisabled(string key)
    {
        if (string.IsNullOrWhiteSpace(username))
            return;

        string cardName = displayNames.TryGetValue(key, out string named) ? named : key;
        bool makeDisabled = !disabledCards.Contains(key);

        // Apply locally first so the icon reacts instantly.
        SetLocalDisabled(key, makeDisabled);
        ApplyOverlay(key);
        PunchCard(key, makeDisabled);

        FBCardCollection.Instance.SetCardDisabled(
            username,
            cardName,
            makeDisabled,
            () =>
            {
                DynamicPopupToast.Instance?.ShowToast(
                    makeDisabled
                        ? $"{cardName} disabled. It will not appear in battle."
                        : $"{cardName} enabled again.",
                    3
                );

                // Rebuild the battle pool with the new set.
                if (DeckManager.instance != null)
                    DeckManager.instance.UpdateCardsList();
            },
            error =>
            {
                // Put the previous state back if the write failed.
                SetLocalDisabled(key, !makeDisabled);
                ApplyOverlay(key);

                Debug.LogError($"Failed to update disabled card: {error}");
                DynamicPopupToast.Instance?.ShowToast(
                    $"Could not update {cardName}. Please try again.",
                    3
                );
            }
        );
    }

    private void SetLocalDisabled(string key, bool disabled)
    {
        if (disabled)
            disabledCards.Add(key);
        else
            disabledCards.Remove(key);
    }
}

// Forwards UI clicks on a card to the collection screen.
public class CardClickRelay : MonoBehaviour, IPointerClickHandler
{
    private CardCollectionSceneScript owner;
    private string cardKey;

    public void Setup(CardCollectionSceneScript scene, string key)
    {
        owner = scene;
        cardKey = key;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (owner != null)
            owner.HandleCardClicked(cardKey);
    }
}
