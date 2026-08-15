using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class OCRSpaceUploader : MonoBehaviour
{
    public Text ExtractedText; // Assign in Inspector
    public Text CardGainText;  // Assign in Inspector
    public TextAsset labelsFile; // One label per line
    public TextAsset trashRulesFile; // Format: trash:bin per line

    private const string apiKey = "K85755317388957";
    private const string apiUrl = "https://api.ocr.space/parse/image";

    [Serializable]
    public class ParsedResult { public string ParsedText; }
    [Serializable]
    public class OCRResponse { public ParsedResult[] ParsedResults; }

    int CountInOrderMatches(string input, string label)
    {
        int inputIndex = 0, matchCount = 0;
        input = input.ToLower(); label = label.ToLower();
        foreach (char c in label)
        {
            while (inputIndex < input.Length && input[inputIndex] != c)
                inputIndex++;
            if (inputIndex == input.Length) break;
            matchCount++;
            inputIndex++;
        }
        return matchCount;
    }

    string GetBestMatch(string input, List<string> options)
    {
        string best = null;
        int bestScore = 0;
        foreach (var label in options)
        {
            int score = CountInOrderMatches(input, label);
            if (score >= label.Length / 2 && score > bestScore)
            {
                best = label;
                bestScore = score;
            }
        }
        return best;
    }

    Dictionary<string, string> LoadTrashRules(TextAsset file)
    {
        Dictionary<string, string> rules = new Dictionary<string, string>();
        if (file == null) return rules;
        string[] lines = file.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var parts = line.Split(':');
            if (parts.Length == 2)
                rules[parts[0].Trim().ToLower()] = parts[1].Trim().ToLower();
        }
        return rules;
    }

    public IEnumerator UploadImageForOCR(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            Debug.LogError("File not found: " + fullPath);
            Display("Image not found.");
            yield break;
        }

        byte[] imageBytes = File.ReadAllBytes(fullPath);
        string base64 = Convert.ToBase64String(imageBytes);

        WWWForm form = new WWWForm();
        form.AddField("apikey", apiKey);
        form.AddField("base64Image", "data:image/jpeg;base64," + base64);
        form.AddField("language", "eng");

        Display("Sending to OCR...");

        using (UnityWebRequest www = UnityWebRequest.Post(apiUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("OCR request failed: " + www.error);
                Display("OCR error.");
                yield break;
            }

            string json = www.downloadHandler.text;
            Debug.Log("OCR JSON: " + json);

            try
            {
                OCRResponse result = JsonUtility.FromJson<OCRResponse>(json);
                if (result.ParsedResults != null && result.ParsedResults.Length > 0)
                {
                    string extracted = result.ParsedResults[0].ParsedText.Trim().ToLower();
                    Display("OCR Result: " + extracted);

                    List<string> knownLabels = new List<string>();
                    if (labelsFile != null)
                    {
                        string[] lines = labelsFile.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string line in lines)
                            knownLabels.Add(line.Trim().ToLower());
                    }

                    string bestMatch = GetBestMatch(extracted, knownLabels);
                    Debug.Log("Best Match: " + (bestMatch ?? "None"));

                    // Save bestMatch to persistent data path at OCR/ocr.txt
                    string ocrDir = Path.Combine(Application.persistentDataPath, "OCR");
                    if (!Directory.Exists(ocrDir))
                        Directory.CreateDirectory(ocrDir);

                    string ocrPath = Path.Combine(ocrDir, "ocr.txt");
                    try
                    {
                        File.WriteAllText(ocrPath, bestMatch ?? "None");
                        Debug.Log("Best match saved to: " + ocrPath);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Failed to save best match: " + e.Message);
                    }

                    string predictionPath = Path.Combine(Application.persistentDataPath, "Prediction/prediction.txt");
                    string predictedClass = "unknown";
                    if (File.Exists(predictionPath))
                    {
                        predictedClass = File.ReadAllText(predictionPath).Trim().ToLower();
                        Debug.Log("Prediction.txt content: " + predictedClass);
                    }
                    else
                    {
                        Debug.LogWarning("Prediction file not found at: " + predictionPath);
                    }

                    var rules = LoadTrashRules(trashRulesFile);
                    bool isInvalidBin = true;

                    if (bestMatch != null && rules.TryGetValue(predictedClass, out string correctBin))
                    {
                        isInvalidBin = false;
                        if (correctBin == bestMatch)
                            CardGainText.text = "Gained 1 card";
                        else
                            CardGainText.text = "Gained 1 debuff card";

                        Display("Best Match: " + bestMatch);
                    }
                    else
                    {
                        Display("Invalid bin");
                    }

                    // ✅ Only load result scene if not invalid bin
                    if (!isInvalidBin)
                    {
                        OCRSceneHandlerScript handler = FindFirstObjectByType<OCRSceneHandlerScript>();
                        if (handler != null)
                        {
                            Debug.Log("Calling LoadBinResultScene...");
                            handler.LoadBinResultScene();
                        }
                        else
                        {
                            Debug.LogWarning("OCRSceneHandlerScript not found in scene.");
                        }
                    }
                }
                else
                {
                    Display("No text found.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("JSON parse error: " + e.Message);
                Display("Parsing error.");
            }
        }
    }

    void Display(string message)
    {
        if (ExtractedText != null)
            ExtractedText.text = message;
        else
            Debug.Log(message);
    }
}
