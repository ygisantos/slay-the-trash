using System.Collections;
using UnityEngine;

public class ResetCardButton : MonoBehaviour
{
    private static readonly string[] Warnings =
    {
        "Are you sure you want to reset your cards?",
        "Are you really sure?",
        "LAST WARNING: this is not revertable!"
    };

    public void ResetCard()
    {
        StartCoroutine(ConfirmRoutine());
    }

    private IEnumerator ConfirmRoutine()
    {
        foreach (string warning in Warnings)
        {
            bool? answer = null;

            DialogueManager.Instance.ShowDialogue(
                warning,
                "Yes",
                "No",
                () => answer = true,
                () => answer = false
            );

            yield return new WaitUntil(() => answer.HasValue);
            if (answer == false)
                yield break;

            // Modal.Open() is ignored until the previous dialogue has fully closed.
            yield return new WaitUntil(() => !DialogueManager.Instance.IsShowing);
        }

        DeckManager.instance.InitializeDeck();
    }
}
