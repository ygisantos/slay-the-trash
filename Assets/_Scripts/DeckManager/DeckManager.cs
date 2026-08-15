using DG.Tweening.Core.Easing;
using DG.Tweening.Plugins.Core.PathCore;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.GPUSort;
using Path = System.IO.Path;

public class DeckManager : MonoBehaviour
{
    [SerializeField] private HeroData heroData;
    [SerializeField] private List<CardData> cardTypes;

    [SerializeField] private List<RarityTypes> cardRarity;
    private List<string> cardAsString;
    private string filePath;
    public static DeckManager instance { get; private set; }
    public List<string> cardNames;

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

        string folderPath = Path.Combine(Application.persistentDataPath, "Card_Collection");
        filePath = Path.Combine(folderPath, "card_collection.txt");

        if (!File.Exists(filePath))
        {
            //string content = File.ReadAllText(filePath);
            //Cards.text = string.IsNullOrWhiteSpace(content) ? "No cards collected yet." : content;
            Debug.LogWarning("Card collection file not found at: " + filePath + ". Creating a new .txt file...");
            Directory.CreateDirectory(folderPath);
            File.WriteAllText(filePath, "");
        }

        //InitializeDeck();
        UpdateCardsList();    
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public void UpdateCardsList()
    {
        // CONVERT TXT FILE TO STRING LIST
        try
        {
            cardAsString = File.ReadLines(filePath).ToList();
            //StartCoroutine(ConvertToPrefabs());
        }
        catch (FileNotFoundException)
        {
            Debug.LogWarning($"Card collection file not found at: {filePath}");
        }
        
        UpdateDeck();
        UpdateCardNames();
    }

    void UpdateDeck()
    {
        heroData.Deck.Clear(); //Clear Deck to give room to actual cards in txt file
        foreach (var card in cardAsString)
        {
            string[] cardAndRar = card.Split(':');
            for (int i = 0; i < cardTypes.Count; i++)
            {
                if (cardTypes[i].name == cardAndRar[0])
                {
                    heroData.Deck.Add(cardTypes[i]);
                    RarityTypes rar = cardAndRar[1] switch
                    {
                        "0" => RarityTypes.Bronze,
                        "1" => RarityTypes.Silver,
                        "2" => RarityTypes.Gold,
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
        foreach (string s in cardAsString)
        {
            string[] parts = s.Split(':');
            if (parts.Length >= 3)
                cardNames.Add(parts[1]);
        }
    }


    public void InitializeDeck()
    {
        File.WriteAllText(filePath, "");
        cardRarity.Clear();
        UpdateCardsList();
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
