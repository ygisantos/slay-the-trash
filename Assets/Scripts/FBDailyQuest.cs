using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Extensions;
using Firebase.Firestore;
using Sirenix.OdinInspector;
using UnityEngine;

public class FBDailyQuest : MonoBehaviour
{
    private static FBDailyQuest instance;

    public static FBDailyQuest Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<FBDailyQuest>();

                if (instance == null)
                {
                    GameObject clone = new GameObject("FBDailyQuest");
                    instance = clone.AddComponent<FBDailyQuest>();
                }
            }

            return instance;
        }
    }

    public const string COLLECTION = "daily_quest";

    // Bump this whenever CreateQuestDocument's shape changes (e.g. adding wasteType),
    // so existing documents created before the change get regenerated instead of
    // silently missing the new fields.
    private const int CURRENT_SCHEMA_VERSION = 2;

    [Header("Inspector Debug")]
    [SerializeField] private int debugQuestNumber = 1;
    [SerializeField] private int debugProgress = 1;
    [SerializeField] private bool debugCompleted;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    [Button("Debug Load Today's Progress")]
    private void DebugLoadTodaysProgress()
    {
        StartCoroutine(DebugLoadTodaysProgressRoutine());
    }

    private IEnumerator DebugLoadTodaysProgressRoutine()
    {
        yield return StartCoroutine(
            ServerTimeHelper.SyncServerTimeCoroutine()
        );

        if (!ServerTimeHelper.IsReady)
        {
            Debug.LogError("Could not synchronize server time for debug load.");
            yield break;
        }

        string username = GetDebugUsername();
        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogError("Debug daily quest load requires a logged-in username.");
            yield break;
        }

        string date = ServerTimeHelper.GetUtcNow().ToString("yyyy-MM-dd");
        LoadUserProgress(
            date,
            username,
            progress =>
            {
                Debug.Log(
                    $"Daily quest progress loaded for {username} on {date}: " +
                    FormatProgress(progress)
                );
            },
            error => Debug.LogError(error)
        );
    }

    [Button("Debug Save Quest Progress")]
    private void DebugSaveQuestProgress()
    {
        StartCoroutine(DebugSaveQuestProgressRoutine());
    }

    private IEnumerator DebugSaveQuestProgressRoutine()
    {
        if (debugQuestNumber < 1 || debugQuestNumber > 5)
        {
            Debug.LogError("Debug quest number must be between 1 and 5.");
            yield break;
        }

        yield return StartCoroutine(
            ServerTimeHelper.SyncServerTimeCoroutine()
        );

        if (!ServerTimeHelper.IsReady)
        {
            Debug.LogError("Could not synchronize server time for debug save.");
            yield break;
        }

        string username = GetDebugUsername();
        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogError("Debug daily quest save requires a logged-in username.");
            yield break;
        }

        string date = ServerTimeHelper.GetUtcNow().ToString("yyyy-MM-dd");
        Dictionary<string, object> progress =
            new Dictionary<string, object>
            {
                {
                    $"quest{debugQuestNumber}_progress",
                    Mathf.Max(0, debugProgress)
                },
                {
                    $"quest{debugQuestNumber}_completed",
                    debugCompleted
                }
            };

        SaveUserProgress(
            date,
            username,
            progress,
            () => Debug.Log(
                $"Daily quest progress saved for {username} on {date}."
            ),
            error => Debug.LogError(error)
        );
    }

    [Button("Debug Add Progress")]
    private void DebugAddProgress()
    {
        DailyQuestManager manager =
            FindFirstObjectByType<DailyQuestManager>();

        if (manager == null)
        {
            Debug.LogError("DailyQuestManager could not be found in the scene.");
            return;
        }

        manager.AddQuestProgress(
            debugQuestNumber,
            Mathf.Max(1, debugProgress)
        );
    }

    [Button("Debug Complete Quest")]
    private void DebugCompleteQuest()
    {
        DailyQuestManager manager =
            FindFirstObjectByType<DailyQuestManager>();

        if (manager == null)
        {
            Debug.LogError("DailyQuestManager could not be found in the scene.");
            return;
        }

        manager.CompleteQuestForDebug(debugQuestNumber);
    }

    private IEnumerator DebugChangeProgressRoutine(bool complete)
    {
        if (debugQuestNumber < 1 || debugQuestNumber > 5)
        {
            Debug.LogError("Debug quest number must be between 1 and 5.");
            yield break;
        }

        yield return StartCoroutine(
            ServerTimeHelper.SyncServerTimeCoroutine()
        );

        if (!ServerTimeHelper.IsReady)
        {
            Debug.LogError("Could not synchronize server time for debug action.");
            yield break;
        }

        string username = GetDebugUsername();
        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogError("Debug daily quest action requires a logged-in username.");
            yield break;
        }

        string date = ServerTimeHelper.GetUtcNow().ToString("yyyy-MM-dd");
        Dictionary<string, object> updates =
            new Dictionary<string, object>();

        if (complete)
        {
            updates[$"quest{debugQuestNumber}_completed"] = true;
        }
        else
        {
            updates[$"quest{debugQuestNumber}_progress"] =
                Mathf.Max(0, debugProgress);
        }

        SaveUserProgress(
            date,
            username,
            updates,
            () => Debug.Log(
                $"Debug {(complete ? "completion" : "progress")} saved " +
                $"for quest {debugQuestNumber}."
            ),
            error => Debug.LogError(error)
        );
    }

    public void EnsureDailyQuest(
        string date,
        List<QuestDefinition> quests,
        Action<Dictionary<string, object>> onSuccess,
        Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(date) || quests == null || quests.Count < 5)
        {
            onError?.Invoke("Five daily quest definitions are required.");
            return;
        }

        WaitForFirebase(
            () =>
            {
                DocumentReference document =
                    FirebaseManager.Instance.DB
                        .Collection(COLLECTION)
                        .Document(date);

                document.GetSnapshotAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        onError?.Invoke(GetTaskError(task, "Could not read daily quests."));
                        return;
                    }

                    if (task.Result.Exists)
                    {
                        Dictionary<string, object> existingData = task.Result.ToDictionary();
                        int existingVersion = GetSchemaVersion(existingData);

                        if (existingVersion >= CURRENT_SCHEMA_VERSION)
                        {
                            onSuccess?.Invoke(existingData);
                            return;
                        }
                    }

                    Dictionary<string, object> data =
                        CreateQuestDocument(date, quests);

                    document.SetAsync(data).ContinueWithOnMainThread(createTask =>
                    {
                        if (createTask.IsFaulted || createTask.IsCanceled)
                        {
                            onError?.Invoke(
                                GetTaskError(createTask, "Could not create daily quests.")
                            );
                            return;
                        }

                        onSuccess?.Invoke(data);
                    });
                });
            },
            onError
        );
    }

    public void LoadUserProgress(
        string date,
        string username,
        Action<Dictionary<string, object>> onSuccess,
        Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(date) || string.IsNullOrWhiteSpace(username))
        {
            onError?.Invoke("A date and username are required for daily quest progress.");
            return;
        }

        WaitForFirebase(
            () =>
            {
                UserDocument(date, username).GetSnapshotAsync()
                    .ContinueWithOnMainThread(task =>
                    {
                        if (task.IsFaulted || task.IsCanceled)
                        {
                            onError?.Invoke(
                                GetTaskError(task, "Could not load daily quest progress.")
                            );
                            return;
                        }

                        if (task.Result.Exists)
                        {
                            onSuccess?.Invoke(task.Result.ToDictionary());
                            return;
                        }

                        Dictionary<string, object> defaults =
                            CreateDefaultProgress(username);

                        UserDocument(date, username).SetAsync(defaults)
                            .ContinueWithOnMainThread(createTask =>
                            {
                                if (createTask.IsFaulted || createTask.IsCanceled)
                                {
                                    onError?.Invoke(
                                        GetTaskError(
                                            createTask,
                                            "Could not create daily quest progress."
                                        )
                                    );
                                    return;
                                }

                                onSuccess?.Invoke(defaults);
                            });
                    });
            },
            onError
        );
    }

    public void SaveUserProgress(
        string date,
        string username,
        Dictionary<string, object> progress,
        Action onSuccess = null,
        Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(date) || string.IsNullOrWhiteSpace(username))
        {
            onError?.Invoke("A date and username are required for daily quest progress.");
            return;
        }

        if (progress == null || progress.Count == 0)
        {
            onError?.Invoke("Daily quest progress cannot be empty.");
            return;
        }

        WaitForFirebase(
            () =>
            {
                UserDocument(date, username).SetAsync(
                    progress,
                    SetOptions.MergeAll
                ).ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        onError?.Invoke(
                            GetTaskError(task, "Could not save daily quest progress.")
                        );
                        return;
                    }

                    onSuccess?.Invoke();
                });
            },
            onError
        );
    }

    public static Dictionary<string, object> CreateDefaultProgress(string username)
    {
        Dictionary<string, object> progress =
            new Dictionary<string, object>
            {
                { "username", username },
                { "quest1_progress", 0 },
                { "quest2_progress", 0 },
                { "quest3_progress", 0 },
                { "quest4_progress", 0 },
                { "quest5_progress", 0 },
                { "quest1_completed", false },
                { "quest2_completed", false },
                { "quest3_completed", false },
                { "quest4_completed", false },
                { "quest5_completed", false }
                ,
                { "quest1_rewarded", false },
                { "quest2_rewarded", false },
                { "quest3_rewarded", false },
                { "quest4_rewarded", false },
                { "quest5_rewarded", false }
            };

        return progress;
    }

    private static Dictionary<string, object> CreateQuestDocument(
        string date,
        List<QuestDefinition> quests)
    {
        Dictionary<string, object> data =
            new Dictionary<string, object>
            {
                { "date", date },
                { "schemaVersion", CURRENT_SCHEMA_VERSION }
            };

        for (int index = 0; index < 5; index++)
        {
            QuestDefinition quest = quests[index];
            data[$"quest{index + 1}"] = new Dictionary<string, object>
            {
                { "title", quest.title },
                { "minAmount", quest.minAmount },
                { "maxAmount", quest.maxAmount },
                { "amount", quest.targetAmount },
                { "wasteType", quest.wasteType ?? "" }
            };
        }

        return data;
    }

    private static int GetSchemaVersion(Dictionary<string, object> data)
    {
        if (data == null || !data.TryGetValue("schemaVersion", out object value))
            return 0;

        return value switch
        {
            long longValue => (int)longValue,
            int intValue => intValue,
            _ => 0
        };
    }

    private DocumentReference UserDocument(string date, string username)
    {
        return FirebaseManager.Instance.DB
            .Collection(COLLECTION)
            .Document(date)
            .Collection("users")
            .Document(username);
    }

    private void WaitForFirebase(Action operation, Action<string> onError)
    {
        StartCoroutine(WaitForFirebaseRoutine(operation, onError));
    }

    private IEnumerator WaitForFirebaseRoutine(
        Action operation,
        Action<string> onError)
    {
        float timeout = 15f;
        while ((FirebaseManager.Instance == null ||
                !FirebaseManager.Instance.IsInitialized) &&
               timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (FirebaseManager.Instance != null &&
            FirebaseManager.Instance.IsInitialized)
        {
            operation?.Invoke();
            yield break;
        }

        onError?.Invoke("Firebase Firestore initialization timed out.");
    }

    private static string GetTaskError(
        System.Threading.Tasks.Task task,
        string fallback)
    {
        if (task.Exception == null)
            return fallback;

        return task.Exception.Flatten().InnerException?.Message
            ?? task.Exception.Message
            ?? fallback;
    }

    private static string GetDebugUsername()
    {
        Dictionary<string, object> profile =
            FBAuthentication.Instance != null
                ? FBAuthentication.Instance.CurrentProfile
                : null;

        if (profile == null && DataManager.Instance != null)
            profile = DataManager.Instance.GetProfile();

        return FirebaseDataHelper.GetString(profile, "username");
    }

    private static string FormatProgress(Dictionary<string, object> progress)
    {
        if (progress == null || progress.Count == 0)
            return "No progress document fields found.";

        List<string> values = new List<string>();
        for (int index = 1; index <= 5; index++)
        {
            string progressKey = $"quest{index}_progress";
            string completedKey = $"quest{index}_completed";
            string value = FirebaseDataHelper.GetString(progress, progressKey);
            string completed = FirebaseDataHelper.GetString(progress, completedKey);
            values.Add($"quest{index}={value ?? "0"}, completed={completed ?? "false"}");
        }

        return string.Join(" | ", values);
    }
}
