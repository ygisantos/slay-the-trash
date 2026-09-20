using UnityEngine;

public class CardViewHoverSystem : Singleton<CardViewHoverSystem>
{
    [SerializeField] private GameObject cardViewHoeverPrefab;

    private GameObject cardObject; // current hover instance
    public Transform viewTransform;
    private CardView cardViewHoever; // reference to its CardView component

    public void Show(Card card, Vector3 position)
    {
        if (cardObject != null)
        {
            Destroy(cardObject);
        }

        cardObject = Instantiate(cardViewHoeverPrefab, position, Quaternion.identity, viewTransform);

        cardViewHoever = cardObject.GetComponent<CardView>();
        if (cardViewHoever == null)
        {
            return;
        }

        // Setup card data
        cardViewHoever.Setup(card);
        cardViewHoever.interactable = false;
        cardViewHoever.sg.sortingOrder = 99;

        // Hover copy must not steal the mouse from the hand card (first card sits at z=0
        // and otherwise enter/exit-loops with this preview).
        foreach (var col in cardObject.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        cardObject.SetActive(true);
    }

    public void Hide()
    {
        if (cardObject != null)
        {
            Destroy(cardObject);
            cardObject = null;
        }
    }
}
