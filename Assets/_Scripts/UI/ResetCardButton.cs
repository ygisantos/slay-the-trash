using UnityEngine;

public class ResetCardButton : MonoBehaviour
{
    public void ResetCard()
    {
        DeckManager.instance.InitializeDeck();
    }
}
