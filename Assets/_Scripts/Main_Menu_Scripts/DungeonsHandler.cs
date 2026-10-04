using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DungeonsHandler : MonoBehaviour
{
    [HideInInspector] public int plasticCards = 0;
    [HideInInspector] public int paperCards = 0;
    [HideInInspector] public int foodwasteCards = 0;
    [HideInInspector] public int ownedPlastic = 0;
    [HideInInspector] public int ownedPaper = 0;
    [HideInInspector] public int ownedFoodWaste = 0;

    void OnEnable()
    {
        DeckManager.OnDeckUpdated += RefreshOwnedCards;
    }

    void OnDisable()
    {
        DeckManager.OnDeckUpdated -= RefreshOwnedCards;
    }

    private void RefreshOwnedCards()
    {
        string username = GetCurrentUsername();
        if (string.IsNullOrWhiteSpace(username))
            return;

        FBCardCollection.Instance.GetCards(
            username,
            cards =>
            {
                ownedPlastic = ownedPaper = ownedFoodWaste = 0;

                foreach (var card in cards)
                {
                    switch (card.trashType?.Trim().ToLowerInvariant())
                    {
                        case "plastic":
                        case "plastic bottle": ownedPlastic++; break;
                        case "paper": ownedPaper++; break;
                        case "food waste": ownedFoodWaste++; break;
                    }
                }
            },
            error => Debug.LogError($"Failed to load owned cards: {error}")
        );
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RefreshOwnedCards();

        string username = GetCurrentUsername();
        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogWarning("Cannot load card collection without a logged-in username.");
            return;
        }

        FBLeaderboard.Instance.Read(
            username,
            points =>
            {
                plasticCards = FirebaseDataHelper.GetInt(points, "plastic_scan_count");
                paperCards = FirebaseDataHelper.GetInt(points, "paper_scan_count");
                foodwasteCards = FirebaseDataHelper.GetInt(points, "food_waste_scan_count");
            },
            error => Debug.LogError($"Failed to load dungeon counters: {error}")
        );
    }

    private string GetCurrentUsername()
    {
        Dictionary<string, object> profile =
            FBAuthentication.Instance != null
                ? FBAuthentication.Instance.CurrentProfile
                : null;

        if (profile == null && DataManager.Instance != null)
            profile = DataManager.Instance.GetProfile();

        return FirebaseDataHelper.GetString(profile, "username");
    }

    // Update is called once per frame
    void Update()
    {

    }

}

