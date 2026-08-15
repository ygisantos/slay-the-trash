using UnityEngine;

public class QuestionManager : MonoBehaviour
{
    public static QuestionManager Instance { get; private set; }

    [Header("Data")]
    public QuestionLibrary questionLibrary;
    public QuestionCanva questionCanva;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    public void QuestionActivate()
    {
        questionCanva.ShowRandomQuestion();
    }
    /// <summary>
    /// Returns a random QuestionData from the library.
    /// </summary>
    public QuestionData GetRandomQuestion()
    {
        if (questionLibrary == null || questionLibrary.questions.Count == 0)
        {
            Debug.LogWarning("QuestionLibrary is empty or not assigned!");
            return null;
        }

        int index = Random.Range(0, questionLibrary.questions.Count);
        return questionLibrary.questions[index];
    }
}
