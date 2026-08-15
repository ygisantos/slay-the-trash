using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DungeonSelector : MonoBehaviour
{
    [System.Serializable]
    public class DungeonInfo
    {
        public string dungeonName;
        [TextArea] public string enemiesDescription;
        [TextArea] public string requirements;
    }

    [Header("Dungeon GameObjects")]
    public GameObject dungeon1;
    public GameObject dungeon2;
    public GameObject dungeon3;

    [Header("Dungeon Info (Editable in Inspector)")]
    public DungeonInfo[] dungeonInfos;

    [Header("UI Text Fields")]
    public TextMeshProUGUI dungeonNameText;
    public TextMeshProUGUI enemiesDescriptionText;
    public TextMeshProUGUI requirementsText;

    [Header("Arrows")]
    public Image leftArrow;
    public Image rightArrow;

    [Header("Animation Settings")]
    public float popScale = 1.15f;
    public float popSpeed = 0.12f;

    private int currentIndex = 0;
    private GameObject[] dungeons;

    void Start()
    {
        // put all dungeons into an array
        dungeons = new GameObject[] { dungeon1, dungeon2, dungeon3 };

        // show the first dungeon WITHOUT animation
        ShowDungeon(0, false);
    }

    public void NextDungeon()
    {
        if (currentIndex < dungeons.Length - 1)
        {
            currentIndex++;
            ShowDungeon(currentIndex, true);
        }
    }

    public void PreviousDungeon()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            ShowDungeon(currentIndex, true);
        }
    }

    private void ShowDungeon(int index, bool animate)
    {
        // activate only the selected dungeon
        for (int i = 0; i < dungeons.Length; i++)
        {
            dungeons[i].SetActive(i == index);
        }

        // play animation only if animate is true
        if (animate)
        {
            StartCoroutine(PopAnim(dungeons[index].transform));
        }

        // update arrows visibility
        UpdateArrows();

        // show dungeon info in UI
        ShowDungeonInfo(index);
    }

    private void UpdateArrows()
    {
        leftArrow.gameObject.SetActive(currentIndex > 0);
        rightArrow.gameObject.SetActive(currentIndex < dungeons.Length - 1);
    }

    private void ShowDungeonInfo(int index)
    {
        if (index < 0 || index >= dungeonInfos.Length) return;

        dungeonNameText.text = dungeonInfos[index].dungeonName;
        enemiesDescriptionText.text = dungeonInfos[index].enemiesDescription;
        requirementsText.text = dungeonInfos[index].requirements;
    }

    private IEnumerator PopAnim(Transform t)
    {
        Vector3 original = t.localScale;
        Vector3 bigger = original * popScale;

        float time = 0f;
        while (time < popSpeed)
        {
            time += Time.deltaTime;
            t.localScale = Vector3.Lerp(original, bigger, time / popSpeed);
            yield return null;
        }

        time = 0f;
        while (time < popSpeed)
        {
            time += Time.deltaTime;
            t.localScale = Vector3.Lerp(bigger, original, time / popSpeed);
            yield return null;
        }
    }
}
