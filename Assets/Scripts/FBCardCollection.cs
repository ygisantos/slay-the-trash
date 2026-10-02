using System;
using System.Collections.Generic;
using Firebase.Firestore;
using UnityEngine;

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
                string counterField = GetDungeonCounterField(trashType);

                if (snapshot.Exists)
                {
                    Dictionary<string, object> data = snapshot.ToDictionary();
                    bool isDuplicate = ContainsCard(data, trashType, cardName, rarity);
                    Dictionary<string, object> updates = new Dictionary<string, object>
                    {
                        { "cards", FieldValue.ArrayUnion(cardEntry) }
                    };

                    if (counterField != null)
                    {
                        updates[counterField] = TryGetCounter(data, counterField, out int currentCount)
                            ? FieldValue.Increment(1)
                            : CountCardsByTrashType(data, trashType) + 1;
                    }

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

                    if (counterField != null)
                        newDocument[counterField] = 1L;

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
