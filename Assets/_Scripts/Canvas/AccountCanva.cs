using UnityEngine;

public class AccountCanva : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject loginCanvas;
    [SerializeField] private GameObject signupCanvas;

    private void Start()
    {
        // Ensure at least one is active
        if (loginCanvas != null && signupCanvas != null)
        {
            loginCanvas.SetActive(true);
            signupCanvas.SetActive(false);
        }
    }

    /// <summary>
    /// Show SIGN UP panel and hide LOGIN
    /// </summary>
    public void ShowSignUp()
    {
        if (loginCanvas != null) loginCanvas.SetActive(false);
        if (signupCanvas != null) signupCanvas.SetActive(true);
    }

    /// <summary>
    /// Show LOGIN panel and hide SIGN UP
    /// </summary>
    public void ShowLogin()
    {
        if (signupCanvas != null) signupCanvas.SetActive(false);
        if (loginCanvas != null) loginCanvas.SetActive(true);
    }
}
