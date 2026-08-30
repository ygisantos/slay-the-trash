using System;
using System.Collections.Generic;
using System.Linq;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

public class FBAuthentication : MonoBehaviour
{
    private static FBAuthentication instance;

    public static FBAuthentication Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<FBAuthentication>();

                if (instance == null)
                {
                    GameObject singletonPrefab =
                        Resources.Load<GameObject>("FirebaseManager");

                    if (singletonPrefab == null)
                    {
                        Debug.LogError(
                            "FBAuthentication prefab not found in Resources folder."
                        );

                        return null;
                    }

                    GameObject clone = Instantiate(singletonPrefab);
                    instance = clone.GetComponent<FBAuthentication>();
                }
            }

            return instance;
        }
    }


    private const string USERS_COLLECTION = "users";

    public string CurrentUserId { get; private set; }

    public Dictionary<string, object> CurrentProfile { get; private set; }

    public bool IsLoggedIn =>
        !string.IsNullOrEmpty(CurrentUserId);


    // ============================================================
    // SINGLETON
    // ============================================================

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


    // ============================================================
    // REGISTER
    // ============================================================

    public void Register(
        string username,
        string email,
        string password,
        Action<string> onSuccess = null,
        Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            onError?.Invoke("Username is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            onError?.Invoke("Password is required.");
            return;
        }

        username = username.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(email))
            email = email.Trim().ToLowerInvariant();
        else
            email = null;


        // --------------------------------------------------------
        // CHECK USERNAME
        // --------------------------------------------------------

        Query usernameQuery =
            FirebaseManager.Instance.DB
                .Collection(USERS_COLLECTION)
                .WhereEqualTo("username", username)
                .Limit(1);

        FirebaseManager.Instance.QueryDocuments(
            usernameQuery,

            usernameSnapshot =>
            {
                if (usernameSnapshot.Count > 0)
                {
                    onError?.Invoke(
                        "Username already exists."
                    );

                    return;
                }


                // ------------------------------------------------
                // CHECK EMAIL
                // ------------------------------------------------

                if (string.IsNullOrEmpty(email))
                {
                    CreateUser(
                        username,
                        null,
                        password,
                        onSuccess,
                        onError
                    );

                    return;
                }

                Query emailQuery =
                    FirebaseManager.Instance.DB
                        .Collection(USERS_COLLECTION)
                        .WhereEqualTo("email", email)
                        .Limit(1);

                FirebaseManager.Instance.QueryDocuments(
                    emailQuery,

                    emailSnapshot =>
                    {
                        if (emailSnapshot.Count > 0)
                        {
                            onError?.Invoke(
                                "Email already exists."
                            );

                            return;
                        }

                        CreateUser(
                            username,
                            email,
                            password,
                            onSuccess,
                            onError
                        );
                    },

                    onError
                );
            },

            onError
        );
    }


    private void CreateUser(
        string username,
        string email,
        string password,
        Action<string> onSuccess,
        Action<string> onError)
    {
        string userId = Guid.NewGuid().ToString();

        Timestamp now = ServerTimeHelper.GetFirestoreTimestamp();

        Dictionary<string, object> profile =
            new Dictionary<string, object>
            {
                { "userId", userId },
                { "username", username },
                { "email", email },
                { "password", password },

                // Account status
                { "disabled", false },
                { "onDisabled", null },

                // Account timestamps
                { "registered_at", now },
                { "createdAt", now },
                { "email_update_at", null },
                { "logged_in_at", null },
                { "password_update_at", null }
            };

        FirebaseManager.Instance.CreateDocument(
            USERS_COLLECTION,
            userId,
            profile,

            id =>
            {
                CurrentUserId = id;
                CurrentProfile = profile;

                onSuccess?.Invoke(id);
            },

            onError
        );
    }


    // ============================================================
    // LOGIN
    // ============================================================

    public void Login(
        string usernameOrEmail,
        string password,
        Action<Dictionary<string, object>> onSuccess = null,
        Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail))
        {
            onError?.Invoke(
                "Username or email is required."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            onError?.Invoke(
                "Password is required."
            );

            return;
        }

        usernameOrEmail =
            usernameOrEmail.Trim().ToLowerInvariant();


        // --------------------------------------------------------
        // TRY USERNAME
        // --------------------------------------------------------

        Query usernameQuery =
            FirebaseManager.Instance.DB
                .Collection(USERS_COLLECTION)
                .WhereEqualTo("username", usernameOrEmail)
                .WhereEqualTo("password", password)
                .Limit(1);

        FirebaseManager.Instance.QueryDocuments(
            usernameQuery,

            usernameSnapshot =>
            {
                if (usernameSnapshot.Count > 0)
                {
                    ProcessLoginResult(
                        usernameSnapshot,
                        onSuccess,
                        onError
                    );

                    return;
                }


                // ------------------------------------------------
                // TRY EMAIL
                // ------------------------------------------------

                Query emailQuery =
                    FirebaseManager.Instance.DB
                        .Collection(USERS_COLLECTION)
                        .WhereEqualTo("email", usernameOrEmail)
                        .WhereEqualTo("password", password)
                        .Limit(1);

                FirebaseManager.Instance.QueryDocuments(
                    emailQuery,

                    emailSnapshot =>
                    {
                        if (emailSnapshot.Count > 0)
                        {
                            ProcessLoginResult(
                                emailSnapshot,
                                onSuccess,
                                onError
                            );

                            return;
                        }

                        onError?.Invoke(
                            "Invalid username/email or password."
                        );
                    },

                    onError
                );
            },

            onError
        );
    }


    private void ProcessLoginResult(
        QuerySnapshot snapshot,
        Action<Dictionary<string, object>> onSuccess,
        Action<string> onError)
    {
        DocumentSnapshot document =
            snapshot.Documents.FirstOrDefault();

        if (document == null)
        {
            onError?.Invoke(
                "User account could not be found."
            );

            return;
        }

        Dictionary<string, object> profile =
            document.ToDictionary();


        // --------------------------------------------------------
        // CHECK DISABLED
        // --------------------------------------------------------

        bool disabled =
            GetBool(profile, "disabled");

        if (disabled)
        {
            onError?.Invoke(
                "This account has been disabled."
            );

            return;
        }


        // --------------------------------------------------------
        // LOGIN SUCCESS
        // --------------------------------------------------------

        Timestamp now = ServerTimeHelper.GetFirestoreTimestamp();

        Dictionary<string, object> updates =
            new Dictionary<string, object>
            {
                { "logged_in_at", now }
            };

        profile["logged_in_at"] = now;

        FirebaseManager.Instance.UpdateDocument(
            USERS_COLLECTION,
            document.Id,
            updates,

            () =>
            {
                CurrentUserId = document.Id;
                CurrentProfile = profile;

                onSuccess?.Invoke(profile);
            },

            error =>
            {
                CurrentUserId = document.Id;
                CurrentProfile = profile;

                onSuccess?.Invoke(profile);
            }
        );
    }


    // ============================================================
    // CHANGE PASSWORD
    // ============================================================

    public void ChangePassword(
        string currentPassword,
        string newPassword,
        Action onSuccess = null,
        Action<string> onError = null)
    {
        if (!IsLoggedIn)
        {
            onError?.Invoke(
                "No user is currently logged in."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(currentPassword))
        {
            onError?.Invoke(
                "Current password is required."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            onError?.Invoke(
                "New password is required."
            );

            return;
        }

        string storedPassword =
            GetString(CurrentProfile, "password");

        if (storedPassword != currentPassword)
        {
            onError?.Invoke(
                "Current password is incorrect."
            );

            return;
        }

        if (currentPassword == newPassword)
        {
            onError?.Invoke(
                "New password must be different from the current password."
            );

            return;
        }

        Timestamp now = ServerTimeHelper.GetFirestoreTimestamp();

        Dictionary<string, object> updates =
            new Dictionary<string, object>
            {
                {
                    "password",
                    newPassword
                },
                {
                    "password_update_at",
                    now
                }
            };

        FirebaseManager.Instance.UpdateDocument(
            USERS_COLLECTION,
            CurrentUserId,
            updates,

            () =>
            {
                CurrentProfile["password"] =
                    newPassword;
                CurrentProfile["password_update_at"] =
                    now;

                onSuccess?.Invoke();
            },

            onError
        );
    }


    // ============================================================
    // CHANGE EMAIL
    // ============================================================

    public void ChangeEmail(
        string newEmail,
        Action onSuccess = null,
        Action<string> onError = null)
    {
        if (!IsLoggedIn)
        {
            onError?.Invoke(
                "No user is currently logged in."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(newEmail))
        {
            onError?.Invoke(
                "Email is required."
            );

            return;
        }

        newEmail =
            newEmail.Trim().ToLowerInvariant();


        // --------------------------------------------------------
        // CHECK IF EMAIL ALREADY EXISTS
        // --------------------------------------------------------

        Query emailQuery =
            FirebaseManager.Instance.DB
                .Collection(USERS_COLLECTION)
                .WhereEqualTo("email", newEmail)
                .Limit(1);

        FirebaseManager.Instance.QueryDocuments(
            emailQuery,

            snapshot =>
            {
                DocumentSnapshot existing =
                    snapshot.Documents.FirstOrDefault();

                if (existing != null &&
                    existing.Id != CurrentUserId)
                {
                    onError?.Invoke(
                        "Email already exists."
                    );

                    return;
                }


                // ------------------------------------------------
                // UPDATE EMAIL
                // ------------------------------------------------

                Timestamp now = ServerTimeHelper.GetFirestoreTimestamp();

                Dictionary<string, object> updates =
                    new Dictionary<string, object>
                    {
                        {
                            "email",
                            newEmail
                        },
                        {
                            "email_update_at",
                            now
                        }
                    };

                FirebaseManager.Instance.UpdateDocument(
                    USERS_COLLECTION,
                    CurrentUserId,
                    updates,

                    () =>
                    {
                        CurrentProfile["email"] =
                            newEmail;
                        CurrentProfile["email_update_at"] =
                            now;

                        onSuccess?.Invoke();
                    },

                    onError
                );
            },

            onError
        );
    }


    // ============================================================
    // SEARCH USERNAME / EMAIL
    // ============================================================

    public void SearchUser(
        string usernameOrEmail,
        Action<Dictionary<string, object>> onSuccess = null,
        Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail))
        {
            onError?.Invoke(
                "Username or email is required."
            );

            return;
        }

        usernameOrEmail =
            usernameOrEmail.Trim().ToLowerInvariant();


        // --------------------------------------------------------
        // SEARCH USERNAME
        // --------------------------------------------------------

        Query usernameQuery =
            FirebaseManager.Instance.DB
                .Collection(USERS_COLLECTION)
                .WhereEqualTo("username", usernameOrEmail)
                .Limit(1);

        FirebaseManager.Instance.QueryDocuments(
            usernameQuery,

            usernameSnapshot =>
            {
                DocumentSnapshot usernameDocument =
                    usernameSnapshot.Documents.FirstOrDefault();

                if (usernameDocument != null)
                {
                    onSuccess?.Invoke(
                        usernameDocument.ToDictionary()
                    );

                    return;
                }


                // ------------------------------------------------
                // SEARCH EMAIL
                // ------------------------------------------------

                Query emailQuery =
                    FirebaseManager.Instance.DB
                        .Collection(USERS_COLLECTION)
                        .WhereEqualTo("email", usernameOrEmail)
                        .Limit(1);

                FirebaseManager.Instance.QueryDocuments(
                    emailQuery,

                    emailSnapshot =>
                    {
                        DocumentSnapshot emailDocument =
                            emailSnapshot.Documents.FirstOrDefault();

                        if (emailDocument != null)
                        {
                            onSuccess?.Invoke(
                                emailDocument.ToDictionary()
                            );

                            return;
                        }

                        onError?.Invoke(
                            "User not found."
                        );
                    },

                    onError
                );
            },

            onError
        );
    }


    // ============================================================
    // GET PROFILE
    // ============================================================

    public void GetProfile(
        Action<Dictionary<string, object>> onSuccess = null,
        Action<string> onError = null)
    {
        if (!IsLoggedIn)
        {
            onError?.Invoke(
                "No user is currently logged in."
            );

            return;
        }

        FirebaseManager.Instance.GetDocument(
            USERS_COLLECTION,
            CurrentUserId,

            snapshot =>
            {
                if (!snapshot.Exists)
                {
                    onError?.Invoke(
                        "User profile does not exist."
                    );

                    return;
                }

                CurrentProfile =
                    snapshot.ToDictionary();

                onSuccess?.Invoke(
                    CurrentProfile
                );
            },

            onError
        );
    }


    // ============================================================
    // LOGOUT
    // ============================================================

    public void Logout()
    {
        CurrentUserId = null;
        CurrentProfile = null;
    }


    // ============================================================
    // HELPERS
    // ============================================================

    private string GetString(
        Dictionary<string, object> data,
        string key)
    {
        if (data == null)
            return null;

        if (!data.TryGetValue(key, out object value))
            return null;

        return value?.ToString();
    }


    private bool GetBool(
        Dictionary<string, object> data,
        string key)
    {
        if (data == null)
            return false;

        if (!data.TryGetValue(key, out object value))
            return false;

        if (value is bool boolValue)
            return boolValue;

        return bool.TryParse(
            value?.ToString(),
            out bool result
        ) && result;
    }
}
