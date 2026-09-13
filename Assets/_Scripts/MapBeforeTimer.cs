using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapBeforeTimer : MonoBehaviour
{
    public float time;
    public TextMeshProUGUI timeDisplay;
    public string sceneToLoad;

    private float timeEnd;

    void Start()
    {
        timeEnd = Time.time + time;
    }

    private void Update()
    {
        int timeLeft = (int)(timeEnd - Time.time);
        timeDisplay.text = $"Next room in {timeLeft + 1}";

        if (Time.time >= timeEnd)
        {
            Transitioner.Instance.TransitionToScene(sceneToLoad);
        }
    }
}
