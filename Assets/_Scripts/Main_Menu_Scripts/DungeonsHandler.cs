using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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

                Debug.Log($"[Dungeons] Saved cards: {cards.Count} (plastic {ownedPlastic}, paper {ownedPaper}, food waste {ownedFoodWaste}).");
            },
            error => Debug.LogError($"Failed to load owned cards: {error}")
        );
    }

    // The profile can finish loading after this scene starts, so wait for the username.
    void Start()
    {
        StartCoroutine(LoadWhenReady());
    }

    private IEnumerator LoadWhenReady()
    {
        float waited = 0f;
        while (string.IsNullOrWhiteSpace(GetCurrentUsername()) && waited < 10f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        string username = GetCurrentUsername();
        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogWarning("[Dungeons] No logged-in username found; dungeon progress was not loaded.");
            yield break;
        }

        RefreshOwnedCards();

        FBLeaderboard.Instance.Read(
            username,
            points =>
            {
                plasticCards = FirebaseDataHelper.GetInt(points, "plastic_scan_count");
                paperCards = FirebaseDataHelper.GetInt(points, "paper_scan_count");
                foodwasteCards = FirebaseDataHelper.GetInt(points, "food_waste_scan_count");

                Debug.Log($"[Dungeons] Scan counters for '{username}': plastic {plasticCards}, paper {paperCards}, food waste {foodwasteCards}.");
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

