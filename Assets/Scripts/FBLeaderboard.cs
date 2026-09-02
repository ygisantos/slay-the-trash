using System;
using System.Collections.Generic;
using System.Linq;
using Firebase.Firestore;
using UnityEngine;

public class FBLeaderboard : MonoBehaviour
{
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