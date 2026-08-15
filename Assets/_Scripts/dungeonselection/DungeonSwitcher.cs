using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class DungeonSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class DungeonInfo
    {
        public string dungeonName;
        [TextArea] public string enemiesDescription;
    }

    [Header("Dungeon GameObjects (5 Total)")]
    public GameObject dungeon1;
    public GameObject dungeon2;
    public GameObject dungeon3;
    public GameObject dungeon4;   // ✅ NEW
    public GameObject dungeon5;   // ✅ NEW

    private GameObject[] dungeons;
    private int currentIndex = 0;

    [Header("Dungeon Info (5 Total)")]
    public DungeonInfo[] dungeonInfos;

    [Header("UI References")]
    public TextMeshProUGUI dungeonNameText;
    public TextMeshProUGUI enemiesDescriptionText;

    [Header("Arrows")]
    public Image leftArrow;
    public Image rightArrow;

    [Header("Popup Animation Settings")]
    public float popScale = 1.15f;
    public float popSpeed = 0.12f;

    [Header("Dungeon Checker References (5 Total)")]
    public DungeonChecker dungeonChecker1;
    public DungeonChecker dungeonChecker2;
    public DungeonChecker dungeonChecker3;
    public DungeonChecker dungeonChecker4;   // ✅ NEW
    public DungeonChecker dungeonChecker5;   // ✅ NEW

    private DungeonChecker[] dungeonCheckers;

    void Start()
    {
        // ✅ Now includes 5 dungeons
        dungeons = new GameObject[]
        {
            dungeon1,
            dungeon2,
            dungeon3,
            dungeon4,
            dungeon5
        };

        // ✅ Now includes 5 checkers
        dungeonCheckers = new DungeonChecker[]
        {
            dungeonChecker1,
            dungeonChecker2,
            dungeonChecker3,
            dungeonChecker4,
            dungeonChecker5
        };

        ShowDungeon(0, false);
    }

    public void NextDungeon()
    {
        if (currentIndex < dungeons.Length - 1)
        {
            currentIndex++;
            SoundManager.PlaySound(SoundType.CLICK);
            ShowDungeon(currentIndex, true);
        }
    }

    public void PreviousDungeon()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            SoundManager.PlaySound(SoundType.CLICK);
            ShowDungeon(currentIndex, true);
        }
    }

    private void ShowDungeon(int index, bool animate)
    {
        // Activate correct dungeon
        for (int i = 0; i < dungeons.Length; i++)
            dungeons[i].SetActive(i == index);

        // Popup animation
        if (animate)
            StartCoroutine(PopAnim(dungeons[index].transform));

        // Update name
        if (dungeonNameText != null)
            dungeonNameText.text = dungeonInfos[index].dungeonName;

        // Update enemies description
        if (enemiesDescriptionText != null)
            enemiesDescriptionText.text = dungeonInfos[index].enemiesDescription;

        UpdateArrows();

        // Switch checker
        for (int i = 0; i < dungeonCheckers.Length; i++)
        {
            if (dungeonCheckers[i] != null)
            {
                if (i == index)
                    dungeonCheckers[i].UpdateForDungeon(index);
                else
                    dungeonCheckers[i].Deactivate();
            }
        }
    }

    private void UpdateArrows()
    {
        leftArrow.gameObject.SetActive(currentIndex > 0);
        rightArrow.gameObject.SetActive(currentIndex < dungeons.Length - 1);
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
