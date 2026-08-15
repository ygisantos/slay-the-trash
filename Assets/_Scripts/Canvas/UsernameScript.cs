using UnityEngine;
using TMPro;

public class UsernameScript : MonoBehaviour
{
    [Header("Display Username")]
    public TextMeshProUGUI usernameText;

    private void Start()
    {
        UpdateUsernameDisplay();
    }

    /// <summary>
    /// Refreshes the UI text with the current username from LoginManager.
    /// </summary>
    public void UpdateUsernameDisplay()
    {
        if (usernameText == null) return;

        if (LogInManager.Instance != null)
        {
            if(LogInManager.Instance.CurrentUsername == "")
            {
                usernameText.text = "No User";
                return;
            }
            usernameText.text = LogInManager.Instance.CurrentUsername;
        }
        else
        {
            usernameText.text = "No User";
        }
    }
}
