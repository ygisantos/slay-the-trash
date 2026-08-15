using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Timer: MonoBehaviour
{
    public Text timerText; // Drag your Timer (Text) here
    public ThrowingInstructionsSceneHandler sceneHandler; // Drag the script holder here

    public int countdownSeconds = 10;

    void Start()
    {
        if (timerText != null && sceneHandler != null)
        {
            StartCoroutine(StartCountdown());
        }
        else
        {
            Debug.LogError("TimerText or SceneHandler not assigned.");
        }
    }

    IEnumerator StartCountdown()
    {
        int remaining = countdownSeconds;

        while (remaining > 0)
        {
            timerText.text = $"Proceeding to Trashbin Scanner in {remaining} seconds";
            yield return new WaitForSeconds(1f);
            remaining--;
        }

        timerText.text = "Loading Trashbin Scanner...";
        sceneHandler.LoadOCRSceneScene(); // Call your function after countdown
    }
}
