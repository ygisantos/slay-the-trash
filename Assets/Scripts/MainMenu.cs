using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class MainMenu : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private UnityEvent onLogoutConfirmed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string username = DataManager.Instance.GetUsername();
        if(ValidationHelper.IsNullOrWhiteSpace(username)) Transitioner.Instance.TransitionToScene("LoginScene");

        usernameText.text = username;     
    }

    public void ConfirmLogout()
    {
        DataManager.Instance.ClearUsername();
        Transitioner.Instance.TransitionToScene("LoginScene");
    }

    public void OpenLogout() {
        DialogueManager.Instance.ShowDialogue(
            "Are you sure you want to log out?",
            "Yes",
            "No",
            onLogoutConfirmed
        );
    }
}
