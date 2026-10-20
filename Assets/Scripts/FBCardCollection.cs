using System;
using System.Collections.Generic;
using Firebase.Firestore;
using UnityEngine;
using Sirenix.OdinInspector;

// Persists earned cards to Firestore per user, replacing the old local card_collection.txt file.
public class FBCardCollection : MonoBehaviour
{
    private static FBCardCollection instance;

    public static FBCardCollection Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<FBCardCollection>();

                if (instance == null)
                {
                    GameObject clone = new GameObject("FBCardCollection");
                    instance = clone.AddComponent<FBCardCollection>();
                }
            }

            return instance;
        }
    }

    public const string COLLECTION = "card_collections";
    public const string DISABLED_FIELD = "disabled_cards";

    // Names of the cards the player has switched off for battle.
    public void GetDisabledCards(
        string username,
        Action<List<string>> onSuccess,
        Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            onError?.Invoke("Username is required to load disabled cards.");
            return;
        }

        FirebaseManager.Instance.GetDocument(
            COLLECTION,
            username,
            snapshot =>
            {
                List<string> names = new List<string>();

                if (snapshot.Exists)
                {
                    Dictionary<string, object> data = snapshot.ToDictionary();

                    if (data.TryGetValue(DISABLED_FIELD, out object raw) &&
                        raw is List<object> stored)
                    {
                        foreach (object item in stored)
                        {
                            if (item is string name && !string.IsNullOrWhiteSpace(name))
                                names.Add(name);
                        }
                    }
                }

                onSuccess?.Invoke(names);
            },
            onError
        );
    }

    public void SetCardDisabled(
        string username,
        string cardName,
        bool disabled,
        Action onSuccess = null,
        Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(cardName))
        {
            onError?.Invoke("Username and card name are required to update a disabled card.");
            return;
        }

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            {
                DISABLED_FIELD,
                disabled
                    ? FieldValue.ArrayUnion(cardName)
                    : FieldValue.ArrayRemove(cardName)
            }
        };

        FirebaseManager.Instance.GetDocument(
            COLLECTION,
            username,
            snapshot =>
            {
                if (snapshot.Exists)
                {
                    FirebaseManager.Instance.UpdateDocument(
                        COLLECTION,
                        username,
                        updates,
                        onSuccess,
                        onError
                    );
                    return;
                }

                Dictionary<string, object> newDocument = new Dictionary<string, object>
                {
                    { "username", username },
                    {
                        DISABLED_FIELD,
                        disabled ? new List<object> { cardName } : new List<object>()
                    }
                };

                FirebaseManager.Instance.CreateDocument(
                    COLLECTION,
                    username,
                    newDocument,
                    _ => onSuccess?.Invoke(),
                    onError
                );
            },
            onError
        );
    }

    public enum CardRarityDebug
    {
        Bronze = 0,
        Silver = 1,
        Gold = 2
    }

    [BoxGroup("Debug Card Giver")]
    [Tooltip("Card library asset used to populate the dropdown list of cards.")]
    public CardLibrary cardLibrary;

    [BoxGroup("Debug Card Giver")]
    [ValueDropdown("GetCardNameOptions")]
    public string debugCardName = "Scavenge";

    [BoxGroup("Debug Card Giver")]
    public CardRarityDebug debugRarity = CardRarityDebug.Bronze;

    private IEnumerable<string> GetCardNameOptions()
    {
        if (cardLibrary != null && cardLibrary.cards != null && cardLibrary.cards.Count > 0)
        {
            List<string> names = new List<string>();
            foreach (var card in cardLibrary.cards)
            {
                if (!string.IsNullOrEmpty(card.Name) && !names.Contains(card.Name))
                    names.Add(card.Name);
            }
            return names;
        }

        return new List<string>
        {
            "Scavenge",
            "Crumpled", "Paper Shuriken", "Mache Plateguard", "Cardboard Spire", "Box Slam", "Origami Shredfall", "Shredstorm Hurricane",
            "Bottle Cap Barrage", "Eco-Edge Sword", "Bottle Blaster", "Bottle Barrage", "Recycled Polywall", "Plastic Wave",
            "Banana Splitter", "Popping Wrapper", "Croissant", "The Bonkstick",
            "Recharge", "Static Surge", "Overclock",
            "Tin Carapace", "Scrap Cleaver", "Corrosive Edge", "Steel Katana"
        };
    }

    private string InferTrashType(string cName)
    {
        if (cardLibrary != null && cardLibrary.cards != null)
        {
            foreach (var card in cardLibrary.cards)
            {
                if (string.Equals(card.Name, cName, StringComparison.OrdinalIgnoreCase))
                {
                    return card.Type switch
                    {
                        CardType.Paper => "paper",
                        CardType.Plastic => "plastic",
                        CardType.BioWaste => "food waste",
                        CardType.Metal => "metal",
                        CardType.EWaste => "e-waste",
                        _ => "other"
                    };
                }
            }
        }

        string upper = cName.ToUpperInvariant();
        if (upper.StartsWith("METAL") || upper.Contains("TIN") || upper.Contains("SCRAP") || upper.Contains("CORROSIVE") || upper.Contains("KATANA")) return "metal";
        if (upper.StartsWith("EWASTE") || upper.Contains("RECHARGE") || upper.Contains("STATIC") || upper.Contains("OVERCLOCK") || upper.Contains("BATTERY")) return "e-waste";
        if (upper == "CRUMPLED" || upper.Contains("PAPER") || upper.Contains("SHRED") || upper.Contains("MACHE") || upper.Contains("CARDBOARD") || upper.Contains("BOX")) return "paper";
        if (upper.Contains("BOTTLE") || upper.Contains("PLASTIC") || upper.Contains("POLY") || upper.Contains("CAP") || upper.Contains("ECO-EDGE")) return "plastic";
        if (upper.Contains("BANANA") || upper.Contains("WRAPPER") || upper.Contains("CROISSANT") || upper.Contains("BONK")) return "food waste";

        return "other";
    }

    [BoxGroup("Debug Card Giver")]
    [Button("Give Card To Collection", ButtonSizes.Medium)]
    public void DebugGiveCard()
    {
        string username = GetDebugUsername();
        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogError("[FBCardCollection] Cannot give card: No logged-in username found. Make sure to log in or run in play mode.");
            return;
        }

        if (string.IsNullOrWhiteSpace(debugCardName))
        {
            Debug.LogError("[FBCardCollection] Card name cannot be empty.");
            return;
        }

        string trashType = InferTrashType(debugCardName);
        int rarityVal = (int)debugRarity;

        Debug.Log($"[FBCardCollection] Giving card '{debugCardName}' (TrashType: '{trashType}', Rarity: {debugRarity}) to user '{username}'...");

        AddCardWithStatus(
            username,
            trashType,
            debugCardName,
            rarityVal,
            isDuplicate =>
            {
                string msg = isDuplicate
                    ? $"[Debug] Added duplicate card '{debugCardName}' to {username}'s collection."
                    : $"[Debug] Added new card '{debugCardName}' to {username}'s collection!";
                Debug.Log(msg);
                DynamicPopupToast.Instance?.ShowToast(msg);

                DeckManager deck = FindFirstObjectByType<DeckManager>();
                if (deck != null) deck.UpdateCardsList();
            },
            error => Debug.LogError($"[FBCardCollection] Failed to add card: {error}")
        );
    }

    private string GetDebugUsername()
    {
        Dictionary<string, object> profile =
            FBAuthentication.Instance != null
                ? FBAuthentication.Instance.CurrentProfile
                : null;

        if (profile == null && DataManager.Instance != null)
            profile = DataManager.Instance.GetProfile();

        return FirebaseDataHelper.GetString(profile, "username");
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void AddCard(
        string username,
        string trashType,
        string cardName,
        int rarity,
        Action onSuccess = null,
        Action<string> onError = null)
    {
        AddCardWithStatus(
            username,
            trashType,
            cardName,
            rarity,
            _ => onSuccess?.Invoke(),
            onError
        );
    }

    public void AddCardWithStatus(
        string username,
        string trashType,
        string cardName,
        int rarity,
        Action<bool> onSuccess = null,
        Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            onError?.Invoke("Username is required to save collection data.");
            return;
        }

        Dictionary<string, object> cardEntry = new Dictionary<string, object>
        {
            { "trashType", trashType },
            { "cardName", cardName },
            { "rarity", rarity }
        };

        FirebaseManager.Instance.GetDocument(
            COLLECTION,
            username,
            snapshot =>
            {
                if (snapshot.Exists)
                {
                    Dictionary<string, object> data = snapshot.ToDictionary();
                    bool isDuplicate = ContainsCard(data, trashType, cardName, rarity);
                    Dictionary<string, object> updates = new Dictionary<string, object>
                    {
                        { "cards", FieldValue.ArrayUnion(cardEntry) }
                    };

                    FirebaseManager.Instance.UpdateDocument(
                        COLLECTION,
                        username,
                        updates,
                        () => onSuccess?.Invoke(isDuplicate),
                        onError
                    );
                }
                else
                {
                    Dictionary<string, object> newDocument = new Dictionary<string, object>
                    {
                        { "username", username },
                        { "cards", new List<object> { cardEntry } }
                    };

                    FirebaseManager.Instance.CreateDocument(
                        COLLECTION,
                        username,
                        newDocument,
                        _ => onSuccess?.Invoke(false),
                        onError
                    );
                }
            },
            onError
        );
    }

    public void GetDungeonCounters(
        string username,
        Action<int, int, int> onSuccess,
        Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            onError?.Invoke("Username is required to load dungeon counters.");
            return;
        }

        FirebaseManager.Instance.GetDocument(
            COLLECTION,
            username,
            snapshot =>
            {
                if (!snapshot.Exists)
                {
                    onSuccess?.Invoke(0, 0, 0);
                    return;
                }

                Dictionary<string, object> data = snapshot.ToDictionary();
                onSuccess?.Invoke(
                    GetCounterOrLegacyCount(data, "plastic_scan_count", "plastic"),
                    GetCounterOrLegacyCount(data, "paper_scan_count", "paper"),
                    GetCounterOrLegacyCount(data, "food_waste_scan_count", "food waste")
                );
            },
            onError
        );
    }

    private static string GetDungeonCounterField(string trashType)
    {
        switch (NormalizeDungeonType(trashType))
        {
            case "plastic": return "plastic_scan_count";
            case "paper": return "paper_scan_count";
            case "food waste": return "food_waste_scan_count";
            default: return null;
        }
    }

    private static string NormalizeDungeonType(string trashType)
    {
        string normalized = trashType?.Trim().ToLowerInvariant();
        return normalized == "plastic bottle" ? "plastic" : normalized;
    }

    private static int GetCounterOrLegacyCount(
        Dictionary<string, object> data,
        string counterField,
        string trashType)
    {
        return TryGetCounter(data, counterField, out int count)
            ? count
            : CountCardsByTrashType(data, trashType);
    }

    private static bool TryGetCounter(
        Dictionary<string, object> data,
        string counterField,
        out int count)
    {
        if (data.TryGetValue(counterField, out object value))
        {
            if (value is long longValue)
            {
                count = (int)longValue;
                return true;
            }

            if (value is int intValue)
            {
                count = intValue;
                return true;
            }
        }

        count = 0;
        return false;
    }

    private static int CountCardsByTrashType(
        Dictionary<string, object> data,
        string trashType)
    {
        if (!data.TryGetValue("cards", out object rawCards) ||
            rawCards is not List<object> cardList)
        {
            return 0;
        }

        string targetType = NormalizeDungeonType(trashType);
        int count = 0;

        foreach (object rawCard in cardList)
        {
            if (rawCard is Dictionary<string, object> card &&
                NormalizeDungeonType(FirebaseDataHelper.GetString(card, "trashType")) == targetType)
            {
                count++;
            }
        }

        return count;
    }

    private static bool ContainsCard(
        Dictionary<string, object> data,
        string trashType,
        string cardName,
        int rarity)
    {
        if (!data.TryGetValue("cards", out object rawCards) ||
            rawCards is not List<object> cardList)
        {
            return false;
        }

        foreach (object rawCard in cardList)
        {
            if (rawCard is not Dictionary<string, object> card ||
                FirebaseDataHelper.GetString(card, "trashType") != trashType ||
                FirebaseDataHelper.GetString(card, "cardName") != cardName ||
                !card.TryGetValue("rarity", out object rawRarity))
            {
                continue;
            }

            if ((rawRarity is long longRarity && longRarity == rarity) ||
                (rawRarity is int intRarity && intRarity == rarity))
            {
                return true;
            }
        }

        return false;
    }

    // Returns each card as a (trashType, cardName, rarity) tuple.
    public void GetCards(
        string username,
        Action<List<(string trashType, string cardName, int rarity)>> onSuccess,
        Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            onError?.Invoke("Username is required to load collection data.");
            return;
        }

        FirebaseManager.Instance.GetDocument(
            COLLECTION,
            username,
            snapshot =>
            {
                List<(string, string, int)> cards = new List<(string, string, int)>();

                if (!snapshot.Exists)
                {
                    onSuccess?.Invoke(cards);
                    return;
                }

                Dictionary<string, object> data = snapshot.ToDictionary();
                if (data.TryGetValue("cards", out object rawCards) &&
                    rawCards is List<object> cardList)
                {
                    foreach (object rawCard in cardList)
                    {
                        if (rawCard is not Dictionary<string, object> card)
                            continue;

                        string trashType = FirebaseDataHelper.GetString(card, "trashType") ?? "";
                        string cardName = FirebaseDataHelper.GetString(card, "cardName") ?? "";
                        int rarity = card.TryGetValue("rarity", out object rawRarity) &&
                            rawRarity is long rarityValue
                                ? (int)rarityValue
                                : 0;

                        cards.Add((trashType, cardName, rarity));
                    }
                }

                onSuccess?.Invoke(cards);
            },
            onError
        );
    }

    public void ClearCards(
        string username,
        Action onSuccess = null,
        Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            onError?.Invoke("Username is required to clear collection data.");
            return;
        }

        FirebaseManager.Instance.DocumentExists(
            COLLECTION,
            username,
            exists =>
            {
                if (exists)
                {
                    FirebaseManager.Instance.UpdateDocument(
                        COLLECTION,
                        username,
                        new Dictionary<string, object>
                        {
                            { "cards", new List<object>() }
                        },
                        onSuccess,
                        onError
                    );
                    return;
                }

                FirebaseManager.Instance.CreateDocument(
                    COLLECTION,
                    username,
                    new Dictionary<string, object>
                    {
                        { "username", username },
                        { "cards", new List<object>() }
                    },
                    _ => onSuccess?.Invoke(),
                    onError
                );
            },
            onError
        );
    }
}
