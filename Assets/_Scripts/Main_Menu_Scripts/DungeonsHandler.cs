using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

public class DungeonsHandler : MonoBehaviour
{
    [HideInInspector] public int plasticCards = 0;
    [HideInInspector] public int paperCards = 0;
    [HideInInspector] public int foodwasteCards = 0;

    private List<string> cards;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string username = GetCurrentUsername();
        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogWarning("Cannot load card collection without a logged-in username.");
            return;
        }

        FBCardCollection.Instance.GetCards(
            username,
            entries =>
            {
                // Keep the "trashType:cardName:rarity" shape CheckDeck already expects.
                cards = entries
                    .Select(entry => $"{entry.trashType}:{entry.cardName}:{entry.rarity}")
                    .ToList();

                CheckDeck();
            },
            error => Debug.LogError($"Failed to load card collection: {error}")
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

    void CheckDeck()
    {
        foreach (string card in cards)
        {
            string[] cardSplit = card.Split(':');
            if (cardSplit[0] == "plastic bottle")
                plasticCards++;
            else if (cardSplit[0] == "paper")
                paperCards++;
            else if (cardSplit[0] == "food waste")
                foodwasteCards++;
            else
                Debug.LogWarning($"Card type of {cardSplit[1]} Not Found. Skipping Card...");
        }
    }
}

