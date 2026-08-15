using UnityEngine;
using System.IO;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

public class DungeonsHandler : MonoBehaviour
{
    [HideInInspector] public int plasticCards = 0;
    [HideInInspector] public int paperCards = 0;
    [HideInInspector] public int foodwasteCards = 0;
    
    private string filePath;
    private List<string> cards;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "Card_Collection");
        filePath = Path.Combine(folderPath, "card_collection.txt");
        if (File.Exists(filePath))
        {
            CheckDeck();
        }
        else
        {
            Debug.LogWarning("Card collection file not found at: " + filePath + ". Creating a new .txt file...");
            File.WriteAllText(filePath, "");
        }    
    }

    // Update is called once per frame
    void Update()
    {

    }

    void CheckDeck()
    {
        cards = File.ReadLines(filePath).ToList();
        foreach (string card in cards)
        {
            string[] cardSplit = card.Split(':');
            if (cardSplit[0] == "plastic bottle")
                plasticCards++;
            else if (cardSplit[0] == "paper")
                paperCards++;
            else if (cardSplit[0] == "food waste")
                foodwasteCards++;
            else
                Debug.LogWarning($"Card type of {cardSplit[1]} Not Found. Skipping Card...");
        }
    }
}
