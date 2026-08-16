using System;
using System.Collections.Generic;
using System.Linq;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

public class FBAuthentication : MonoBehaviour
{
    public static FBAuthentication Instance { get; private set; }

    private const string USERS_COLLECTION = "users";

    public string CurrentUserId { get; private set; }

    public Dictionary<string, object> CurrentProfile { get; private set; }

    public bool IsLoggedIn =>
        !string.IsNullOrEmpty(CurrentUserId);


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
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

        username = username.Trim();

        if (!string.IsNullOrWhiteSpace(email))
            email = email.Trim();
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

                // Account creation date
                { "createdAt", Timestamp.GetCurrentTimestamp() }
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

        usernameOrEmail = usernameOrEmail.Trim();


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

        CurrentUserId = document.Id;
        CurrentProfile = profile;

        onSuccess?.Invoke(profile);
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

        Dictionary<string, object> updates =
            new Dictionary<string, object>
            {
                {
                    "password",
                    newPassword
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

        newEmail = newEmail.Trim();


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

                Dictionary<string, object> updates =
                    new Dictionary<string, object>
                    {
                        {
                            "email",
                            newEmail
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

        usernameOrEmail = usernameOrEmail.Trim();


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