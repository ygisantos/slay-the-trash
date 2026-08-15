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
    public Button togglePasswordButton;
    public Button loginButton;

    [Header("Error Message")]
    public TextMeshProUGUI errorText;

    [Header("Username List UI")]
    public UsernameListUI usernameListUI; // New system
    public GameObject usernameListPanel;  // Holder panel (not used for generating content anymore)

    private bool passwordVisible = false;

    private void Start()
    {
        ClearError();

        passwordField.contentType = TMP_InputField.ContentType.Password;

        loginButton.onClick.AddListener(Login);
        togglePasswordButton.onClick.AddListener(TogglePassword);

        usernameField.onSelect.AddListener((_) => ShowUsernameList());
        usernameField.onDeselect.AddListener((_) => HideUserListDelayed());
    }

    // --------------------------------------------------------
    // PASSWORD TOGGLE
    // --------------------------------------------------------
    private void TogglePassword()
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

        ClearError();

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
        if (errorText == null) return;

        errorText.text = msg;
        errorText.color = success ? new Color(0.1f, 0.85f, 0.1f) : Color.red;
    }

    private void ClearError()
    {
        if (errorText != null)
            errorText.text = "";
    }

    // --------------------------------------------------------
    // USERNAME LIST (integrated with UsernameListUI)
    // --------------------------------------------------------
    private void ShowUsernameList()
    {
        if (usernameListPanel == null || usernameListUI == null)
            return;


        usernameListPanel.SetActive(true);


        int count = PlayerPrefs.GetInt("UserCount", 0);

        if (count == 0)
        {
            usernameListUI.PopulateEmpty();
            usernameListPanel.SetActive(false);
            return;
        }

        string[] users = new string[count];
        for (int i = 0; i < count; i++)
            users[i] = PlayerPrefs.GetString("User" + i, "");

        usernameListUI.Populate(users);

        usernameListUI.onSelectUsername = (selectedUser) =>
        {
            usernameField.text = selectedUser;
            usernameListPanel.SetActive(false);
        };
    }


    private void HideUserListDelayed()
    {
        Invoke(nameof(HideUsernameList), 0.15f);
    }

    private void HideUsernameList()
    {
        if (usernameListPanel != null)
            usernameListPanel.SetActive(false);
    }

    
    public void showPassword() {
        showPasswordImg.SetActive(true);
        hidePasswordImg.SetActive(false);
    }

    public void hidePassword() {
        showPasswordImg.SetActive(false);
        hidePasswordImg.SetActive(true);
    }

    private void passwordMask(bool showPass) {
        
    }
}
