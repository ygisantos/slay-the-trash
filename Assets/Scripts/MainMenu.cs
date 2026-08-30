using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private UnityEvent onLogoutConfirmed;

    private void Start()
    {
        LoadUserProfileFromFirebase();
    }

    private void LoadUserProfileFromFirebase()
    {
        if (FBAuthentication.Instance == null ||
            !FBAuthentication.Instance.IsLoggedIn)
        {
            Transitioner.Instance.TransitionToScene("LoginScene");
            return;
        }

        LoadingScreenManager.Instance.Show("Fetching profile...");

        FBAuthentication.Instance.GetProfile(
            profile =>
            {
                LoadingScreenManager.Instance.Hide();
                DataManager.Instance.SetProfile(profile);

                string username =
                    profile != null &&
                    profile.TryGetValue("username", out object usernameObj)
                        ? usernameObj?.ToString() ?? string.Empty
                        : DataManager.Instance.GetUsername();

                if (ValidationHelper.IsNullOrWhiteSpace(username))
                {
                    Transitioner.Instance.TransitionToScene("LoginScene");
                    return;
                }

                if (usernameText != null)
                    usernameText.text = username;
            },
            error =>
            {
                LoadingScreenManager.Instance.Hide();
                Debug.LogError(error);
                DialogueManager.Instance.ShowErrorDialog(error);
                Transitioner.Instance.TransitionToScene("LoginScene");
            }
        );
    }

    public void ConfirmLogout()
    {
        DataManager.Instance.ClearAll();

        if (FBAuthentication.Instance != null)
            FBAuthentication.Instance.Logout();

        Transitioner.Instance.TransitionToScene("LoginScene");
    }

    public void OpenLogout()
    {
        DialogueManager.Instance.ShowDialogue(
            "Are you sure you want to log out?",
            "Yes",
            "No",
            onLogoutConfirmed
        );
    }
}
