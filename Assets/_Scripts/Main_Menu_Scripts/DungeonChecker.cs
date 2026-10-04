using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using System;

public class DungeonChecker : MonoBehaviour
{
    [Header("UI Text")]
    public TextMeshProUGUI requirementsText;

    [Header("Requirement Data")]
    public CardTypes requiredType;
    public int requiredAmount;
    public string sceneToLoad;

    [Header("Warning Popup")]
    public GameObject notEnough;

    [Header("Object Transparency (Optional)")]
    public GameObject objectToFade;  // ⬅ assign the image/object here
    public float fadedAlpha = 0.5f;  // 50%
    public float normalAlpha = 1f;   // 100%

    private int currentCards;
    private int ownedCards;
    private bool isActive = false;

    private bool MeetsRequirement => currentCards >= requiredAmount && ownedCards > 0;

    public enum CardTypes
    {
        Paper,
        Plastic,
        FoodWaste
    }

    void Start()
    {
        _ = DynamicPopupToast.Instance;
    }
    
    void Update()
    {
        if (!isActive || requirementsText == null) return;

        DungeonsHandler handler = FindAnyObjectByType<DungeonsHandler>();
        currentCards = requiredType switch
        {
            CardTypes.Paper => handler.paperCards,
            CardTypes.Plastic => handler.plasticCards,
            CardTypes.FoodWaste => handler.foodwasteCards,
            _ => 0,
        };

        // A reset collection must block entry even if scan stats are enough.
        ownedCards = requiredType switch
        {
            CardTypes.Paper => handler.ownedPaper,
            CardTypes.Plastic => handler.ownedPlastic,
            CardTypes.FoodWaste => handler.ownedFoodWaste,
            _ => 0,
        };

        requirementsText.text = $"{Math.Min(currentCards, requiredAmount)}/{requiredAmount} {requiredType} Cards To Enter";
        if (currentCards >= requiredAmount && ownedCards == 0)
            requirementsText.text += $"\nCollect a {requiredType} card first";

        // Apply transparency
        UpdateTransparency();
    }

    private void UpdateTransparency()
    {
        if (objectToFade == null) return;

        float alpha = MeetsRequirement ? normalAlpha : fadedAlpha;

        // Try UI Image first
        if (objectToFade.TryGetComponent<Image>(out var img))
        {
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }
        // Try SpriteRenderer
        else if (objectToFade.TryGetComponent<SpriteRenderer>(out var sprite))
        {
            Color c = sprite.color;
            c.a = alpha;
            sprite.color = c;
        }
        // Try TMP Image
        else if (objectToFade.TryGetComponent<TMPro.TMP_SpriteAsset>(out var tmp))
        {
            // TMP images are different; usually they use Image instead
            Debug.LogWarning("TMP Sprite Asset transparency not supported. Use UI Image.");
        }
    }

    // Used by DungeonSwitcher
    public void UpdateForDungeon(int index)
    {
        isActive = true;
    }

    public void Deactivate()
    {
        isActive = false;
    }

    public void RequirementCheck()
    {
        if (MeetsRequirement)
        {
            SoundManager.Instance.PlayMusic(SoundType.GAMEMUSIC);
            Transitioner.Instance.TransitionToScene(sceneToLoad);
        }
        else
        {
            DynamicPopupToast.Instance.ShowToast("Not enough cards to enter the dungeon!");
        }
    }

}
