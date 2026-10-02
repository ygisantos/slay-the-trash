using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DungeonsHandler : MonoBehaviour
{
    [HideInInspector] public int plasticCards = 0;
    [HideInInspector] public int paperCards = 0;
    [HideInInspector] public int foodwasteCards = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string username = GetCurrentUsername();
        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogWarning("Cannot load card collection without a logged-in username.");
            return;
        }

        FBCardCollection.Instance.GetDungeonCounters(
            username,
            (plasticCount, paperCount, foodWasteCount) =>
            {
                plasticCards = plasticCount;
                paperCards = paperCount;
                foodwasteCards = foodWasteCount;
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

