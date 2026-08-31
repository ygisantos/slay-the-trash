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

    public void ShowDialogue(string message, string positive, string negative, UnityAction onPositive, UnityAction onNegative)
    {
        UnityEvent positiveEvent = new UnityEvent();
        if (onPositive != null)
            positiveEvent.AddListener(onPositive);

        UnityEvent negativeEvent = new UnityEvent();
        if (onNegative != null)
            negativeEvent.AddListener(onNegative);

        ShowDialogue(message, positive, negative, positiveEvent, negativeEvent);
    }

    public void ShowDialogue(string message, string positive, string negative, UnityAction onPositive)
    {
        UnityEvent positiveEvent = new UnityEvent();
        if (onPositive != null)
            positiveEvent.AddListener(onPositive);

        ShowDialogue(message, positive, negative, positiveEvent);
    }

    public void ShowDialogue(string message, string positive, string negative, UnityEvent onPositive, UnityEvent onNegative)
    {
        messageText.text = message;
        positiveText.text = positive;
        negativeText.text = negative;
        negativeText.gameObject.SetActive(true);
        negativeButton.gameObject.SetActive(true);

        positiveButton.onClick.RemoveAllListeners();
        positiveButton.onClick.AddListener(() =>
        {
            onPositive?.Invoke();
            modal.Close();
        });

        negativeButton.onClick.RemoveAllListeners();
        negativeButton.onClick.AddListener(() =>
        {
            onNegative?.Invoke();
            modal.Close();
        });

        modal.Open();
    }

    public void ShowDialogue(string message, string positive, string negative, UnityEvent onPositive)
    {
        messageText.text = message;
        positiveText.text = positive;
        negativeText.text = negative;
        negativeText.gameObject.SetActive(true);
        negativeButton.gameObject.SetActive(true);

        positiveButton.onClick.RemoveAllListeners();
        positiveButton.onClick.AddListener(() =>
        {
            onPositive?.Invoke();
            modal.Close();
        });

        negativeButton.onClick.RemoveAllListeners();
        negativeButton.onClick.AddListener(() =>
        {
            modal.Close();
        });
        modal.Open();
    }

    public void ShowErrorDialog(string message)
    {
        if (modal == null)
        {
            Debug.LogError(message);
            return;
        }

        ShowDialogue(
            message,
            "OK",
            "",
            new UnityEvent()
        );

        if (negativeText != null)
            negativeText.gameObject.SetActive(false);

        if (negativeButton != null)
            negativeButton.gameObject.SetActive(false);
    }

    public void ShowSuccessDialog(string message, UnityAction onConfirmed = null)
    {
        ShowDialogue(message, "OK", "", onConfirmed);

        if (negativeText != null)
            negativeText.gameObject.SetActive(false);

        if (negativeButton != null)
            negativeButton.gameObject.SetActive(false);
    }
}
