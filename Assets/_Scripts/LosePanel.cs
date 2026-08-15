using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LosePanel : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Scene Names")]
    //[SerializeField] private string gameSceneName = "GameScene"; // Change to your game scene name
    [SerializeField] private string mainMenuSceneName = "MainMenu"; // Change to your main menu scene name

    [Header("Blocking")]
    [SerializeField] private Image blockingPanel; // Optional: semi-transparent background

    private void Awake()
    {
        // Setup button listeners
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryClicked);
        }
        else
        {
            Debug.LogWarning("Retry Button is not assigned!");
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
        else
        {
            Debug.LogWarning("Main Menu Button is not assigned!");
        }

        // Make sure blocking panel blocks raycasts
        if (blockingPanel != null)
        {
            blockingPanel.raycastTarget = true;
        }
    }

    public void OnRetryClicked()
    {
        Debug.Log("Retry button clicked - Reloading scene...");

        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        // OR if you want to load a specific scene:
        // SceneManager.LoadScene(gameSceneName);
    }

    public void OnMainMenuClicked()
    {
        Debug.Log("Main Menu button clicked - Loading main menu...");

        // Load main menu scene
        SceneManager.LoadScene(mainMenuSceneName);
    }


}