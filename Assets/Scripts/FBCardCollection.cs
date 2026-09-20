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
                            { "cards", FieldValue.ArrayUnion(cardEntry) }
                        },
                        onSuccess,
                        onError
                    );
                }
                else
                {
                    FirebaseManager.Instance.CreateDocument(
                        COLLECTION,
                        username,
                        new Dictionary<string, object>
                        {
                            { "username", username },
                            { "cards", new List<object> { cardEntry } }
                        },
                        _ => onSuccess?.Invoke(),
                        onError
                    );
                }
            },
            onError
        );
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
    }
}
