using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CardSystem : Singleton<CardSystem>
{
    [SerializeField] private HandView handView;
    [SerializeField] private Transform drawPilePoint;
    [SerializeField] private Transform discardPilePoint;
    private readonly List<Card> drawPile = new();
    private readonly List<Card> discardPile = new();
    private readonly List<Card> hand = new();

    public bool IsCombatActive { get; private set; }

    void Start()
    {
        SetDeckVisualsActive(false);
    }

    void OnEnable()
    {
        ActionSystem.AttachPerformer<DrawCardsGA>(DrawCardsPerformer);
        ActionSystem.AttachPerformer<DiscardAllCardsGA>(DiscardAllCardsPerformer);
        ActionSystem.AttachPerformer<PlayCardGA>(PlayCardPerformer);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<DrawCardsGA>();
        ActionSystem.DetachPerformer<DiscardAllCardsGA>();
        ActionSystem.DetachPerformer<PlayCardGA>();
    }

    public void Setup(List<CardData> deckData)
    {
        drawPile.Clear();
        discardPile.Clear();
        foreach (var cardData in deckData)
        {
            Card card = new(cardData);
            drawPile.Add(card);
        }
    }

    public void BeginCombat(List<CardData> deckData)
    {
        ClearAllCardsImmediate();
        Setup(deckData);
        IsCombatActive = true;
        SetDeckVisualsActive(true);
    }

    public void EndCombat()
    {
        ClearAllCardsImmediate();
        IsCombatActive = false;
        SetDeckVisualsActive(false);
        if (ManaSystem.Instance != null)
            ManaSystem.Instance.ResetMana();
    }

    private IEnumerator DrawCardsPerformer(DrawCardsGA drawCardsGA)
    {
        if (!IsCombatActive || drawCardsGA.Amount <= 0)
            yield break;

        EnsureDrawPileHasCards();

        int remaining = drawCardsGA.Amount;
        int fromDrawPile = Mathf.Min(remaining, drawPile.Count);
        for (int i = 0; i < fromDrawPile; i++)
        {
            yield return DrawCard();
        }

        remaining -= fromDrawPile;
        if (remaining <= 0) yield break;

        RefillDeck();
        EnsureDrawPileHasCards();
        int fromDiscard = Mathf.Min(remaining, drawPile.Count);
        for (int i = 0; i < fromDiscard; i++)
        {
            yield return DrawCard();
        }
    }

    private IEnumerator DiscardAllCardsPerformer(DiscardAllCardsGA discardAllCardsGA)
    {
        foreach (var card in new List<Card>(hand))
        {
            CardView cardView = handView.RemoveCard(card);
            if (cardView == null) continue;
            yield return DiscardCard(cardView);
        }
        hand.Clear();
    }

    private IEnumerator PlayCardPerformer(PlayCardGA playCardGA)
    {
        if (!IsCombatActive) yield break;

        hand.Remove(playCardGA.Card);
        CardView cardView = handView.RemoveCard(playCardGA.Card);
        if (cardView != null)
            yield return DiscardCard(cardView);

        SpendManaGA spendManaGA = new(playCardGA.Card.Mana);
        ActionSystem.Instance.AddReaction(spendManaGA);

        if (playCardGA.Card.ManualTargetEffect != null)
        {
            PerformEffectGA performEffectGA = new(playCardGA.Card.ManualTargetEffect, new() { playCardGA.ManualTarget });
            ActionSystem.Instance.AddReaction(performEffectGA);
        }
        foreach (var effectWrapper in playCardGA.Card.OtherEffects)
        {
            List<CombatantView> targets = effectWrapper.TargetMode.GetTargets();
            PerformEffectGA performEffectGA = new(effectWrapper.Effect, targets);
            ActionSystem.Instance.AddReaction(performEffectGA);
        }
    }

    private IEnumerator DrawCard()
    {
        Card card = drawPile.Draw();
        if (card == null) yield break;

        hand.Add(card);
        CardView cardView = CardViewCreator.Instance.CreateCardView(card, drawPilePoint.position, drawPilePoint.rotation);
        yield return handView.AddCard(cardView);
    }

    private void RefillDeck()
    {
        drawPile.AddRange(discardPile);
        discardPile.Clear();
    }

    private void EnsureDrawPileHasCards()
    {
        if (drawPile.Count > 0) return;
        if (discardPile.Count > 0)
        {
            RefillDeck();
            return;
        }
        if (CardManager.Instance == null) return;
        Setup(CardManager.Instance.GetCardDataList(10));
    }

    private IEnumerator DiscardCard(CardView cardView)
    {
        discardPile.Add(cardView.Card);
        cardView.transform.DOScale(Vector3.zero, 0.15f);
        Tween tween = cardView.transform.DOMove(discardPilePoint.position, 0.15f);
        yield return tween.WaitForCompletion();
        Destroy(cardView.gameObject);
    }

    private void ClearAllCardsImmediate()
    {
        if (CardViewHoverSystem.Instance != null)
            CardViewHoverSystem.Instance.Hide();

        if (Interactions.Instance != null)
            Interactions.Instance.PlayerIsDragging = false;

        if (handView != null)
            handView.Clear();

        if (CardViewCreator.Instance != null)
            CardViewCreator.Instance.ClearAll();

        hand.Clear();
        drawPile.Clear();
        discardPile.Clear();
    }

    private void SetDeckVisualsActive(bool active)
    {
        if (handView != null)
            handView.gameObject.SetActive(active);

        EndTurnButtonUI endTurn = FindAnyObjectByType<EndTurnButtonUI>(FindObjectsInactive.Include);
        if (endTurn == null) return;

        Canvas canvas = endTurn.GetComponentInParent<Canvas>(true);
        if (canvas != null)
            canvas.gameObject.SetActive(active);
        else
            endTurn.gameObject.SetActive(active);
    }
}
