using UnityEngine;
using TMPro;

public class UserManager : MonoBehaviour
{

    [Header("Register")]
    [SerializeField] private TextMeshProUGUI username;
    [SerializeField] private TextMeshProUGUI password;
    [SerializeField] private TextMeshProUGUI passwordConfirm;
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private Modal modal;

    [Header("Login")]
    [SerializeField] private TextMeshProUGUI usernameLogin;
    [SerializeField] private TextMeshProUGUI passwordLogin;
    [SerializeField] private TextMeshProUGUI errorLoginText;

    private DynamicText dynamicTxt;
    public void Start()
    {
        errorText.text = "";
        errorLoginText.text = "";
    }

    public void Register()
    {
        dynamicTxt = errorText.GetComponent<DynamicText>();
        if (username == null || password == null || passwordConfirm == null)
        {
            Debug.LogError("Please assign all fields in the inspector.");
            return;
        }

        if (string.IsNullOrEmpty(username.text) || string.IsNullOrEmpty(password.text) || string.IsNullOrEmpty(passwordConfirm.text))
        {
            dynamicTxt.Error("Please fill in all fields.");
            return;
        }

        if (password.text != passwordConfirm.text)
        {
            Debug.LogError("Passwords do not match.");
            return;
        }

        FBAuthentication.Instance.Register(
            username.text,
            null,
            password.text,

            userId =>
            {
                dynamicTxt.Success($"Registration successful!");
                Debug.Log($"Registered: {userId}");
                Transitioner.Instance.TransitionToScene("MainMenuScene");
            },

            error =>
            {
                dynamicTxt.Error($"{error}");
                Debug.LogError(error);
            }
        );
    }
    
    public void Login()
    {
        dynamicTxt = errorLoginText.GetComponent<DynamicText>();
        if (usernameLogin == null || passwordLogin == null)
        {
            Debug.LogError("Please assign all fields in the inspector.");
            return;
        }

        if (string.IsNullOrEmpty(usernameLogin.text) || string.IsNullOrEmpty(passwordLogin.text))
        {
            Debug.LogError("Please fill in all fields.");
            return;
        }

        FBAuthentication.Instance.Login(
            usernameLogin.text,
            passwordLogin.text,

            profile =>
            {
                dynamicTxt.Success($"Login successful!");
                Debug.Log(
                    $"Welcome {profile["username"]}"
                );
                Transitioner.Instance.TransitionToScene("MainMenuScene");
            },

            error =>
            {
                dynamicTxt.Error($"{error}");
                Debug.LogError(error);
            }
        );
    }
}
