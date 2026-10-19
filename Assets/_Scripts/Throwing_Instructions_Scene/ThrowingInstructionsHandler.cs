using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Collections;
 
public class ThrowingInstructionsHandler: MonoBehaviour
{
    private bool isLoading = false;
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
        _ = DynamicPopupToast.Instance;
        predictionPath = System.IO.Path.Combine(Application.persistentDataPath, "Prediction", "prediction.txt");
        prediction = "";

        if (System.IO.File.Exists(predictionPath))
        {
            prediction = System.IO.File.ReadAllText(predictionPath).Trim().ToLower();
            trashTypeText.text = prediction;
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
        if (cardToGet == "Unknown")
            return;

        string username = GetCurrentUsername();
        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogWarning("Cannot save card to collection without a logged-in username.");
            return;
        }

        // NOTE: the scan itself is already recorded by CameraHandler.Predict ->
        // FBLeaderboard.AddScanStats. Recording it here as well incremented
        // paper/plastic_scan_count twice per scan, which made dungeon progress
        // advance two steps for every one scanned. Only the card is saved here.

        FBCardCollection.Instance.AddCardWithStatus(
            username,
            prediction,
            cardToGet,
            cardRarity,
            isDuplicate =>
            {
                DeckManager myDeckLong = FindAnyObjectByType<DeckManager>();
                if (myDeckLong != null)
                    myDeckLong.UpdateCardsList();

                if (isDuplicate)
                {
                    DynamicPopupToast.Instance?.ShowToast(
                        $"Duplicate card: {cardToGet}. Scan counted toward dungeon progress.", 6
                    );
                    StartCoroutine(LoadWithDelay("DungeonSelection", 3f));
                }
                else
                {
                    StartCoroutine(LoadWithDelay("DungeonSelection", 0.1f));
                }
            },
            error => Debug.LogError($"Failed to save card to collection: {error}")
        );
    }

    
    private IEnumerator LoadWithDelay(string sceneName, float delay)
    {
        if (isLoading) yield break;
        isLoading = true;

        yield return new WaitForSeconds(delay);
        Transitioner.Instance.TransitionToScene(sceneName);
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
            case "plastic":
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
            case "metal":
                if (rand >= 1 && rand <= 40)
                    cardToGet = "Tin Carapace";
                else if (rand > 40 && rand <= 70)
                    cardToGet = "Scrap Cleaver";
                else if (rand > 70 && rand <= 90)
                    cardToGet = "Corrosive Edge";
                else
                    cardToGet = "Steel Katana";
                break;
            case "e-waste":
            case "ewaste":
            case "e_waste":
                if (rand >= 1 && rand <= 50)
                    cardToGet = "Recharge";
                else if (rand > 50 && rand <= 80)
                    cardToGet = "Overclock";
                else
                    cardToGet = "Static Surge";
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

        // StartCollection();
    }
}
