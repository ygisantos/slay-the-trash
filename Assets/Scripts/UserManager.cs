using UnityEngine;
using TMPro;

public class UserManager : MonoBehaviour
{
    private static UserManager instance;

    public static UserManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<UserManager>();

                if (instance == null)
                {
                    GameObject singletonPrefab =
                        Resources.Load<GameObject>("UserManager");

                    if (singletonPrefab == null)
                    {
                        Debug.LogError(
                            "UserManager prefab not found in Resources folder."
                        );

                        return null;
                    }

                    GameObject clone = Instantiate(singletonPrefab);
                    instance = clone.GetComponent<UserManager>();
                }
            }

            return instance;
        }
    }


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


    // =========================================================
    // SINGLETON
    // =========================================================

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

        LoadingScreenManager.Instance.Show("Creating account...");

        FBAuthentication.Instance.Register(
            usernameValue,
            null,
            passwordValue,

            userId =>
            {
                LoadingScreenManager.Instance.Hide();
                registerDynamicText.Success("Registration successful!");

                Debug.Log($"Registered: {userId}");

                // DataManager.Instance.SetProfile(
                //     FBAuthentication.Instance.CurrentProfile
                // );

                // Transitioner.Instance.TransitionToScene(
                //     "MainMenuScene"
                // );

                modal.Close();
                username.text = "";
                password.text = "";
                passwordConfirm.text = "";
                DialogueManager.Instance.ShowSuccessDialog(
                    "Registration successful! You can now log in."
                );
            },

            error =>
            {
                LoadingScreenManager.Instance.Hide();
                registerDynamicText.Error(error);
                DialogueManager.Instance.ShowErrorDialog(error);

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

        if (username.text.Length > 12)
        {
            registerDynamicText.Error(
                "Username cannot exceed 12 characters."
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

        LoadingScreenManager.Instance.Show("Signing in...");

        FBAuthentication.Instance.Login(
            usernameValue,
            passwordValue,

            profile =>
            {
                DataManager.Instance.SetProfile(profile);
                LoadingScreenManager.Instance.Hide(() =>
                {
                    loginDynamicText.Success("Login successful!");

                    Debug.Log(
                        $"Welcome {profile["username"]}"
                    );

                    DialogueManager.Instance.ShowSuccessDialog(
                        "Login successful!",
                        () => Transitioner.Instance.TransitionToScene("MainMenuScene")
                    );
                });
            },

            error =>
            {
                LoadingScreenManager.Instance.Hide(() =>
                {
                    loginDynamicText.Error(error);
                    DialogueManager.Instance.ShowErrorDialog(error);
                    Debug.LogError(error);
                });
            }
        );
    }


    public void Logout()
    {
        modal.Close();
        FBAuthentication.Instance.Logout();

        Debug.Log("Logged out.");

        Transitioner.Instance.TransitionToScene(
            "LoginScene"
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