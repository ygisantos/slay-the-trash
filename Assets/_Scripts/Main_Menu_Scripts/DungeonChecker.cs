using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using UnityEngine.UI;

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
    private bool isActive = false;

    public enum CardTypes
    {
        Paper,
        Plastic,
        FoodWaste
    }

    void Update()
    {
        if (!isActive || requirementsText == null) return;

        // Get card amount
        currentCards = requiredType switch
        {
            CardTypes.Paper => FindAnyObjectByType<DungeonsHandler>().paperCards,
            CardTypes.Plastic => FindAnyObjectByType<DungeonsHandler>().plasticCards,
            CardTypes.FoodWaste => FindAnyObjectByType<DungeonsHandler>().foodwasteCards,
            _ => 0,
        };

        // Update UI text
        requirementsText.text = $"{currentCards}/{requiredAmount} {requiredType} Cards\nTo Enter";

        // Apply transparency
        UpdateTransparency();
    }

    private void UpdateTransparency()
    {
        if (objectToFade == null) return;

        float alpha = currentCards >= requiredAmount ? normalAlpha : fadedAlpha;

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
        if (currentCards >= requiredAmount)
        {
            SoundManager.PlayMusic(SoundType.GAMEMUSIC);
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            StartCoroutine(NotEnoughCards());
        }
    }

    private IEnumerator NotEnoughCards()
    {
        if (notEnough != null)
        {
            notEnough.SetActive(true);
            yield return new WaitForSeconds(1f);
            notEnough.SetActive(false);
        }
        else
            Debug.LogWarning("Missing Warning Game Object Reference!");
    }
}
