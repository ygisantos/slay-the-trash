using UnityEngine;
using TMPro;

public class UserManager : MonoBehaviour
{
    [Header("Register")]
    [SerializeField] private TMP_InputField username;
    [SerializeField] private TMP_InputField password;
    [SerializeField] private TMP_InputField passwordConfirm;

    [SerializeField] private GameObject unhidePassword;
    [SerializeField] private GameObject hidePassword;
    [SerializeField] private GameObject unhidePasswordConfirm;
    [SerializeField] private GameObject hidePasswordConfirm;

    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private Modal modal;

    [Header("Login")]
    [SerializeField] private TMP_InputField usernameLogin;
    [SerializeField] private TMP_InputField passwordLogin;

    [SerializeField] private GameObject unhidePasswordLogin;
    [SerializeField] private GameObject hidePasswordLogin;

    [SerializeField] private TextMeshProUGUI errorLoginText;

    private DynamicText registerDynamicText;
    private DynamicText loginDynamicText;


    private void Start()
    {
        if (errorText != null)
            errorText.text = "";

        if (errorLoginText != null)
            errorLoginText.text = "";

        // Make sure passwords start hidden
        SetPasswordVisibility(
            password,
            unhidePassword,
            hidePassword,
            false
        );

        SetPasswordVisibility(
            passwordConfirm,
            unhidePasswordConfirm,
            hidePasswordConfirm,
            false
        );

        SetPasswordVisibility(
            passwordLogin,
            unhidePasswordLogin,
            hidePasswordLogin,
            false
        );
    }


    // =========================================================
    // REGISTER
    // =========================================================

    public void Register()
    {
        if (!ValidateRegisterFields())
            return;

        GetRegisterDynamicText();

        string usernameValue = username.text.Trim();
        string passwordValue = password.text;

        FBAuthentication.Instance.Register(
            usernameValue,
            null,
            passwordValue,

            userId =>
            {
                registerDynamicText.Success("Registration successful!");

                Debug.Log($"Registered: {userId}");

                Transitioner.Instance.TransitionToScene(
                    "MainMenuScene"
                );
            },

            error =>
            {
                registerDynamicText.Error(error);

                Debug.LogError(error);
            }
        );
    }


    private bool ValidateRegisterFields()
    {
        GetRegisterDynamicText();

        if (username == null ||
            password == null ||
            passwordConfirm == null)
        {
            registerDynamicText.Error(
                "Please assign all register fields in the Inspector."
            );

            return false;
        }

        if (ValidationHelper.IsNullOrWhiteSpace(username.text))
        {
            registerDynamicText.Error(
                "Username is required."
            );

            return false;
        }

        if (ValidationHelper.IsNullOrWhiteSpace(password.text))
        {
            registerDynamicText.Error(
                "Password is required."
            );

            return false;
        }

        if (ValidationHelper.IsNullOrWhiteSpace(passwordConfirm.text))
        {
            registerDynamicText.Error(
                "Please confirm your password."
            );

            return false;
        }

        if (!ValidationHelper.IsUsername(username.text))
        {
            registerDynamicText.Error(
                "Username can only contain letters, numbers, and underscores."
            );

            return false;
        }

        if (!ValidationHelper.IsLengthBetween(
                username.text,
                3,
                20))
        {
            registerDynamicText.Error(
                "Username must be between 3 and 20 characters."
            );

            return false;
        }

        if (!ValidationHelper.IsPasswordLengthValid(
                password.text,
                8,
                128))
        {
            registerDynamicText.Error(
                "Password must be at least 8 characters."
            );

            return false;
        }

        if (password.text != passwordConfirm.text)
        {
            registerDynamicText.Error(
                "Passwords do not match."
            );

            return false;
        }

        return true;
    }


    // =========================================================
    // LOGIN
    // =========================================================

    public void Login()
    {
        GetLoginDynamicText();

        if (usernameLogin == null ||
            passwordLogin == null)
        {
            loginDynamicText.Error(
                "Please assign all login fields in the Inspector."
            );

            return;
        }

        if (ValidationHelper.IsNullOrWhiteSpace(usernameLogin.text))
        {
            loginDynamicText.Error(
                "Username is required."
            );

            return;
        }

        if (ValidationHelper.IsNullOrWhiteSpace(passwordLogin.text))
        {
            loginDynamicText.Error(
                "Password is required."
            );

            return;
        }

        string usernameValue = usernameLogin.text.Trim();
        string passwordValue = passwordLogin.text;

        FBAuthentication.Instance.Login(
            usernameValue,
            passwordValue,

            profile =>
            {
                loginDynamicText.Success(
                    "Login successful!"
                );

                Debug.Log(
                    $"Welcome {profile["username"]}"
                );

                Transitioner.Instance.TransitionToScene(
                    "MainMenuScene"
                );
            },

            error =>
            {
                loginDynamicText.Error(error);

                Debug.LogError(error);
            }
        );
    }


    // =========================================================
    // PASSWORD TOGGLE
    // =========================================================

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


    public void TogglePasswordRegister()
    {
        TogglePassword(
            password,
            unhidePassword,
            hidePassword
        );
    }


    public void TogglePasswordConfirm()
    {
        TogglePassword(
            passwordConfirm,
            unhidePasswordConfirm,
            hidePasswordConfirm
        );
    }


    public void TogglePasswordLogin()
    {
        TogglePassword(
            passwordLogin,
            unhidePasswordLogin,
            hidePasswordLogin
        );
    }


    // =========================================================
    // DYNAMIC TEXT
    // =========================================================

    private void GetRegisterDynamicText()
    {
        if (registerDynamicText == null &&
            errorText != null)
        {
            registerDynamicText =
                errorText.GetComponent<DynamicText>();
        }
    }


    private void GetLoginDynamicText()
    {
        if (loginDynamicText == null &&
            errorLoginText != null)
        {
            loginDynamicText =
                errorLoginText.GetComponent<DynamicText>();
        }
    }
}