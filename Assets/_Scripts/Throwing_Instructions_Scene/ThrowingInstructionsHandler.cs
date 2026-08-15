using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ThrowingInstructionsHandler: MonoBehaviour
{
    public TextMeshProUGUI trashTypeText;
    public TextMeshProUGUI trashBinsText;
    public TextMeshProUGUI cardGet;
    public TextMeshProUGUI wrongScan;
    public RawImage trashTypeImage;
    public TextAsset trashRulesAsset;
    public TextMeshProUGUI collectButtonText;

    public Image Popup;

    private string predictionPath;
    private string prediction;
    private string cardToGet;
    private int cardRarity = 0;

    void Start()
    {
        predictionPath = System.IO.Path.Combine(Application.persistentDataPath, "Prediction", "prediction.txt");
        prediction = "";

        if (System.IO.File.Exists(predictionPath))
        {
            prediction = System.IO.File.ReadAllText(predictionPath).Trim().ToLower();
            trashTypeText.text = prediction;
            //START ADDING SHIT HERE, THE REST BELOW BAKA DAI NA PAGHALION/PAGBAGUHON
            //StartCollection();
            StartRandomCard();

        }
        else
        {
            trashTypeText.text = "Prediction not found.";
            Debug.LogWarning("Prediction file not found at: " + predictionPath);
            return;
        }

        // Show image
        LoadTrashTypeImage(prediction);

        // Show bin types
        if (trashRulesAsset == null)
        {
            trashBinsText.text = "No rules assigned.";
            Debug.LogWarning("No TextAsset assigned for trash rules.");
            return;
        }

        string[] lines = trashRulesAsset.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        List<string> matchingBins = new List<string>();

        foreach (string line in lines)
        {
            string[] parts = line.Split(':');
            if (parts.Length == 2 && parts[0].Trim().ToLower() == prediction)
            {
                string binLabel = parts[1].Trim();
                if (!matchingBins.Contains(binLabel))
                {
                    matchingBins.Add(binLabel);
                }
            }
        }
        string binFirst = matchingBins.Count > 0 ? matchingBins[0] : "None";
        string correctBin = string.Join(" or ", matchingBins);
        Popup.color = binFirst switch
        {
            "recyclable" => Color.green,
            "biodegradable" => Color.yellow,
            "nonbiodegradable" => Color.red,
            _ => Color.white
        };

        trashBinsText.text = matchingBins.Count > 0 ? $"Please Throw in {correctBin.ToUpper()} Bin!" : "No matching bins.";
    }

    void LoadTrashTypeImage(string prediction)
    {
        if (trashTypeImage == null) return;

        string imageName = prediction.Replace(" ", "_");
        Texture2D tex = Resources.Load<Texture2D>("TrashTypeImage/" + imageName);

        if (tex != null)
        {
            trashTypeImage.texture = tex;
        }
        else
        {
            Debug.LogWarning("Image not found in Resources/TrashTypeImage/: " + imageName);
        }
    }

    public void StartCollection()
    {
        string cardCollectionPath = System.IO.Path.Combine(Application.persistentDataPath, "Card_Collection", "card_collection.txt");
        string cardsCollection;
        if (System.IO.File.Exists(cardCollectionPath) && cardToGet != "Unknown")
        {
            cardsCollection = System.IO.File.ReadAllText(cardCollectionPath);
            cardsCollection += $"{prediction}:{cardToGet}:{cardRarity}\n";
            System.IO.File.WriteAllText(cardCollectionPath, cardsCollection);

            DeckManager myDeckLong = FindAnyObjectByType<DeckManager>(); // wink wink
            if (myDeckLong != null)
                myDeckLong.UpdateCardsList();
        }
        else
        {
            Debug.LogWarning($"Card Type not found or Card Collection not found in {cardCollectionPath}");
        }
    }

    private void StartRandomCard()
    {
        int rand = Random.Range(1, 101);
        switch (prediction)
        {
            case "paper":
                if (rand >= 1 && rand <= 30)
                    cardToGet = Random.Range(0, 2) == 0 ? "Crumpled" : "Paper Shuriken";
                if (rand > 30 && rand <= 55)
                    cardToGet = "Mache Plateguard";
                if (rand > 55 && rand <= 75)
                    cardToGet = "Cardboard Spire";
                if (rand > 75 && rand <= 90)
                    cardToGet = Random.Range(0, 2) == 0 ? "Box Slam" : "Origami Shredfall";
                if (rand > 90 && rand <= 100)
                    cardToGet = "Shredstorm Hurricane";
                break;
            case "plastic bottle":
                if (rand >= 1 && rand <= 50)
                    cardToGet = Random.Range(0, 2) == 0 ? "Bottle Cap Barrage" : "Eco-Edge Sword";
                if (rand > 51 && rand <= 75)
                    cardToGet = "Bottle Blaster";
                if (rand > 75 && rand <= 90)
                    cardToGet = rand >= 1 && rand <= 50 ? "Bottle Barrage" : "Recycled Polywall";
                if (rand > 90 && rand <= 100)
                    cardToGet = "Plastic Wave";
                break;
            case "food waste":
                if (rand >= 1 && rand <= 65)
                    cardToGet = Random.Range(0, 2) == 1 ? "Banana Splitter" : "Popping Wrapper";
                if (rand > 65 && rand <= 90)
                    cardToGet = "Croissant";
                if (rand > 90 && rand <= 100)
                    cardToGet = "The Bonkstick";
                break;
            default:
                cardToGet = "Unknown";
                Debug.LogWarning($"{prediction} not found! Are you missing some variables?");
                break;
        }

        cardRarity = Random.Range(0, 3);
        string rarity = cardRarity switch
        {
            0 => "Bronze",
            1 => "Silver",
            2 => "Gold",
            _ => "IDK",
        };

        if (cardToGet != "Unknown")
        {
            cardGet.text = $"You got a {cardToGet} card!";
            if (rarity != "IDK")
                cardGet.text += $"\nWith a {rarity} Badge!";
        }
        else
        {
            cardGet.text = $"{prediction}s are not yet available :(";
            collectButtonText.text = "Back to Dungeon Select";
            wrongScan.text = "Back to Scanning";
        }
        DeckManager decker = FindAnyObjectByType<DeckManager>();
        if (decker != null)
        {
            decker.UpdateCardsList();
        }
    }
}
