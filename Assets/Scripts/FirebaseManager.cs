using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    private FirebaseFirestore db;

    public FirebaseFirestore DB => db;

    public bool IsInitialized { get; private set; }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeFirebase();
    }


    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError(
                        $"Firebase dependency check failed: {task.Exception}"
                    );

                    return;
                }

                if (task.IsCanceled)
                {
                    Debug.LogError(
                        "Firebase dependency check was canceled."
                    );

                    return;
                }

                DependencyStatus status = task.Result;

                if (status == DependencyStatus.Available)
                {
                    db = FirebaseFirestore.DefaultInstance;

                    IsInitialized = true;

                    Debug.Log(
                        "Firebase Firestore initialized successfully."
                    );
                }
                else
                {
                    Debug.LogError(
                        $"Could not resolve Firebase dependencies: {status}"
                    );
                }
            });
    }


    // ============================================================
    // CREATE DOCUMENT
    // ============================================================

    public void CreateDocument(
        string collection,
        string documentId,
        Dictionary<string, object> data,
        Action<string> onSuccess = null,
        Action<string> onError = null)
    {
        if (!CheckInitialized(onError))
            return;

        if (string.IsNullOrWhiteSpace(collection))
        {
            onError?.Invoke("Collection name cannot be empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            onError?.Invoke("Document ID cannot be empty.");
            return;
        }

        if (data == null)
        {
            onError?.Invoke("Document data cannot be null.");
            return;
        }

        DocumentReference document =
            db.Collection(collection).Document(documentId);

        document.SetAsync(data)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    onError?.Invoke(GetErrorMessage(task));
                    return;
                }

                if (task.IsCanceled)
                {
                    onError?.Invoke(
                        "Create operation was canceled."
                    );

                    return;
                }

                onSuccess?.Invoke(documentId);
            });
    }


    // ============================================================
    // CREATE DOCUMENT WITH AUTOMATIC FIRESTORE ID
    // ============================================================

    public void CreateDocument(
        string collection,
        Dictionary<string, object> data,
        Action<string> onSuccess = null,
        Action<string> onError = null)
    {
        if (!CheckInitialized(onError))
            return;

        if (string.IsNullOrWhiteSpace(collection))
        {
            onError?.Invoke("Collection name cannot be empty.");
            return;
        }

        if (data == null)
        {
            onError?.Invoke("Document data cannot be null.");
            return;
        }

        DocumentReference document =
            db.Collection(collection).Document();

        document.SetAsync(data)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    onError?.Invoke(GetErrorMessage(task));
                    return;
                }

                if (task.IsCanceled)
                {
                    onError?.Invoke(
                        "Create operation was canceled."
                    );

                    return;
                }

                onSuccess?.Invoke(document.Id);
            });
    }


    // ============================================================
    // GET DOCUMENT
    // ============================================================

    public void GetDocument(
        string collection,
        string documentId,
        Action<DocumentSnapshot> onSuccess,
        Action<string> onError = null)
    {
        if (!CheckInitialized(onError))
            return;

        if (string.IsNullOrWhiteSpace(collection))
        {
            onError?.Invoke("Collection name cannot be empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            onError?.Invoke("Document ID cannot be empty.");
            return;
        }

        DocumentReference document =
            db.Collection(collection).Document(documentId);

        document.GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    onError?.Invoke(GetErrorMessage(task));
                    return;
                }

                if (task.IsCanceled)
                {
                    onError?.Invoke(
                        "Get operation was canceled."
                    );

                    return;
                }

                onSuccess?.Invoke(task.Result);
            });
    }


    // ============================================================
    // GET COLLECTION
    // ============================================================

    public void GetCollection(
        string collection,
        Action<QuerySnapshot> onSuccess,
        Action<string> onError = null)
    {
        if (!CheckInitialized(onError))
            return;

        if (string.IsNullOrWhiteSpace(collection))
        {
            onError?.Invoke("Collection name cannot be empty.");
            return;
        }

        db.Collection(collection)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    onError?.Invoke(GetErrorMessage(task));
                    return;
                }

                if (task.IsCanceled)
                {
                    onError?.Invoke(
                        "Get collection operation was canceled."
                    );

                    return;
                }

                onSuccess?.Invoke(task.Result);
            });
    }


    // ============================================================
    // QUERY
    // ============================================================

    public void QueryDocuments(
        Query query,
        Action<QuerySnapshot> onSuccess,
        Action<string> onError = null)
    {
        if (!CheckInitialized(onError))
            return;

        if (query == null)
        {
            onError?.Invoke("Query cannot be null.");
            return;
        }

        query.GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    onError?.Invoke(GetErrorMessage(task));
                    return;
                }

                if (task.IsCanceled)
                {
                    onError?.Invoke(
                        "Query operation was canceled."
                    );

                    return;
                }

                onSuccess?.Invoke(task.Result);
            });
    }


    // ============================================================
    // UPDATE DOCUMENT
    // ============================================================

    public void UpdateDocument(
        string collection,
        string documentId,
        Dictionary<string, object> updates,
        Action onSuccess = null,
        Action<string> onError = null)
    {
        if (!CheckInitialized(onError))
            return;

        if (string.IsNullOrWhiteSpace(collection))
        {
            onError?.Invoke("Collection name cannot be empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            onError?.Invoke("Document ID cannot be empty.");
            return;
        }

        if (updates == null || updates.Count == 0)
        {
            onError?.Invoke("Updates cannot be empty.");
            return;
        }

        DocumentReference document =
            db.Collection(collection).Document(documentId);

        document.UpdateAsync(updates)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    onError?.Invoke(GetErrorMessage(task));
                    return;
                }

                if (task.IsCanceled)
                {
                    onError?.Invoke(
                        "Update operation was canceled."
                    );

                    return;
                }

                onSuccess?.Invoke();
            });
    }


    // ============================================================
    // DELETE DOCUMENT
    // ============================================================

    public void DeleteDocument(
        string collection,
        string documentId,
        Action onSuccess = null,
        Action<string> onError = null)
    {
        if (!CheckInitialized(onError))
            return;

        if (string.IsNullOrWhiteSpace(collection))
        {
            onError?.Invoke("Collection name cannot be empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            onError?.Invoke("Document ID cannot be empty.");
            return;
        }

        DocumentReference document =
            db.Collection(collection).Document(documentId);

        document.DeleteAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    onError?.Invoke(GetErrorMessage(task));
                    return;
                }

                if (task.IsCanceled)
                {
                    onError?.Invoke(
                        "Delete operation was canceled."
                    );

                    return;
                }

                onSuccess?.Invoke();
            });
    }


    // ============================================================
    // CHECK DOCUMENT EXISTS
    // ============================================================

    public void DocumentExists(
        string collection,
        string documentId,
        Action<bool> onResult,
        Action<string> onError = null)
    {
        GetDocument(
            collection,
            documentId,

            snapshot =>
            {
                onResult?.Invoke(snapshot.Exists);
            },

            onError
        );
    }


    // ============================================================
    // HELPERS
    // ============================================================

    private bool CheckInitialized(Action<string> onError)
    {
        if (!IsInitialized || db == null)
        {
            onError?.Invoke(
                "Firebase Firestore is not initialized yet."
            );

            return false;
        }

        return true;
    }


    private string GetErrorMessage(
        System.Threading.Tasks.Task task)
    {
        if (task.Exception != null)
        {
            return task.Exception
                .Flatten()
                .InnerException?.Message
                ?? task.Exception.Message;
        }

        return "Unknown Firebase error.";
    }
}