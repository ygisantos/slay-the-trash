using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class CardViewCreator : Singleton<CardViewCreator>
{
    [SerializeField] private CardView cardViewPrefab;
     public List<CardView> cardViewList = new List<CardView>();
    public CardView CreateCardView(Card card, Vector3 position, Quaternion rotation)
    {
        CleanList();
        CardView cardView = Instantiate(cardViewPrefab, position, rotation,transform);
        cardView.transform.localPosition = Vector3.zero;
        cardView.transform.DOScale(Vector3.one, 0.15f);
        cardView.Setup(card);
        cardViewList.Add(cardView);
        return cardView;
    }
    public void CleanList()
    {
        cardViewList.RemoveAll(c => c == null);
    }
    public void CardNotInteractable(CardView c)
    {
        CleanList();

        foreach (CardView card in cardViewList)
        {
            if (c == null || c == card) continue;
            card.interactable = false;
        }
    }

    public void CardInteractable()
    {
        CleanList();

        foreach (CardView card in cardViewList)
        {
            card.interactable = true;
        }
    }

    public void ShowWrappers()
    {
        CleanList();

        foreach (CardView card in cardViewList)
        {
            card.wrapper.SetActive(true);
        }
    }

}
