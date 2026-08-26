using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

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

    private DateTime serverTimeAtSync;
    private float realtimeAtSync;

    private const string TimeApi =
        "https://timeapi.io/api/Time/current/zone?timeZone=UTC";

    private void Start()
    {
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        yield return StartCoroutine(SyncServerTime());

        if (serverTimeAtSync == default)
        {
            Debug.LogError(
                "Daily Quest Manager could not synchronize server time."
            );

            yield break;
        }

        GenerateDailyQuests();

        StartCoroutine(CountdownRoutine());
    }

    // =========================================================
    // SERVER TIME
    // =========================================================

    [Serializable]
    private class TimeApiResponse
    {
        public string dateTime;
    }

    private IEnumerator SyncServerTime()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(TimeApi))
        {
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    $"Failed to get server time: {request.error}"
                );

                yield break;
            }

            string json = request.downloadHandler.text;

            try
            {
                TimeApiResponse response =
                    JsonUtility.FromJson<TimeApiResponse>(json);

                if (response == null ||
                    string.IsNullOrEmpty(response.dateTime))
                {
                    Debug.LogError(
                        "Server returned an invalid time response."
                    );

                    yield break;
                }

                serverTimeAtSync = DateTime.Parse(
                    response.dateTime,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind
                ).ToUniversalTime();

                realtimeAtSync =
                    Time.realtimeSinceStartup;

                Debug.Log(
                    $"Server UTC Time: " +
                    $"{serverTimeAtSync:yyyy-MM-dd HH:mm:ss}"
                );
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"Failed to parse server time: {e.Message}"
                );
            }
        }
    }

    private DateTime GetServerTime()
    {
        float elapsed =
            Time.realtimeSinceStartup - realtimeAtSync;

        return serverTimeAtSync.AddSeconds(elapsed);
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

        DateTime serverTime = GetServerTime();

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
                GetServerTime();

            DateTime nextReset =
                now.Date.AddDays(1);

            TimeSpan remaining =
                nextReset - now;

            if (remaining.TotalSeconds <= 0)
            {
                // Synchronize again so the new date
                // is definitely based on server time.
                yield return StartCoroutine(
                    SyncServerTime()
                );

                if (serverTimeAtSync != default)
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