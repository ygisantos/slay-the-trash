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
        PullDeckManagerData();
    }

    private void OnEnable()
    {
        DeckManager.OnDeckUpdated += OnDeckUpdated;
    }

    private void OnDisable()
    {
        DeckManager.OnDeckUpdated -= OnDeckUpdated;
    }

    private void OnDeckUpdated()
    {
        PullDeckManagerData();
    }

    public void PullDeckManagerData()
    {
        runtimePool.Clear();

        DeckManager deck = DeckManager.instance != null ? DeckManager.instance : FindFirstObjectByType<DeckManager>();

        if (deck != null && deck.cardNames != null && deck.cardNames.Count > 0)
        {
            var cardCounts = new Dictionary<string, int>();
            foreach (var name in deck.cardNames)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (!cardCounts.ContainsKey(name))
                    cardCounts[name] = 0;
                cardCounts[name]++;
            }

            foreach (var kvp in cardCounts)
            {
                runtimePool.Add(new CardPoolEntry(kvp.Key, kvp.Value));
            }
        }

        // Always include basic starter cards
        runtimePool.Add(new CardPoolEntry("Scavenge", 5));
    }

    /// <summary>
    /// Refills the runtime pool with unlocked cards.
    /// </summary>
    private void ResetRuntimePool()
    {
        PullDeckManagerData();
    }

    public List<CardData> GetCardDataList(int count)
    {
        List<CardData> result = new List<CardData>();

        if (runtimePool.Count == 0)
        {
            ResetRuntimePool();
        }

        for (int i = 0; i < count; i++)
        {
            if (runtimePool.Count == 0)
            {
                ResetRuntimePool();
            }

            if (runtimePool.Count == 0)
                break;

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

            // When only 1 entry left, refill
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
        if (library == null || library.cards == null) return null;

        foreach (var c in library.cards)
        {
            if (c.Name == name)
                return c.Data;
        }

        Debug.LogWarning($"[CardManager] Card not found in library: {name}");
        return null;
    }
}
