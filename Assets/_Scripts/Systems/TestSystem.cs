using System.Collections.Generic;
using UnityEngine;

public class TestSystem : MonoBehaviour
{
    [SerializeField] private List<CardData> deckData;

    private void Start()
    {
        if (CardSystem.Instance == null) return;
        CardSystem.Instance.BeginCombat(deckData);
        ActionSystem.Instance.Perform(new DrawCardsGA(5));
    }
}
