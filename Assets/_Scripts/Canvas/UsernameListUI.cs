using UnityEngine;
using System;
using TMPro;

public class UsernameListUI : MonoBehaviour
{
    public Transform contentRoot;
    public GameObject usernameButtonPrefab;

    public Action<string> onSelectUsername;

    public void Populate(string[] usernames)
    {
        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);

        foreach (var name in usernames)
        {
            if (string.IsNullOrEmpty(name)) continue;

            var buttonObj = Instantiate(usernameButtonPrefab, contentRoot);
            var btn = buttonObj.GetComponent<UsernameButton>();

            btn.Setup(name, OnClicked);
        }
    }
    public void PopulateEmpty()
    {
        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);

        // Create a non-clickable grey text message
        var placeholder = new GameObject("EmptyMessage", typeof(RectTransform));
        placeholder.transform.SetParent(contentRoot);

        var text = placeholder.AddComponent<TextMeshProUGUI>();
        text.text = "No saved usernames";
        text.fontSize = 32;
        text.color = new Color(0.7f, 0.7f, 0.7f);
        text.alignment = TMPro.TextAlignmentOptions.Center;
    }


    void OnClicked(string username)
    {
        onSelectUsername?.Invoke(username);
    }
}
