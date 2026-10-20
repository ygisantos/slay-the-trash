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

    // Category colours win over the bin colour, so categories that share a bin stay
    // distinguishable (metal shares "recyclable" with plastic, but reads grey).
    private static readonly Dictionary<string, Color> CategoryColors = new Dictionary<string, Color>
    {
        { "metal",   new Color(0.72f, 0.74f, 0.78f) },  // grey
        { "e-waste", new Color(0.90f, 0.35f, 0.32f) },  // red
        { "e_waste", new Color(0.90f, 0.35f, 0.32f) },
        { "ewaste",  new Color(0.90f, 0.35f, 0.32f) },
    };

    void Start()
    {
        _ = DynamicPopupToast.Instance;
        predictionPath = System.IO.Path.Combine(Application.persistentDataPath, "Prediction", "prediction.txt");
        prediction = "";

        if (System.IO.File.Exists(predictionPath))
        {
            prediction = System.IO.File.ReadAllText(predictionPath).Trim().ToLower();
            trashTypeText.text = DisplayName(prediction);
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

        Popup.color = ColorForCategory(prediction, binFirst);

        trashBinsText.text = matchingBins.Count > 0
            ? $"Please throw it in the {TitleCase(correctBin)} bin!"
            : "No matching bins.";
    }

    // A category colour (metal grey, e-waste red) wins over the bin colour, so
    // categories that share a bin stay visually distinct. Falls back to the bin.
    private static Color ColorForCategory(string trashType, string binLabel)
    {
        if (!string.IsNullOrEmpty(trashType) &&
            CategoryColors.TryGetValue(trashType, out Color categoryColor))
        {
            return categoryColor;
        }

        switch (NormalizeKey(binLabel))
        {
            case "recyclable": return Color.green;
            case "biodegradable": return Color.yellow;
            case "nonbiodegradable": return Color.red;
            default: return Color.white;
        }
    }

    // Drops hyphens/underscores/spaces so "non-biodegradable" matches "nonbiodegradable".
    private static string NormalizeKey(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Trim().ToLowerInvariant()
            .Replace("-", string.Empty)
            .Replace("_", string.Empty)
            .Replace(" ", string.Empty);
    }

    private static string TitleCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        string[] words = value.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length == 0)
                continue;

            words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
        }

        return string.Join(" ", words);
    }

    // Player-facing trash name: "e-waste" reads "E-Waste", "paper" reads "Paper".
    private static string DisplayName(string trashType)
    {
        switch (trashType)
        {
            case "e-waste":
            case "e_waste":
            case "ewaste":
                return "E-Waste";
            case "food waste":
                return "Food Waste";
            default:
                return TitleCase(trashType);
        }
    }

    void LoadTrashTypeImage(string prediction)
    {
        if (trashTypeImage == null) return;

        string imageName = TrashTypeImageName(prediction);
        Texture2D tex = Resources.Load<Texture2D>("TrashTypeImage/" + imageName);

        if (tex != null)
        {
            trashTypeImage.texture = tex;
            trashTypeImage.enabled = true;
        }
        else
        {
            // Without this the previous scan's icon would stay on screen, which reads
            // as the wrong trash type. Hide it until the sprite is added.
            trashTypeImage.texture = null;
            trashTypeImage.enabled = false;
            Debug.LogWarning(
                "Image not found in Resources/TrashTypeImage/: " + imageName +
                " (the trash-type image is hidden until one is added)");
        }
    }

    // Sprite name in Resources/TrashTypeImage for each scanned category.
    private static string TrashTypeImageName(string trashType)
    {
        switch (trashType)
        {
            case "metal":
                return "Metal";
            case "e-waste":
            case "e_waste":
            case "ewaste":
                return "EWaste";
            case "plastic bottle":
                return "plastic_bottle";
            case "plastic":
                return "plastic";
            case "food waste":
                return "food_waste";
            case "paper":
                return "paper";
            default:
                return string.IsNullOrEmpty(trashType)
                    ? "paper"
                    : trashType.Replace(" ", "_");
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
