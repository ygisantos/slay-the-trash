using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class QuestDefinition
{
    public string title;
    public int minAmount;
    public int maxAmount;
    [HideInInspector] public int targetAmount;
}

public class DailyQuestManager : MonoBehaviour
{
    [Header("Quest UI Items")]
    [SerializeField] private List<GameObject> questItems = new();

    [Header("Possible Quests")]
    [SerializeField] private List<QuestDefinition> possibleQuests = new();

    [Header("Settings")]
    [SerializeField] private int seed = 12345;

    [Header("Countdown")]
    [SerializeField] private TMP_Text countdownText;

    [Header("Quest Status Colors")]
    [SerializeField] private Color incompleteQuestColor = Color.white;
    [SerializeField] private Color completedQuestColor = Color.green;

    private readonly int[] questTargets = new int[5];
    private readonly int[] questProgress = new int[5];
    private readonly bool[] questCompleted = new bool[5];
    private readonly bool[] questRewarded = new bool[5];
    private readonly bool[] rewardInProgress = new bool[5];
    private string currentDate;
    private string currentUsername;

    private void Start()
    {
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        yield return StartCoroutine(
            ServerTimeHelper.SyncServerTimeCoroutine(
                success =>
                {
                    if (!success)
                    {
                        Debug.LogError(
                            "Daily Quest Manager could not synchronize server time."
                        );
                    }
                }
            )
        );

        if (!ServerTimeHelper.IsReady)
            yield break;

        GenerateDailyQuests();
        StartCoroutine(CountdownRoutine());
    }

    private void GenerateDailyQuests()
    {
        if (questItems.Count != 5)
        {
            Debug.LogWarning(
                $"Daily quests are designed for 5 UI items. Currently assigned: {questItems.Count}"
            );
        }

        if (questItems.Count < 5 || possibleQuests.Count < questItems.Count)
        {
            Debug.LogError(
                $"You need 5 UI items and at least {questItems.Count} unique quests."
            );
            return;
        }

        DateTime serverTime = ServerTimeHelper.GetUtcNow();
        string date = serverTime.ToString("yyyy-MM-dd");
        int dailySeed = CreateDailySeed(date);
        System.Random random = new System.Random(dailySeed);
        List<QuestDefinition> availableQuests =
            new List<QuestDefinition>(possibleQuests);
        List<QuestDefinition> generatedQuests =
            new List<QuestDefinition>();

        for (int index = 0; index < 5; index++)
        {
            int randomIndex = random.Next(availableQuests.Count);
            QuestDefinition selectedQuest = availableQuests[randomIndex];
            availableQuests.RemoveAt(randomIndex);

            int amount = random.Next(
                selectedQuest.minAmount,
                selectedQuest.maxAmount + 1
            );

            generatedQuests.Add(
                new QuestDefinition
                {
                    title = selectedQuest.title,
                    minAmount = selectedQuest.minAmount,
                    maxAmount = selectedQuest.maxAmount,
                    targetAmount = amount
                }
            );
        }

        currentDate = date;
        currentUsername = GetCurrentUsername();
        if (string.IsNullOrWhiteSpace(currentUsername))
        {
            Debug.LogError("Cannot load daily quests without a username.");
            return;
        }

        FBDailyQuest.Instance.EnsureDailyQuest(
            date,
            generatedQuests,
            ApplyQuestDocument,
            error => Debug.LogError(error)
        );
    }

    private void ApplyQuestDocument(Dictionary<string, object> document)
    {
        for (int index = 0; index < 5; index++)
        {
            if (!document.TryGetValue(
                    $"quest{index + 1}",
                    out object questValue
                ))
            {
                continue;
            }

            Dictionary<string, object> quest =
                questValue as Dictionary<string, object>;
            if (quest == null)
                continue;

            string title = FirebaseDataHelper.GetString(quest, "title");
            int amount = GetInt(quest, "amount");
            if (string.IsNullOrWhiteSpace(title) || amount < 0)
                continue;

            questTargets[index] = amount;
            SetQuestUI(
                questItems[index],
                title,
                amount,
                questProgress[index]
            );
        }

        FBDailyQuest.Instance.LoadUserProgress(
            currentDate,
            currentUsername,
            ApplyProgress,
            error => Debug.LogError(error)
        );
    }

    private void ApplyProgress(Dictionary<string, object> progress)
    {
        for (int index = 0; index < 5; index++)
        {
            int questNumber = index + 1;
            questProgress[index] = Mathf.Max(
                0,
                GetInt(progress, $"quest{questNumber}_progress")
            );
            questCompleted[index] = GetBool(
                progress,
                $"quest{questNumber}_completed"
            );
            questRewarded[index] = GetBool(
                progress,
                $"quest{questNumber}_rewarded"
            );

            if (questProgress[index] >= questTargets[index])
            {
                questProgress[index] = questTargets[index];
                questCompleted[index] = true;
            }

            SetQuestProgressUI(index);

            if (questCompleted[index] && !questRewarded[index])
                CompleteQuest(index);
        }
    }

    public void AddQuestProgress(int questNumber, int amount = 1)
    {
        int index = questNumber - 1;
        if (index < 0 || index >= 5)
        {
            Debug.LogError("Quest number must be between 1 and 5.");
            return;
        }

        if (amount <= 0)
            return;

        if (string.IsNullOrWhiteSpace(currentDate) ||
            string.IsNullOrWhiteSpace(currentUsername))
        {
            Debug.LogError("Daily quest data has not finished loading.");
            return;
        }

        if (questCompleted[index])
            return;

        questProgress[index] = Mathf.Min(
            questTargets[index],
            questProgress[index] + amount
        );
        questCompleted[index] =
            questProgress[index] >= questTargets[index];
        SetQuestProgressUI(index);

        if (questCompleted[index])
            CompleteQuest(index);
        else
            SaveQuestState(index);
    }

