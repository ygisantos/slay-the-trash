using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;

public class DialogueManager : MonoBehaviour
{
    
    private static DialogueManager instance;

    public static DialogueManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<DialogueManager>();

                if (instance == null)
                {
                    GameObject singletonPrefab =
                        Resources.Load<GameObject>("DialogueManager");

                    if (singletonPrefab == null)
                    {
                        Debug.LogError(
                            "DialogueManager prefab not found in Resources folder."
                        );

                        return null;
                    }

                    GameObject clone = Instantiate(singletonPrefab);
                    instance = clone.GetComponent<DialogueManager>();
                }
            }

            return instance;
        }
    }
    
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI positiveText;
    [SerializeField] private TextMeshProUGUI negativeText;

    [SerializeField] private Button positiveButton;
    [SerializeField] private Button negativeButton;

    [SerializeField] private Modal modal;

    public void ShowDialogue(string message, string positive, string negative, UnityEvent onPositive, UnityEvent onNegative)
    {
        messageText.text = message;
        positiveText.text = positive;
        negativeText.text = negative;

        positiveButton.onClick.RemoveAllListeners();
        positiveButton.onClick.AddListener(() =>
        {
            onPositive.Invoke();
            modal.Close();
        });

        negativeButton.onClick.RemoveAllListeners();
        negativeButton.onClick.AddListener(() =>
        {
            onNegative.Invoke();
            modal.Close();
        });

        modal.Open();
    }

    public void ShowDialogue(string message, string positive, string negative, UnityEvent onPositive)
    {
        messageText.text = message;
        positiveText.text = positive;
        negativeText.text = negative;

        positiveButton.onClick.RemoveAllListeners();
        positiveButton.onClick.AddListener(() =>
        {
            onPositive.Invoke();
            modal.Close();
        });

        negativeButton.onClick.RemoveAllListeners();
        negativeButton.onClick.AddListener(() =>
        {
            modal.Close();
        });
        modal.Open();
    }
}
