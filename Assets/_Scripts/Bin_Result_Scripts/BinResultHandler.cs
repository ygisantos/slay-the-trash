using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class BinResultHandler : MonoBehaviour
{
    public Text BinResult;
    public Text BinType;
    public Text CardType;
    public Text Timer;

    public RawImage CardGainedImage;

    public Button MainMenuButton; // Assign in Inspector

    public TextAsset trashRulesFile; // Drag trashrules.txt here

    private string displayedImageName = null;

    private void Start()
    {
        MainMenuButton.gameObject.SetActive(false); // Hide initially
        MainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        StartCoroutine(CheckBinResult());
    }

    IEnumerator CheckBinResult()
    {
        string predictionPath = Path.Combine(Application.persistentDataPath, "Prediction/prediction.txt");
        string ocrPath = Path.Combine(Application.persistentDataPath, "OCR/ocr.txt");

        if (trashRulesFile == null || !File.Exists(predictionPath) || !File.Exists(ocrPath))
        {
            Debug.LogError("One or more files are missing.");
            yield break;
        }

        string predictedClass = File.ReadAllText(predictionPath).Trim().ToLower();
        string ocrResult = File.ReadAllText(ocrPath).Trim().ToLower();

        BinType.text = ocrResult;

        Dictionary<string, List<string>> trashRules = LoadTrashRulesFromTextAsset(trashRulesFile);

        bool isCorrect = false;
        if (trashRules.TryGetValue(predictedClass, out List<string> possibleBins))
        {
            isCorrect = possibleBins.Contains(ocrResult);
        }

        if (isCorrect)
        {
            BinResult.text = "Correct Bin";
            CardType.text = "Gained 1 Playing Card";

            string folder = $"Cards/PlayingCards/{predictedClass.Replace(" ", "_")}";
            Texture2D[] images = Resources.LoadAll<Texture2D>(folder);
            if (images.Length > 0)
            {
                var selectedImage = images[Random.Range(0, images.Length)];
                CardGainedImage.texture = selectedImage;
                displayedImageName = selectedImage.name; // Save image name for later
            }
            else
            {
                Debug.LogWarning("No images found in: " + folder);
                displayedImageName = null;
            }

            Timer.text = "";
            MainMenuButton.gameObject.SetActive(true);
            MainMenuButton.interactable = true;
        }
        else
        {
            BinResult.text = "Incorrect Bin";
            CardType.text = "Gained 1 Debuff Card";

            Texture2D[] images = Resources.LoadAll<Texture2D>("Cards/DebuffCards");
            if (images.Length > 0)
            {
                var selectedImage = images[Random.Range(0, images.Length)];
                CardGainedImage.texture = selectedImage;
                displayedImageName = selectedImage.name; // Save image name for later
            }
            else
            {
                Debug.LogWarning("No images found in DebuffCards");
                displayedImageName = null;
            }

            MainMenuButton.gameObject.SetActive(false);
            StartCoroutine(StartCountdown());
        }
    }

    IEnumerator StartCountdown()
    {
        int countdown = 10;
        while (countdown > 0)
        {
            Timer.text = $"Returning to Trashbin Scanner in {countdown} seconds";
            yield return new WaitForSeconds(1f);
            countdown--;
        }

        SaveDisplayedImageName();

        BinResultSceneHandler sceneHandler = FindFirstObjectByType<BinResultSceneHandler>();
        if (sceneHandler != null)
        {
            sceneHandler.LoadOCRScene();
        }
        else
        {
            Debug.LogWarning("BinResultSceneHandler not found in scene.");
        }
    }

    void OnMainMenuButtonClicked()
    {
        SaveDisplayedImageName();
        // Here you can add code to load your main menu or do other things on button click
        Debug.Log("MainMenuButton clicked and image saved.");
    }

    void SaveDisplayedImageName()
    {
        if (string.IsNullOrEmpty(displayedImageName))
        {
            Debug.LogWarning("No displayed image to save.");
            return;
        }

        string folderPath = Path.Combine(Application.persistentDataPath, "Card_Collection");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, "card_collection.txt");

        try
        {
            File.AppendAllText(filePath, displayedImageName + "\n");
            Debug.Log($"Saved displayed image name '{displayedImageName}' to {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save card collection: " + e.Message);
        }
    }

    Dictionary<string, List<string>> LoadTrashRulesFromTextAsset(TextAsset textAsset)
    {
        Dictionary<string, List<string>> rules = new Dictionary<string, List<string>>();
        string[] lines = textAsset.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var parts = line.Split(':');
            if (parts.Length == 2)
            {
                string key = parts[0].Trim().ToLower();
                string value = parts[1].Trim().ToLower();
                if (!rules.ContainsKey(key))
                    rules[key] = new List<string>();
                rules[key].Add(value);
            }
        }
        return rules;
    }
}
