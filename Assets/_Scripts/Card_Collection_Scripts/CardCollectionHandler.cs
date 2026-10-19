using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
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

    private List<string> cards;
    private Coroutine buildRoutine;
    private readonly List<GameObject> headers = new List<GameObject>();

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
        if (noCards != null) noCards.SetActive(false);

        buildRoutine = StartCoroutine(ConvertToPrefabs());
    }

    void Start()
    {
        string username = GetCurrentUsername();
        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogWarning("Cannot load card collection without a logged-in username.");
            return;
        }

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
}
