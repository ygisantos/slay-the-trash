using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    [Header("REFERENCES")]
    public GameObject dimPanel;       // Dim background panel
    public GameObject pausePanel;     // Pause menu panel
    public TMP_Text countdownText;    // Countdown TMP text

    [Header("SETTINGS")]
    public float countdownTime = 3f;  // Countdown duration
    private bool isCountingDown = false;
    private bool isPaused = false;
    private PlayerInput playerInput;
    private Coroutine countdownCoroutine;

    private void Awake()
    {
        dimPanel.SetActive(false);
        pausePanel.SetActive(false);
        countdownText.text = "";
    }

    private void Update()
    {
        // No keyboard or mouse pause logic.
        // Pause/Resume is controlled only by UI buttons.
    }

    // ==========================
    //      UI BUTTON CALLS
    // ==========================

    public void PauseGame()
    {
        SoundManager.PlaySound(SoundType.CLICK);
        if (isPaused) return;

        isPaused = true;
        Time.timeScale = 0f;

        // Enable pause UI
        dimPanel.SetActive(true);
        pausePanel.SetActive(true);

        // Block gameplay clicks
        var img = dimPanel.GetComponent<UnityEngine.UI.Image>();
        if (img) img.raycastTarget = true;


    }

    public void ResumeGame()
    {
        SoundManager.PlaySound(SoundType.CLICK);
        if (!isPaused || isCountingDown) return;

        // Hide pause panel, keep dim panel for countdown
        pausePanel.SetActive(false);
        dimPanel.SetActive(true);
        countdownText.gameObject.SetActive(true);

        // Start countdown
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        countdownCoroutine = StartCoroutine(ResumeCountdown());
    }

    private IEnumerator ResumeCountdown()
    {
        isCountingDown = true;

        float timeLeft = countdownTime;

        // Block UI clicks during countdown
        var img = dimPanel.GetComponent<UnityEngine.UI.Image>();
        if (img) img.raycastTarget = true;

        // Use unscaled time
        while (timeLeft > 0)
        {
            countdownText.text = Mathf.CeilToInt(timeLeft).ToString();
            yield return new WaitForSecondsRealtime(1f);
            timeLeft--;
        }

        // Countdown done
        countdownText.text = "";
        countdownText.gameObject.SetActive(false);
        dimPanel.SetActive(false);

        // Allow gameplay clicks
        if (img) img.raycastTarget = false;

        // Re-enable player input
        if (playerInput) playerInput.enabled = true;

        Time.timeScale = 1f;
        isCountingDown = false;
        isPaused = false;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SoundManager.PlaySound(SoundType.CLICK);
        SceneManager.LoadScene("MainMenuScene");
    }
}
