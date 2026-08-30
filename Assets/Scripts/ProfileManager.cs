using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ProfileManager : MonoBehaviour
{
    [Header("Profile Fields")]
    [SerializeField] private TMP_InputField usernameField;
    [SerializeField] private TMP_InputField emailField;
    [SerializeField] private TMP_InputField newPasswordField;
    [SerializeField] private TMP_InputField confirmPasswordField;

    [Header("Profile Labels")]
    [SerializeField] private TMP_Text registeredAtLabel;
    [SerializeField] private TMP_Text lastEmailUpdateLabel;
    [SerializeField] private TMP_Text lastPasswordUpdateLabel;
    [SerializeField] private TMP_Text lastLoginLabel;

    private void Start()
    {
        SetupProfile();
    }

    private void SetupProfile()
    {
        if (usernameField != null)
        {
            usernameField.readOnly = true;
            usernameField.interactable = false;
        }

        Dictionary<string, object> profile =
            DataManager.Instance != null
                ? DataManager.Instance.GetProfile()
                : new Dictionary<string, object>();

        if (profile == null || profile.Count == 0)
        {
            Debug.LogWarning("No profile cache found.");
            ReturnToLogin();
            return;
        }

        string username =
            profile.TryGetValue("username", out object usernameObj)
                ? usernameObj?.ToString() ?? string.Empty
                : string.Empty;

        string email =
            profile.TryGetValue("email", out object emailObj)
                ? emailObj?.ToString() ?? string.Empty
                : string.Empty;

        if (usernameField != null)
            usernameField.text = username;

        if (emailField != null)
            emailField.text = email;

        UpdateProfileLabels(profile);
    }

    private void ReturnToLogin()
    {
        Debug.Log("Returning to login screen.");

        if (Transitioner.Instance != null)
        {
            Transitioner.Instance.TransitionToScene("LoginScene");
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
    }

    private void UpdateProfileLabels()
    {
        Dictionary<string, object> profile =
            DataManager.Instance != null
                ? DataManager.Instance.GetProfile()
                : new Dictionary<string, object>();

        UpdateProfileLabels(profile);
    }

    private void UpdateProfileLabels(Dictionary<string, object> profile)
    {
        if (profile == null || profile.Count == 0)
        {
            SetLabel(registeredAtLabel, "Not available");
            SetLabel(lastEmailUpdateLabel, "Not available");
            SetLabel(lastPasswordUpdateLabel, "Not available");
            SetLabel(lastLoginLabel, "Not available");
            return;
        }

        SetLabel(
            registeredAtLabel,
            "Registered: " + FormatTimestamp(profile, "registered_at")
        );

        SetLabel(
            lastEmailUpdateLabel,
            "Last email update: " + FormatTimestamp(profile, "email_update_at")
        );

        SetLabel(
            lastPasswordUpdateLabel,
            "Last password update: " + FormatTimestamp(profile, "password_update_at")
        );

        SetLabel(
            lastLoginLabel,
            "Last login: " + FormatTimestamp(profile, "logged_in_at")
        );
    }

    private static void SetLabel(TMP_Text label, string value)
    {
        if (label == null)
            return;

        label.text = value;
    }

    private string FormatTimestamp(
        System.Collections.Generic.Dictionary<string, object> profile,
        string key)
    {
        if (profile == null ||
            !profile.TryGetValue(key, out object value) ||
            value == null)
        {
            return "Never";
        }

        try
        {
            if (value is Firebase.Firestore.Timestamp timestamp)
            {
                return timestamp.ToDateTime().ToLocalTime()
                    .ToString("MMMM dd, yyyy");
            }

            if (value is DateTime dateTime)
            {
                return dateTime.ToLocalTime()
                    .ToString("MMMM dd, yyyy");
            }

            if (value is string text &&
                DateTime.TryParse(text, out DateTime parsed))
            {
                return parsed.ToLocalTime()
                    .ToString("MMMM dd, yyyy");
            }
        }
        catch
        {
            // Ignore conversion issues and fall through.
        }

        return value.ToString();
    }

    public void UpdateEmail()
    {
        if (emailField == null)
        {
            Debug.LogError("Email field is not assigned.");
            return;
        }

        string email = emailField.text.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            Debug.LogError("Email is required.");
            return;
        }

        if (!ValidationHelper.IsEmail(email))
        {
            Debug.LogError("Please enter a valid email address.");
            return;
        }

        if (FBAuthentication.Instance == null ||
            !FBAuthentication.Instance.IsLoggedIn)
        {
            Debug.LogError("No user is currently logged in.");
            return;
        }

        LoadingScreenManager.Instance.Show("Updating email...");

        FBAuthentication.Instance.ChangeEmail(
            email,
            () =>
            {
                LoadingScreenManager.Instance.Hide();
                Debug.Log("Email updated successfully.");
                DataManager.Instance.SetProfile(FBAuthentication.Instance.CurrentProfile);
                SetupProfile();
            },
            error =>
            {
                LoadingScreenManager.Instance.Hide();
                Debug.LogError(error);
                DialogueManager.Instance.ShowErrorDialog(error);
            }
        );
    }

    public void UpdatePassword()
    {
        if (newPasswordField == null ||
            confirmPasswordField == null)
        {
            Debug.LogError("Password fields are not assigned.");
            return;
        }

        string newPassword = newPasswordField.text;
        string confirmPassword = confirmPasswordField.text;

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            Debug.LogError("New password is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(confirmPassword))
        {
            Debug.LogError("Please confirm your password.");
            return;
        }

        if (!ValidationHelper.IsPasswordLengthValid(newPassword, 8, 128))
        {
            Debug.LogError("Password must be at least 8 characters.");
            return;
        }

        if (newPassword != confirmPassword)
        {
            Debug.LogError("Passwords do not match.");
            return;
        }

        if (FBAuthentication.Instance == null ||
            !FBAuthentication.Instance.IsLoggedIn)
        {
            Debug.LogError("No user is currently logged in.");
            return;
        }

        string currentPassword =
            FBAuthentication.Instance.CurrentProfile != null
                && FBAuthentication.Instance.CurrentProfile.TryGetValue(
                    "password",
                    out object currentPasswordObj
                )
                    ? currentPasswordObj?.ToString()
                    : string.Empty;

        LoadingScreenManager.Instance.Show("Updating password...");

        FBAuthentication.Instance.ChangePassword(
            currentPassword,
            newPassword,
            () =>
            {
                LoadingScreenManager.Instance.Hide();
                newPasswordField.text = "";
                confirmPasswordField.text = "";

                Debug.Log("Password updated successfully.");
                DataManager.Instance.SetProfile(FBAuthentication.Instance.CurrentProfile);
            },
            error =>
            {
                LoadingScreenManager.Instance.Hide();
                Debug.LogError(error);
                DialogueManager.Instance.ShowErrorDialog(error);
            }
        );
    }
}
