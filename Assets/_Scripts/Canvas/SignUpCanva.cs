using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SignUpCanva : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public TMP_InputField confirmPasswordField;

    [Header("Buttons")]
    public Button togglePassButton;
    public Button toggleConfirmPassButton;
    public Button signUpButton;

    [Header("Error Message")]
    public TextMeshProUGUI errorText;

    private bool isPasswordVisible = false;
    private bool isConfirmPasswordVisible = false;

    private void Start()
    {
        togglePassButton.onClick.AddListener(TogglePasswordVisibility);
        toggleConfirmPassButton.onClick.AddListener(ToggleConfirmPasswordVisibility);
        signUpButton.onClick.AddListener(SignUp);
        ClearError();
    }

    // --------------------------------------------------------
    // PASSWORD VISIBILITY
    // --------------------------------------------------------

    private void TogglePasswordVisibility()
    {
        isPasswordVisible = !isPasswordVisible;

        passwordField.contentType = TMP_InputField.ContentType.Custom;
        passwordField.inputType = isPasswordVisible ?
            TMP_InputField.InputType.Standard :
            TMP_InputField.InputType.Password;

        passwordField.ForceLabelUpdate();
    }


    private void ToggleConfirmPasswordVisibility()
    {
        isConfirmPasswordVisible = !isConfirmPasswordVisible;

        confirmPasswordField.contentType = TMP_InputField.ContentType.Custom;
        confirmPasswordField.inputType = isConfirmPasswordVisible ?
            TMP_InputField.InputType.Standard :
            TMP_InputField.InputType.Password;

        confirmPasswordField.ForceLabelUpdate();
    }

    // --------------------------------------------------------
    // SIGNUP FUNCTION
    // --------------------------------------------------------

    private void SignUp()
    {
        string username = usernameField.text.Trim();
        string pass1 = passwordField.text;
        string pass2 = confirmPasswordField.text;

        ClearError();

        // 1. Username empty
        if (string.IsNullOrWhiteSpace(username))
        {
            ShowError("Username cannot be empty.");
            return;
        }

        // 2. Check if username already exists
        if (DoesUserExist(username))
        {
            ShowError("Username already exists.");
            return;
        }

        // 3. Passwords do not match
        if (pass1 != pass2)
        {
            ShowError("Passwords do not match.");
            return;
        }

        // 4. Everything valid → Create account
        LogInManager.Instance.AddOrUpdateUser(username, pass1);

        ShowError("Account created successfully!", success: true);
    }

    // --------------------------------------------------------
    // USERNAME CHECK
    // --------------------------------------------------------

    private bool DoesUserExist(string username)
    {
        int count = PlayerPrefs.GetInt("UserCount", 0);

        for (int i = 0; i < count; i++)
        {
            string savedUser = PlayerPrefs.GetString("User" + i, "");
            if (savedUser == username)
                return true;
        }

        return false;
    }

    // --------------------------------------------------------
    // ERROR MESSAGE HELPERS
    // --------------------------------------------------------

    private void ShowError(string message, bool success = false)
    {
        if (errorText == null) return;

        errorText.text = message;
        errorText.color = success ? new Color(0.1f, 0.8f, 0.1f) : Color.red;
    }

    private void ClearError()
    {
        if (errorText != null)
            errorText.text = "";
    }
}
