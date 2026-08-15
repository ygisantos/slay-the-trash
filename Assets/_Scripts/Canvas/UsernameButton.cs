using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UsernameButton : MonoBehaviour
{
    [SerializeField] TMP_Text usernameText;

    private string currentUsername;
    private System.Action<string> onClick;

    public void Setup(string username, System.Action<string> onClickCallback)
    {
        currentUsername = username;
        usernameText.text = username;
        onClick = onClickCallback;
    }

    public void OnClick()
    {
        onClick?.Invoke(currentUsername);
    }
}
