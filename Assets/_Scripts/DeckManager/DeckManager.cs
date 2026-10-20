using DG.Tweening.Core.Easing;
using DG.Tweening.Plugins.Core.PathCore;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.GPUSort;

public class DeckManager : MonoBehaviour
{
    [SerializeField] private HeroData heroData;
    [SerializeField] private List<CardData> cardTypes;

    [SerializeField] private List<RarityTypes> cardRarity;
    private List<(string trashType, string cardName, int rarity)> cardEntries = new();
    public static DeckManager instance { get; private set; }
    public static event Action OnDeckUpdated;
    public List<string> cardNames = new List<string>();

    // Normalised names of cards the player switched off in the collection screen.
    private readonly HashSet<string> disabledCards = new HashSet<string>();

    public enum RarityTypes
    {
        Bronze,
        Silver,
        Gold,
        IDK
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(instance.gameObject);
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        UpdateCardsList();
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public void UpdateCardsList()
    {
        string username = GetCurrentUsername();
        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogWarning("Cannot load card collection without a logged-in username.");
            return;
        }

        FBCardCollection.Instance.GetCards(
            username,
            cards =>
            {
                cardEntries = cards;

                // The disabled set has to be known before the deck is rebuilt,
                // otherwise a disabled card could reach the battle pool.
                FBCardCollection.Instance.GetDisabledCards(
                    username,
                    disabled =>
                    {
                        SetDisabledCards(disabled);
                        RebuildDeck();
                    },
                    error =>
                    {
                        Debug.LogError($"Failed to load disabled cards: {error}");
                        RebuildDeck();
                    }
                );
            },
            error => Debug.LogError($"Failed to load card collection: {error}")
        );
    }

    private void RebuildDeck()
    {
        UpdateDeck();
        UpdateCardNames();
        OnDeckUpdated?.Invoke();
    }

    // ---- disabled cards ----

    public void SetDisabledCards(IEnumerable<string> names)
    {
        disabledCards.Clear();

        if (names == null)
            return;

        foreach (string name in names)
        {
            string clean = NormalizeCardName(name);

            if (!string.IsNullOrEmpty(clean))
                disabledCards.Add(clean);
        }
    }

    public bool IsCardDisabled(string cardName)
    {
        string clean = NormalizeCardName(cardName);
        return !string.IsNullOrEmpty(clean) && disabledCards.Contains(clean);
    }

    // Card names are compared without case, spaces or underscores so that
    // "Bottle Blaster" and "BottleBlaster" resolve to the same card.
    public static string NormalizeCardName(string cardName)
    {
        if (string.IsNullOrWhiteSpace(cardName))
            return string.Empty;

        return cardName.Trim().Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
    }

    void UpdateDeck()
    {
        heroData.Deck.Clear(); //Clear Deck to give room to actual cards from Firestore
        cardRarity.Clear();

        foreach (var entry in cardEntries)
        {
            // Cards the player disabled are left out of the deck entirely.
            if (IsCardDisabled(entry.cardName))
                continue;

            string entryClean = entry.cardName?.Trim().Replace(" ", "").Replace("_", "").ToLowerInvariant();

            for (int i = 0; i < cardTypes.Count; i++)
            {
                if (cardTypes[i] == null) continue;

                string assetName = cardTypes[i].name;
                string assetClean = assetName?.Trim().Replace(" ", "").Replace("_", "").ToLowerInvariant();

                if (string.Equals(assetName, entry.cardName, StringComparison.OrdinalIgnoreCase) || assetClean == entryClean)
                {
                    heroData.Deck.Add(cardTypes[i]);
                    RarityTypes rar = entry.rarity switch
                    {
                        0 => RarityTypes.Bronze,
                        1 => RarityTypes.Silver,
                        2 => RarityTypes.Gold,
                        _ => RarityTypes.IDK
                    };
                    cardRarity.Add(rar);
                    break;
                }
            }
        }
    }
    public void UpdateCardNames()
    {
        cardNames.Clear();
        foreach (var entry in cardEntries)
        {
            cardNames.Add(entry.cardName);
        }
    }


    public void InitializeDeck()
    {
        string username = GetCurrentUsername();
        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogWarning("Cannot reset card collection without a logged-in username.");
            return;
        }

        FBCardCollection.Instance.ClearCards(
            username,
            () => {
                UpdateCardsList();
                DynamicPopupToast.Instance.ShowToast("Card collection has been reset.");
            },
            error => Debug.LogError($"Failed to reset card collection: {error}")
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

    //NOT NECESSARY/REDUNDANT PERO MAYBE NEED SA FUTURE, YOU NEVER KNOW

    //private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    //{
    //    InitializeDeck(scene);
    //}

    //private void InitializeDeck(Scene scene)
    //{
    //    if (scene.name == "MainMenuScene")
    //    {
    //        System.IO.File.WriteAllText(filePath, "");
    //        Debug.Log("Emptied hero's deck");
    //    }
    //}

    //private void OnEnable()
    //{
    //    SceneManager.sceneLoaded += OnSceneLoaded;
    //}

    //private void OnDisable()
    //{
    //    SceneManager.sceneLoaded -= OnSceneLoaded;
    //}
}
