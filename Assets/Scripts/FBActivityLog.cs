using System;
using System.Collections.Generic;
using Firebase.Firestore;
using UnityEngine;

// Writes player activity to the "activity_logs" Firestore collection.
// Every entry is its own auto-id document with:
//   username, date_time, action, description
public static class FBActivityLog
{
    public const string COLLECTION = "activity_logs";

    // Action groups. Use one of these so dashboards can filter by area.
    public const string Authentication = "Authentication";
    public const string Game = "Game";
    public const string Collection = "Collection";
    public const string DailyQuest = "DailyQuest";
    public const string Leaderboard = "Leaderboard";

    // Logs an action for a known user. Logging is fire-and-forget: a failed write
    // only produces a console warning, because a dropped log must never break the
    // action the player just performed.
    public static void Log(string username, string action, string description)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(action))
            return;

        if (string.IsNullOrWhiteSpace(description))
            description = action;

        if (FirebaseManager.Instance == null)
            return;

        Dictionary<string, object> entry = new Dictionary<string, object>
        {
            { "username", username.Trim().ToLowerInvariant() },
            { "date_time", ServerTimeHelper.GetFirestoreTimestamp() },
            { "action", action },
            { "description", description }
        };

        FirebaseManager.Instance.CreateDocument(
            COLLECTION,
            entry,
            _ => { },
            error => Debug.LogWarning($"[ActivityLog] Could not write log: {error}")
        );

        Debug.Log($"[ActivityLog] {action} | {description} | {username}");
    }

    // Logs for whichever user is currently signed in. Does nothing when nobody is
    // signed in (for example the app opening with no cached session).
    public static void LogCurrent(string action, string description)
    {
        Log(CurrentUsername(), action, description);
    }

    // ---- app open ----

    // Shared flag: false until the app-open entry has been written.
    public static bool AppOpenLogged { get; private set; }

    // This project runs with Enter Play Mode Options = DisableDomainReload, so static
    // fields keep their values between Play sessions. Without this reset the app-open
    // entry is written once per Editor session lifetime instead of once per launch.
    // SubsystemRegistration runs before any scene Awake, in the Editor and in builds.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForNewLaunch()
    {
        AppOpenLogged = false;
    }

    // Logs the app launch exactly once per run. A restored session is reported as an
    // auto login so it is not confused with a player signing in again.
    //
    // Safe to call from several places: it only consumes the flag once BOTH Firestore
    // and the auth singleton are ready. FirestoreManager fires it when Firestore comes
    // up, FBAuthentication fires it after restoring the saved session, and whichever
    // happens last performs the single write. That way the entry always carries the
    // real username instead of "guest", no matter which scene each script lives in.
    public static void LogAppOpenOnce()
    {
        if (AppOpenLogged)
            return;

        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsInitialized)
            return;

        // Wait for FBAuthentication so CurrentUsername() can see the restored profile.
        if (FBAuthentication.Instance == null)
            return;

        AppOpenLogged = true;

        string username = CurrentUsername();
        bool hasSession = !string.IsNullOrWhiteSpace(username);

        Log(
            hasSession ? username : "guest",
            Authentication,
            hasSession
                ? "User opened the app (auto login from saved session)"
                : "App opened with no saved session"
        );
    }

    public static string CurrentUsername()
    {
        Dictionary<string, object> profile =
            FBAuthentication.Instance != null
                ? FBAuthentication.Instance.CurrentProfile
                : null;

        if (profile == null && DataManager.Instance != null)
            profile = DataManager.Instance.GetProfile();

        return FirebaseDataHelper.GetString(profile, "username");
    }
}
