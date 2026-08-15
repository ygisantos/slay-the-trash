using UnityEngine;

public enum CorrectChoice
{
    A,
    B
}

[CreateAssetMenu(menuName = "Data/Question Data", fileName = "QuestionData")]
public class QuestionData : ScriptableObject
{
    [TextArea]
    public string question;

    public string choiceA;
    public string choiceB;

    public CorrectChoice correctAnswer;

    [TextArea]
    public string answerAPopUp;
    [TextArea]
    public string answerBPopUp;
}
