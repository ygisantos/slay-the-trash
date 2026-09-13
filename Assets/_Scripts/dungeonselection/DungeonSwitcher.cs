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
    public Color enabledArrowColor = Color.white;
    public Color disabledArrowColor = new Color(1f, 1f, 1f, 0.35f);

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
    private bool isSwitching;
    private const string SelectedDungeonKey = "selected_dungeon_index";

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

        currentIndex = Mathf.Clamp(
            PlayerPrefs.GetInt(SelectedDungeonKey, 0),
            0,
            dungeons.Length - 1
        );

        ShowDungeon(currentIndex, false);
    }

    public void NextDungeon()
    {
        if (isSwitching)
            return;

        if (currentIndex < dungeons.Length - 1)
        {
            currentIndex++;
            SaveSelectedDungeon();
            SoundManager.PlaySound(SoundType.CLICK);
            ShowDungeon(currentIndex, true);
        }
    }

    public void PreviousDungeon()
    {
        if (isSwitching)
            return;

        if (currentIndex > 0)
        {
            currentIndex--;
            SaveSelectedDungeon();
            SoundManager.PlaySound(SoundType.CLICK);
            ShowDungeon(currentIndex, true);
        }
    }

    private void ShowDungeon(int index, bool animate)
    {
        isSwitching = animate;

        // Activate correct dungeon
        for (int i = 0; i < dungeons.Length; i++)
            dungeons[i].SetActive(i == index);

        if (DungeonBackgroundManager.Instance != null)
        {
            DungeonBackgroundManager.Instance.ShowBackground(
                index,
                animate,
                UnlockSwitch
            );
        }
        else
        {
            UnlockSwitch();
        }

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
        SetArrowState(
            leftArrow,
            currentIndex > 0 && !isSwitching
        );
        SetArrowState(
            rightArrow,
            currentIndex < dungeons.Length - 1 && !isSwitching
        );
    }

    private void SetArrowState(Image arrow, bool enabled)
    {
        if (arrow == null)
            return;

        arrow.color = enabled
            ? enabledArrowColor
            : disabledArrowColor;

        Button button = arrow.GetComponent<Button>();
        if (button == null)
            button = arrow.GetComponentInParent<Button>();

        if (button != null)
        {
            button.interactable = enabled;
            arrow.raycastTarget = true;
        }
        else
        {
            arrow.raycastTarget = enabled;
        }
    }

    private void SaveSelectedDungeon()
    {
        PlayerPrefs.SetInt(SelectedDungeonKey, currentIndex);
        PlayerPrefs.Save();
    }

    private void UnlockSwitch()
    {
        isSwitching = false;
        UpdateArrows();
    }

    private IEnumerator PopAnim(Transform t)
    {
        Vector3 original = t.localScale;
        Vector3 bigger = original * popScale;
        float time = 0f;

        while (time < popSpeed)
        {
            time += Time.deltaTime;
            float progress = Mathf.Clamp01(time / popSpeed);
            t.localScale = Vector3.Lerp(
                original,
                bigger,
                Mathf.SmoothStep(0f, 1f, progress)
            );
            yield return null;
        }

        time = 0f;
        while (time < popSpeed)
        {
            time += Time.deltaTime;
            float progress = Mathf.Clamp01(time / popSpeed);
            t.localScale = Vector3.Lerp(
                bigger,
                original,
                Mathf.SmoothStep(0f, 1f, progress)
            );
            yield return null;
        }
    }
}
