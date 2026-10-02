using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    public Modal pauseModal;
    [Header("SETTINGS")]
    public float countdownTime = 3f;  // Countdown duration
    private bool isCountingDown = false;
    private bool isPaused = false;
    private PlayerInput playerInput;
    private Coroutine countdownCoroutine;
    private DynamicPopupToast toast;


    // ==========================
    //      UI BUTTON CALLS
    // ==========================

    public void PauseGame()
    {
        toast = DynamicPopupToast.Instance;
        SoundManager.PlaySound(SoundType.CLICK);
        if (isPaused) return;

        pauseModal.Open();
        isPaused = true;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        SoundManager.PlaySound(SoundType.CLICK);
        toast = DynamicPopupToast.Instance;
        if (!isPaused || isCountingDown) return;
 

        // Start countdown
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        countdownCoroutine = StartCoroutine(ResumeCountdown());
    }

    private IEnumerator ResumeCountdown()
    {
        isCountingDown = true;

        float timeLeft = countdownTime;
        toast = DynamicPopupToast.Instance;

        while (timeLeft > 0)
        {
            toast?.ShowLiveToast($"Resuming in {Mathf.CeilToInt(timeLeft)}...");
            yield return new WaitForSecondsRealtime(1f);
            timeLeft--;
        }

        toast?.HideLiveToast();

        // Re-enable player input
        if (playerInput) playerInput.enabled = true;

        Time.timeScale = 1f;
        isCountingDown = false;
        isPaused = false;
        pauseModal.Close();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        Transitioner.Instance.TransitionToScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SoundManager.PlaySound(SoundType.CLICK);
        Transitioner.Instance.TransitionToScene("MainMenuScene");
    }
}
