using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class QuestDefinition
{
    public string title;
    public int minAmount;
    public int maxAmount;
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
        {
            yield break;
        }

        GenerateDailyQuests();

        StartCoroutine(CountdownRoutine());
    }

    // =========================================================
    // DAILY QUEST GENERATION
    // =========================================================

    private void GenerateDailyQuests()
    {
        if (questItems.Count != 5)
        {
            Debug.LogWarning(
                $"Daily quests are designed for 5 UI items. " +
                $"Currently assigned: {questItems.Count}"
            );
        }

        if (possibleQuests.Count < questItems.Count)
        {
            Debug.LogError(
                $"You need at least {questItems.Count} " +
                $"unique quests, but only have " +
                $"{possibleQuests.Count}."
            );

            return;
        }

        DateTime serverTime = ServerTimeHelper.GetUtcNow();

        string date =
            serverTime.ToString("yyyy-MM-dd");

        int dailySeed =
            CreateDailySeed(date);

        System.Random random =
            new System.Random(dailySeed);

        // Copy the possible quests into a temporary pool.
        List<QuestDefinition> availableQuests =
            new List<QuestDefinition>(possibleQuests);

        // Generate one unique quest per UI item.
        for (int i = 0; i < questItems.Count; i++)
        {
            int randomIndex =
                random.Next(availableQuests.Count);

            QuestDefinition selectedQuest =
                availableQuests[randomIndex];

            // Remove the quest from the pool.
            // This guarantees it cannot appear again today.
            availableQuests.RemoveAt(randomIndex);

            int amount =
                random.Next(
                    selectedQuest.minAmount,
                    selectedQuest.maxAmount + 1
                );

            SetQuestUI(
                questItems[i],
                selectedQuest.title,
                amount
            );
        }

        Debug.Log(
            $"Generated {questItems.Count} unique quests " +
            $"for {date}"
        );
    }

    // =========================================================
    // UI
    // =========================================================

    private void SetQuestUI(
        GameObject item,
        string title,
        int amount)
    {
        Transform titleTransform =
            item.transform.Find("name/Text");

        Transform amountTransform =
            item.transform.Find("status/Text");

        if (titleTransform == null)
        {
            Debug.LogError(
                $"Title not found in {item.name}"
            );

            return;
        }

        if (amountTransform == null)
        {
            Debug.LogError(
                $"Amount not found in {item.name}"
            );

            return;
        }

        TMP_Text titleText =
            titleTransform.GetComponent<TMP_Text>();

        TMP_Text amountText =
            amountTransform.GetComponent<TMP_Text>();

        if (titleText == null)
        {
            Debug.LogError(
                $"Title does not contain TMP_Text: {item.name}"
            );

            return;
        }

        if (amountText == null)
        {
            Debug.LogError(
                $"Amount does not contain TMP_Text: {item.name}"
            );

            return;
        }

        titleText.text = title;
        amountText.text = "0 / " + amount.ToString();
    }

    // =========================================================
    // DETERMINISTIC DAILY SEED
    // =========================================================

    private int CreateDailySeed(string date)
    {
        int hash = 17;

        foreach (char c in date)
        {
            hash = hash * 31 + c;
        }

        return seed ^ hash;
    }

    // =========================================================
    // COUNTDOWN
    // =========================================================

    private IEnumerator CountdownRoutine()
    {
        while (true)
        {
            DateTime now =
                ServerTimeHelper.GetUtcNow();

            DateTime nextReset =
                now.Date.AddDays(1);

            TimeSpan remaining =
                nextReset - now;

            if (remaining.TotalSeconds <= 0)
            {
                // Synchronize again so the new date
                // is definitely based on server time.
                yield return StartCoroutine(
                    ServerTimeHelper.SyncServerTimeCoroutine()
                );

                if (ServerTimeHelper.IsReady)
                {
                    GenerateDailyQuests();
                }

                continue;
            }

            countdownText.text =
                $"{(int)remaining.TotalHours:00}:" +
                $"{remaining.Minutes:00}:" +
                $"{remaining.Seconds:00}";

            yield return new WaitForSecondsRealtime(1f);
        }
    }
}