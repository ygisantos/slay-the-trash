using System;
using System.Collections.Generic;
using System.Linq;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

public class FBLeaderboard : MonoBehaviour
{
    private static FBLeaderboard instance;

    public static FBLeaderboard Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<FBLeaderboard>();

                if (instance == null)
                {
                    GameObject singletonPrefab =
                        Resources.Load<GameObject>("LeaderboardManager");

                    if (singletonPrefab == null)
                    {
                        Debug.LogError(
                            "FBLeaderboard prefab not found in Resources folder."
                        );

                        return null;
                    }

                    GameObject clone = Instantiate(singletonPrefab);
                    instance = clone.GetComponent<FBLeaderboard>();
                }
            }

            return instance;
        }
    }

    public const string POINTS_COLLECTION = "Points";

    private static readonly string[] PointFields =
    {
        "total_trash",
        "recicled_trash",
        "non_bio_trash",
        "bio_trash",
        "dailies",
        "dun1",
        "dun2",
        "dun3",
        "dun4",
        "dun5"
    };

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

    public static Dictionary<string, object> CreateDefaultPoints(
        string userId,
        string username)
    {
        Dictionary<string, object> points =
            new Dictionary<string, object>
            {
                { "userId", userId },
                { "username", username }
            };

        foreach (string field in PointFields)
            points[field] = 0;

        return points;
    }

    public void Create(
        string userId,
        string username,
        Action onSuccess = null,
        Action<string> onError = null)
    {
        FirebaseManager.Instance.CreateDocument(
            POINTS_COLLECTION,
            username,
            CreateDefaultPoints(userId, username),
            id => onSuccess?.Invoke(),
            onError
        );
    }

    public void Read(
        string username,
        Action<Dictionary<string, object>> onSuccess,
        Action<string> onError = null)
    {
        FirebaseManager.Instance.GetDocument(
            POINTS_COLLECTION,
            username,
            snapshot =>
            {
                if (!snapshot.Exists)
                {
                    onError?.Invoke("Points document does not exist.");
                    return;
                }

                onSuccess?.Invoke(snapshot.ToDictionary());
            },
            onError
        );
    }

    public void UpdatePoints(
        string username,
        Dictionary<string, object> updates,
        Action onSuccess = null,
        Action<string> onError = null)
    {
        FirebaseManager.Instance.UpdateDocument(
            POINTS_COLLECTION,
            username,
            updates,
            onSuccess,
            onError
        );
    }

    public void Delete(
        string username,
        Action onSuccess = null,
        Action<string> onError = null)
    {
        FirebaseManager.Instance.DeleteDocument(
            POINTS_COLLECTION,
            username,
            onSuccess,
            onError
        );
    }

    // Called on every scan (collection or daily quest) to atomically bump total + category counts.
    public void AddScanStats(
        string username,
        string category,
        Action onSuccess = null,
        Action<string> onError = null,
        string trashType = null)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            onError?.Invoke("Username is required to record scan stats.");
            return;
        }

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "total_trash", FieldValue.Increment(1) }
        };

        string categoryField = category?.Trim().ToLowerInvariant() switch
        {
            "non-bio" => "non_bio_trash",
            "biological" => "bio_trash",
            "recyclable" => "recicled_trash",
            _ => null
        };

        if (categoryField != null)
            updates[categoryField] = FieldValue.Increment(1);

        string dungeonField = trashType?.Trim().ToLowerInvariant() switch
        {
            "plastic" or "plastic bottle" => "plastic_scan_count",
            "paper" => "paper_scan_count",
            "food waste" => "food_waste_scan_count",
            _ => null
        };

        // Separate write so a rejected dungeon field can't block the totals above.
        if (dungeonField != null)
        {
            MergePoints(
                username,
                new Dictionary<string, object> { { dungeonField, FieldValue.Increment(1) } },
                null,
                error => Debug.LogError($"Failed to record {dungeonField}: {error}")
            );
        }

        MergePoints(username, updates, onSuccess, onError);
    }

    // Only bumps the per-type dungeon counter (plastic/paper/food-waste) without touching
    // total_trash or category stats.  Call this when a card is collected so progress is
    // always recorded, not just when a duplicate is detected.
    public void AddDungeonScanCount(
        string username,
        string trashType,
        Action onSuccess = null,
        Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            onError?.Invoke("Username is required to record dungeon scan count.");
            return;
        }

        string dungeonField = trashType?.Trim().ToLowerInvariant() switch
        {
            "plastic" or "plastic bottle" => "plastic_scan_count",
            "paper"                       => "paper_scan_count",
            "food waste"                  => "food_waste_scan_count",
            _                             => null
        };

        if (dungeonField == null)
            return;

        MergePoints(
            username,
            new Dictionary<string, object> { { dungeonField, FieldValue.Increment(1) } },
            onSuccess,
            onError
        );
    }

    // Like UpdatePoints, but creates the Points document if it is missing.
    private void MergePoints(
        string username,
        Dictionary<string, object> updates,
        Action onSuccess,
        Action<string> onError)
    {
        FirebaseManager manager = FirebaseManager.Instance;

        if (manager == null || manager.DB == null)
        {
            UpdatePoints(username, updates, onSuccess, onError);
            return;
        }

        manager.DB
            .Collection(POINTS_COLLECTION)
            .Document(username)
            .SetAsync(updates, SetOptions.MergeAll)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    string message = task.Exception != null
                        ? task.Exception.GetBaseException().Message
                        : "Write was canceled.";
                    onError?.Invoke(message);
                    return;
                }

                onSuccess?.Invoke();
            });
    }

    // Saves the score to dun1..dun5 only if it beats the stored high score.
    public void SubmitDungeonHighScore(
        string username,
        int dungeonNumber,
        int score,
        Action<bool> onResult = null,
        Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            dungeonNumber < 1 || dungeonNumber > 5 ||
            score <= 0)
        {
            onResult?.Invoke(false);
            return;
        }

        string field = "dun" + dungeonNumber;

        FirebaseManager.Instance.GetDocument(
            POINTS_COLLECTION,
            username,
            snapshot =>
            {
                int best = snapshot.Exists
                    ? FirebaseDataHelper.GetInt(snapshot.ToDictionary(), field)
                    : 0;

                if (score <= best)
                {
                    onResult?.Invoke(false);
                    return;
                }

                MergePoints(
                    username,
                    new Dictionary<string, object> { { field, score } },
                    () => onResult?.Invoke(true),
                    onError
                );
            },
            onError
        );
    }

    public void ReadLeaderboard(
        string sortField,
        int page,
        int pageSize,
        Action<List<Dictionary<string, object>>, int> onSuccess,
        Action<string> onError = null,
        bool descending = true)
    {
        if (Array.IndexOf(PointFields, sortField) < 0)
        {
            onError?.Invoke("Invalid leaderboard sort field.");
            return;
        }

        if (page < 1 || pageSize < 1)
        {
            onError?.Invoke("Page and page size must be greater than zero.");
            return;
        }

        Query query = FirebaseManager.Instance.DB
            .Collection(POINTS_COLLECTION);

        query = descending
            ? query.OrderByDescending(sortField)
            : query.OrderBy(sortField);

        long requestedCount = (long)page * pageSize;
        if (requestedCount > int.MaxValue)
        {
            onError?.Invoke("Requested leaderboard page is too large.");
            return;
        }

        FirebaseManager.Instance.QueryDocuments(
            query.Limit((int)requestedCount),
            snapshot =>
            {
                List<Dictionary<string, object>> results =
                    new List<Dictionary<string, object>>();
                List<DocumentSnapshot> documents =
                    snapshot.Documents.ToList();

                int startIndex = (page - 1) * pageSize;
                for (int index = startIndex;
                    index < documents.Count &&
                    results.Count < pageSize;
                    index++)
                {
                    results.Add(
                        documents[index].ToDictionary()
                    );
                }

                onSuccess?.Invoke(results, results.Count);
            },
            onError
        );
    }
}