    private void SaveQuestState(int index)
    {
        int questNumber = index + 1;
        FBDailyQuest.Instance.SaveUserProgress(
            currentDate,
            currentUsername,
            new Dictionary<string, object>
            {
                { $"quest{questNumber}_progress", questProgress[index] },
                { $"quest{questNumber}_completed", questCompleted[index] },
                { $"quest{questNumber}_rewarded", questRewarded[index] }
            },
            null,
            error => Debug.LogError(error)
        );
    }

    private void CompleteQuest(int index)
    {
        if (index < 0 || index >= 5 ||
            questRewarded[index] || rewardInProgress[index])
        {
            return;
        }

        rewardInProgress[index] = true;
        SaveQuestState(index);

        if (FBAuthentication.Instance == null)
        {
            rewardInProgress[index] = false;
            Debug.LogError("Cannot reward quest without authentication.");
            return;
        }

        FBAuthentication.Instance.AddDailyQuestReward(
            () =>
            {
                questRewarded[index] = true;
                rewardInProgress[index] = false;
                SaveQuestState(index);

                if (DynamicPopupToast.Instance != null)
                {
                    DynamicPopupToast.Instance.ShowToast(
                        "Quest completed! +1 water"
                    );
                }
            },
            error =>
            {
                rewardInProgress[index] = false;
                Debug.LogError(error);
            }
        );
    }

    public void UpdateQuestProgress(int questNumber, int amount = 1)
    {
        AddQuestProgress(questNumber, amount);
    }

    public void CompleteQuestForDebug(int questNumber)
    {
        int index = questNumber - 1;
        if (index < 0 || index >= 5)
        {
            Debug.LogError("Quest number must be between 1 and 5.");
            return;
        }

        questProgress[index] = questTargets[index];
        questCompleted[index] = true;
        SetQuestProgressUI(index);
        CompleteQuest(index);
    }

    private void SetQuestUI(
        GameObject item,
        string title,
        int amount,
        int progress)
    {
        Transform titleTransform = item.transform.Find("name/Text");
        Transform amountTransform = item.transform.Find("status/Text");

        if (titleTransform == null || amountTransform == null)
        {
            Debug.LogError($"Quest UI structure is incomplete: {item.name}");
            return;
        }

        TMP_Text titleText = titleTransform.GetComponent<TMP_Text>();
        TMP_Text amountText = amountTransform.GetComponent<TMP_Text>();
        if (titleText == null || amountText == null)
        {
            Debug.LogError($"Quest UI text is missing: {item.name}");
            return;
        }

        titleText.text = title;
        amountText.text = $"{progress} / {amount}";
        SetQuestStatusColor(
            questItems.IndexOf(item),
            progress >= amount
        );
    }

    private void SetQuestProgressUI(int index)
    {
        if (index < 0 || index >= questItems.Count)
            return;

        Transform amountTransform =
            questItems[index].transform.Find("status/Text");
        TMP_Text amountText = amountTransform != null
            ? amountTransform.GetComponent<TMP_Text>()
            : null;

        if (amountText != null)
            amountText.text = $"{questProgress[index]} / {questTargets[index]}";

        SetQuestStatusColor(index, questCompleted[index]);
    }

    private void SetQuestStatusColor(int index, bool completed)
    {
        if (index < 0 || index >= questItems.Count)
            return;

        Transform statusTransform =
            questItems[index].transform.Find("status");
        Image statusImage = statusTransform != null
            ? statusTransform.GetComponent<Image>()
            : null;

        if (statusImage == null)
        {
            Debug.LogWarning(
                $"Status Image not found on quest item {index + 1}."
            );
            return;
        }

        statusImage.color = completed
            ? completedQuestColor
            : incompleteQuestColor;
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

    private int CreateDailySeed(string date)
    {
        int hash = 17;
        foreach (char character in date)
            hash = hash * 31 + character;

        return seed ^ hash;
    }

    private IEnumerator CountdownRoutine()
    {
        while (true)
        {
            DateTime now = ServerTimeHelper.GetUtcNow();
            DateTime nextReset = now.Date.AddDays(1);
            TimeSpan remaining = nextReset - now;

            if (remaining.TotalSeconds <= 0)
            {
                yield return StartCoroutine(
                    ServerTimeHelper.SyncServerTimeCoroutine()
                );

                if (ServerTimeHelper.IsReady)
                    GenerateDailyQuests();

                continue;
            }

            if (countdownText != null)
            {
                countdownText.text =
                    $"{(int)remaining.TotalHours:00}:" +
                    $"{remaining.Minutes:00}:" +
                    $"{remaining.Seconds:00}";
            }

            yield return new WaitForSecondsRealtime(1f);
        }
    }

    private static int GetInt(
        Dictionary<string, object> data,
        string key)
    {
        if (data == null || !data.TryGetValue(key, out object value))
            return -1;

        if (value is int intValue)
            return intValue;

        return int.TryParse(value?.ToString(), out int result)
            ? result
            : -1;
    }

    private static bool GetBool(
        Dictionary<string, object> data,
        string key)
    {
        if (data == null || !data.TryGetValue(key, out object value))
            return false;

        if (value is bool boolValue)
            return boolValue;

        return bool.TryParse(value?.ToString(), out bool result) && result;
    }
}
