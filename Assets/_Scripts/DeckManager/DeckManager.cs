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
                UpdateDeck();
                UpdateCardNames();
                OnDeckUpdated?.Invoke();
            },
            error => Debug.LogError($"Failed to load card collection: {error}")
        );
    }

    void UpdateDeck()
    {
        heroData.Deck.Clear(); //Clear Deck to give room to actual cards from Firestore
        cardRarity.Clear();

        foreach (var entry in cardEntries)
        {
            for (int i = 0; i < cardTypes.Count; i++)
            {
                if (cardTypes[i].name == entry.cardName)
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
            UpdateCardsList,
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
