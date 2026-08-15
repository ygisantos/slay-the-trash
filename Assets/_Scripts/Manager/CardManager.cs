using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }

    [Header("Set in Inspector")]
    [SerializeField] private CardPool originalPool;
    [SerializeField] private CardLibrary library;

    [Header("Runtime Pool (Hidden)")]
    [SerializeField] private List<CardPoolEntry> runtimePool = new List<CardPoolEntry>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    private void Start()
    {
        if(DeckManager.instance.cardNames.Count != 0)
            PullDeckManagerData();
    }

    private void PullDeckManagerData()
    {
        if (DeckManager.instance == null) return;

        var names = DeckManager.instance.cardNames;

        originalPool.cards.Clear();
        foreach (var n in names)
            originalPool.cards.Add(new CardPoolEntry(n, 1));
        originalPool.cards.Add(new CardPoolEntry("Scavenge", 5));
        ResetRuntimePool();
    }


    /// <summary>s
    /// Copies the original CardPool into the runtime pool.
    /// </summary>
    private void ResetRuntimePool()
    {
        if (originalPool == null) return;
        runtimePool = new List<CardPoolEntry>(originalPool.cards);
    }
    public List<CardData> GetCardDataList(int count)
    {
        List<CardData> result = new List<CardData>();

        for (int i = 0; i < count; i++)
        {
            if (runtimePool.Count == 0)
            {
                ResetRuntimePool();
            }

            int randomIndex = Random.Range(0, runtimePool.Count);
            var entry = runtimePool[randomIndex];

            // Convert name → CardData using library
            CardData data = FindCardData(entry.Name);
            if (data != null)
            {
                result.Add(data);
            }

            // Decrease amount
            entry.Amount--;

            if (entry.Amount <= 0)
            {
                runtimePool.RemoveAt(randomIndex);
            }
            else
            {
                runtimePool[randomIndex] = entry;
            }

            // When 10 entries left, refill
            if (runtimePool.Count <= 1)
            {
                ResetRuntimePool();
            }
        }

        return result;
    }

    /// <summary>
    /// Looks up CardData in the library using its name.
    /// </summary>
    private CardData FindCardData(string name)
    {
        foreach (var c in library.cards)
        {
            if (c.Name == name)
                return c.Data;
        }

        Debug.LogWarning($"[CardManager] Card not found in pool: {name}");
        return null;
    }
}
