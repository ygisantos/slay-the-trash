using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LogInCanva : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    
    [SerializeField] private GameObject showPasswordImg;
    [SerializeField] private GameObject hidePasswordImg;

    [Header("Buttons")]
    public Button loginButton;

    [Header("Error Message")]
    public TextMeshProUGUI errorText;

    [Header("Username List UI")]
    public UsernameListUI usernameListUI; // New system
    public GameObject usernameListPanel;  // Holder panel (not used for generating content anymore)

    private bool passwordVisible = false;

    private void Start()
    {
        passwordField.contentType = TMP_InputField.ContentType.Password;

        loginButton.onClick.AddListener(Login);
    }

    // --------------------------------------------------------
    // PASSWORD TOGGLE
    // --------------------------------------------------------
    private void togglePassword()
    {
        passwordVisible = !passwordVisible;

        passwordField.contentType =
            passwordVisible ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;

        passwordField.ForceLabelUpdate();
    }

    // --------------------------------------------------------
    // LOGIN FUNCTION
    // --------------------------------------------------------
    private void Login()
    {
        string username = usernameField.text.Trim();
        string password = passwordField.text;

        if (string.IsNullOrWhiteSpace(username))
        {
            ShowError("Username cannot be empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError("Password cannot be empty.");
            return;
        }

        if (!DoesUserExist(username))
        {
            ShowError("Username does not exist.");
            return;
        }

        bool success = LogInManager.Instance.ValidateLogin(username, password);

        if (!success)
        {
            ShowError("Incorrect password.");
            return;
        }

        ShowError("Login successful!", true);
        SceneManager.LoadScene("MainMenuScene");
    }

    // --------------------------------------------------------
    // CHECK USERNAME EXISTS
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
    // ERROR UI
    // --------------------------------------------------------
    private void ShowError(string msg, bool success = false)
    {
    }

    public void showPassword(bool isVisible) {
        showPasswordImg.SetActive(isVisible);
        hidePasswordImg.SetActive(!isVisible);
        togglePassword();
    }

}
