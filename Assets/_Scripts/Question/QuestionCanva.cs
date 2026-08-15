using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestionCanva : MonoBehaviour
{
    [Header("UI - Question")]
    public TMP_Text questionText;

    [Header("UI - Choices")]
    [SerializeField] private TMP_Text choiceAText;
    [SerializeField] private TMP_Text choiceBText;

    [Header("UI - Buttons")]
    public Button choiceAButton;
    public Button choiceBButton;
    public Button exitButton;

    [Header("UI - AnswerPopUp")]
    public TextMeshProUGUI answerPopUp;

    [Header("UI - Panels")]
    public GameObject questionCanva;
    public GameObject answerCanva;

    [Header("UI - Reward Display")]
    public TextMeshProUGUI rewardText1; // NEW: Display reward text 1
    public TextMeshProUGUI rewardText2; // NEW: Display reward text 2

    private QuestionData currentQuestion;

    private void Awake()
    {
        choiceAButton.onClick.AddListener(() => OnChoicePressed(true));
        choiceBButton.onClick.AddListener(() => OnChoicePressed(false));
        exitButton.onClick.AddListener(HideQuestion);
    }

    private void Start()
    {
        HideQuestion();
        ClearRewardTexts();
    }

    private void ClearRewardTexts()
    {
        if (rewardText1 != null) rewardText1.text = "";
        if (rewardText2 != null) rewardText2.text = "";
    }

    // ----------------------------------------------------
    // PUBLIC FUNCTION: Show a NEW random question
    // ----------------------------------------------------
    public void ShowRandomQuestion()
    {
        currentQuestion = QuestionManager.Instance.GetRandomQuestion();

        if (currentQuestion == null)
        {
            Debug.LogError("No question found in library!");
            return;
        }

        DisplayQuestion(currentQuestion);
        ClearRewardTexts();
    }

    // ----------------------------------------------------
    // PRIVATE: Update UI with loaded question
    // ----------------------------------------------------
    private void DisplayQuestion(QuestionData question)
    {
        questionText.text = question.question;
        choiceAText.text = question.choiceA;
        choiceBText.text = question.choiceB;

        answerCanva.SetActive(false);
        questionCanva.SetActive(true);
    }

    // ----------------------------------------------------
    // BUTTON HANDLERS
    // ----------------------------------------------------
    private void OnChoicePressed(bool isChoiceA)
    {
        bool isCorrect = false;

        // Determine if player chose the correct answer
        if (isChoiceA && currentQuestion.correctAnswer == CorrectChoice.A)
            isCorrect = true;
        else if (!isChoiceA && currentQuestion.correctAnswer == CorrectChoice.B)
            isCorrect = true;

        // Choose which explanation to display
        string popupText = isChoiceA ? currentQuestion.answerAPopUp : currentQuestion.answerBPopUp;

        // ----------------------------------------------------
        // GIVE REWARD IF CORRECT
        // ----------------------------------------------------
        if (isCorrect)
        {
            RewardManager.Instance.GiveRandomReward();
            DisplayReward(rewardText1);

            RewardManager.Instance.GiveRandomReward();
            DisplayReward(rewardText2);

            ScoreManager.Instance.AddEventComplete();

            popupText =
                "<color=green><b>CORRECT!</b></color>\n" +
                "<color=yellow><b>REWARD RECEIVED!</b></color>\n\n" +
                popupText;
        }
        else
        {
            popupText =
                "<color=red><b>WRONG!</b></color>\n\n" +
                popupText;
        }

        // Display popup
        answerPopUp.text = popupText;

        // Show answer panel
        answerCanva.SetActive(true);
    }

    private void HideQuestion()
    {
        answerCanva.SetActive(false);
        questionCanva.SetActive(false);
        ClearRewardTexts();
    }
    private void DisplayReward(TextMeshProUGUI txt)
    {
        if (txt == null) return;

        txt.text = RewardManager.Instance.rewardTxt;

        // Set color based on last reward type
        switch (RewardManager.Instance.lastRewardType)
        {
            case RewardType.AddHealth:
                txt.color = Color.green;
                break;
            case RewardType.AddShield:
                txt.color = Color.cyan;
                break;
            default:
                txt.color = Color.white;
                break;
        }
    }
}
