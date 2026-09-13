using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

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

    [Header("Security")]
    [SerializeField] private GameObject unhidePassword;
    [SerializeField] private GameObject hidePassword;
    [SerializeField] private GameObject unhidePasswordConfirm;
    [SerializeField] private GameObject hidePasswordConfirm;
    
    private void Start()
    {
        SetupProfile();
    }

    
    private void TogglePassword(
        TMP_InputField passwordField,
        GameObject unhideObj,
        GameObject hideObj)
    {
        if (passwordField == null)
            return;

        bool passwordVisible =
            passwordField.contentType ==
            TMP_InputField.ContentType.Standard;

        SetPasswordVisibility(
            passwordField,
            unhideObj,
            hideObj,
            !passwordVisible
        );
    }
    
    public void ToggleNewPasswordVisibility()
    {
        TogglePassword(
            newPasswordField,
            unhidePassword,
            hidePassword
        );
    }
    
    public void ToggleConfirmPasswordVisibility()
    {
        TogglePassword(
            confirmPasswordField,
            unhidePasswordConfirm,
            hidePasswordConfirm
        );
    }

    private void SetPasswordVisibility(
        TMP_InputField passwordField,
        GameObject unhideObj,
        GameObject hideObj,
        bool visible)
    {
        if (passwordField == null)
            return;

        passwordField.contentType =
            visible
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;

        passwordField.ForceLabelUpdate();

        if (unhideObj != null)
            unhideObj.SetActive(!visible);

        if (hideObj != null)
            hideObj.SetActive(visible);
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
            if (FBAuthentication.Instance != null &&
                FBAuthentication.Instance.IsLoggedIn)
            {
                LoadingScreenManager.Instance.Show("Fetching profile...");
                FBAuthentication.Instance.GetProfile(
                    refreshedProfile =>
                    {
                        LoadingScreenManager.Instance.Hide();
                        DataManager.Instance.SetProfile(refreshedProfile);
                        ApplyProfile(refreshedProfile);
                    },
                    error =>
                    {
                        LoadingScreenManager.Instance.Hide();
                        Debug.LogError(error);
                        ReturnToLogin();
                    }
                );
                return;
            }

            Debug.LogWarning("No profile cache found and no active session.");
            ReturnToLogin();
            return;
        }

        ApplyProfile(profile);
    }

    private void ApplyProfile(Dictionary<string, object> profile)
    {
        if (profile == null || profile.Count == 0)
        {
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

        Transitioner.Instance.TransitionToScene("LoginScene");
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
            "" + FormatTimestamp(profile, "registered_at")
        );

        SetLabel(
            lastEmailUpdateLabel,
            "" + FormatTimestamp(profile, "email_update_at")
        );

        SetLabel(
            lastPasswordUpdateLabel,
            "" + FormatTimestamp(profile, "password_update_at")
        );

        SetLabel(
            lastLoginLabel,
            "" + FormatTimestamp(profile, "logged_in_at")
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
            ShowProfileError("Email field is not assigned.");
            return;
        }

        string email = emailField.text.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowProfileError("Email is required.");
            return;
        }

        if (!ValidationHelper.IsEmail(email))
        {
            ShowProfileError("Please enter a valid email address.");
            return;
        }

        if (FBAuthentication.Instance == null ||
            !FBAuthentication.Instance.IsLoggedIn)
        {
            ShowProfileError("No user is currently logged in.");
            return;
        }

        string oldEmail =
            FBAuthentication.Instance.CurrentProfile != null
                && FBAuthentication.Instance.CurrentProfile.TryGetValue(
                    "email",
                    out object oldEmailObj
                )
                    ? oldEmailObj?.ToString() ?? string.Empty
                    : string.Empty;

        if (string.Equals(oldEmail, email, StringComparison.OrdinalIgnoreCase))
        {
            ShowProfileError("This is the same email you already use.");
            return;
        }

        UnityEngine.Events.UnityAction onConfirm = new UnityEngine.Events.UnityAction(() =>
        {
            LoadingScreenManager.Instance.Show("Updating email...");

            FBAuthentication.Instance.ChangeEmail(
                email,
                () =>
                {
                    LoadingScreenManager.Instance.Hide(() =>
                    {
                        Debug.Log("Email updated successfully.");
                        DataManager.Instance.SetProfile(FBAuthentication.Instance.CurrentProfile);
                        DialogueManager.Instance.ShowSuccessDialog(
                            "Email updated successfully. Please log in again.",
                            LogoutAndReturnToLogin
                        );
                    });
                },
                error =>
                {
                    LoadingScreenManager.Instance.Hide(() =>
                    {
                        Debug.LogError(error);
                        DialogueManager.Instance.ShowErrorDialog(error);
                    });
                }
            );
        });

        DialogueManager.Instance.ShowDialogue(
            "Update your email to:\n" + email + "?",
            "Yes, update",
            "Cancel",
            onConfirm
        );
    }

    public void UpdatePassword()
    {
        if (newPasswordField == null ||
            confirmPasswordField == null)
        {
            ShowProfileError("Password fields are not assigned.");
            return;
        }

        string newPassword = newPasswordField.text;
        string confirmPassword = confirmPasswordField.text;

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            ShowProfileError("New password is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(confirmPassword))
        {
            ShowProfileError("Please confirm your password.");
            return;
        }

        if (!ValidationHelper.IsPasswordLengthValid(newPassword, 8, 128))
        {
            ShowProfileError("Password must be at least 8 characters.");
            return;
        }

        if (newPassword != confirmPassword)
        {
            ShowProfileError("Passwords do not match.");
            return;
        }

        if (FBAuthentication.Instance == null ||
            !FBAuthentication.Instance.IsLoggedIn)
        {
            ShowProfileError("No user is currently logged in.");
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

        UnityEngine.Events.UnityAction onConfirm = new UnityEngine.Events.UnityAction(() =>
        {
            LoadingScreenManager.Instance.Show("Updating password...");

            FBAuthentication.Instance.ChangePassword(
                currentPassword,
                newPassword,
                () =>
                {
                    LoadingScreenManager.Instance.Hide(() =>
                    {
                        newPasswordField.text = "";
                        confirmPasswordField.text = "";
                        Debug.Log("Password updated successfully.");
                        DataManager.Instance.SetProfile(FBAuthentication.Instance.CurrentProfile);
                        DialogueManager.Instance.ShowSuccessDialog(
                            "Password updated successfully. Please log in again.",
                            LogoutAndReturnToLogin
                        );
                    });
                },
                error =>
                {
                    LoadingScreenManager.Instance.Hide(() =>
                    {
                        Debug.LogError(error);
                        DialogueManager.Instance.ShowErrorDialog(error);
                    });
                }
            );
        });

        DialogueManager.Instance.ShowDialogue(
            "Update your password?",
            "Yes, update",
            "Cancel",
            onConfirm
        );
    }

    private void ShowProfileError(string message)
    {
        Debug.LogError(message);

        if (DialogueManager.Instance != null)
            DialogueManager.Instance.ShowErrorDialog(message);
    }

    private void LogoutAndReturnToLogin()
    {
        if (FBAuthentication.Instance != null)
            FBAuthentication.Instance.Logout();

        if (DataManager.Instance != null)
            DataManager.Instance.ClearAll();

        if (Transitioner.Instance != null)
            Transitioner.Instance.TransitionToScene("LoginScene");
        else
            Transitioner.Instance.TransitionToScene("LoginScene");
    }
}
