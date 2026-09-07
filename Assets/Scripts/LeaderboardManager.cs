using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    private FBLeaderboard leaderboard;

    [Header("Leaderboard Filter")]
    [SerializeField] private TMP_Dropdown leaderboardDropdown;

    [Header("Top 15 Rows")]
    [SerializeField] private TMP_Text[] rankLabels = new TMP_Text[15];
    [SerializeField] private TMP_Text[] usernameLabels = new TMP_Text[15];
    [SerializeField] private TMP_Text[] scoreLabels = new TMP_Text[15];
    [SerializeField] private TMP_Text currentPlayerIndicator;

    private const int TopFifteenPage = 1;
    private const int TopFifteenPageSize = 15;
    private string currentSortField;

    private void Awake()
    {
        if (leaderboard == null)
            leaderboard = FBLeaderboard.Instance;

        if (leaderboardDropdown != null)
        {
            leaderboardDropdown.onValueChanged.AddListener(
                OnLeaderboardDropdownChanged
            );
        }

        SetDefaultLeaderboardOption();
        LoadTopFifteen("Total Trash");
    }

    private void OnDestroy()
    {
        if (leaderboardDropdown != null)
        {
            leaderboardDropdown.onValueChanged.RemoveListener(
                OnLeaderboardDropdownChanged
            );
        }
    }

    public void OnLeaderboardDropdownChanged(int optionIndex)
    {
        if (leaderboardDropdown == null ||
            optionIndex < 0 ||
            optionIndex >= leaderboardDropdown.options.Count)
        {
            ShowError("Invalid leaderboard option.");
            return;
        }

        LoadTopFifteen(
            leaderboardDropdown.options[optionIndex].text
        );
    }

    // Kept as a compatibility entry point for existing button or code references.
    public void LoadTopTen(string leaderboardField)
    {
        LoadTopFifteen(leaderboardField);
    }

    public void LoadTopFifteen(string leaderboardField)
    {
        string sortField = NormalizeLeaderboardField(leaderboardField);
        if (string.IsNullOrEmpty(sortField))
        {
            ShowError("Invalid leaderboard selected.");
            return;
        }

        if (leaderboard == null)
        {
            ShowError("Leaderboard service is not available.");
            return;
        }

        currentSortField = sortField;
        ClearRows();
        ShowLoading();

        leaderboard.ReadLeaderboard(
            sortField,
            TopFifteenPage,
            TopFifteenPageSize,
            DisplayRows,
            ShowError
        );
    }

    public void RefreshLeaderboard()
    {
        if (string.IsNullOrEmpty(currentSortField))
        {
            LoadTopFifteen("Total Trash");
            return;
        }

        ClearRows();
        ShowLoading();
        leaderboard.ReadLeaderboard(
            currentSortField,
            TopFifteenPage,
            TopFifteenPageSize,
            DisplayRows,
            ShowError
        );
    }

    private void DisplayRows(
        List<Dictionary<string, object>> rows,
        int resultCount)
    {
        HideLoading();
        SetCurrentPlayerIndicator(rows);

        for (int index = 0; index < rows.Count && index < 15; index++)
        {
            Dictionary<string, object> row = rows[index];
            SetLabel(rankLabels, index, (index + 1).ToString());
            SetLabel(
                usernameLabels,
                index,
                FirebaseDataHelper.GetString(row, "username") ?? "Unknown"
            );
            SetLabel(
                scoreLabels,
                index,
                FirebaseDataHelper.GetString(row, currentSortField) ?? "0"
            );
        }
    }

    private string NormalizeLeaderboardField(string field)
    {
        if (string.IsNullOrWhiteSpace(field))
            return null;

        string normalized = field.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "total trash":
                return "total_trash";
            case "non-biodegradable":
                return "non_bio_trash";
            case "biodegradable":
                return "bio_trash";
            case "recyclable":
                return "recicled_trash";
            case "dalies":
                return "dailies";
            case "dungeon 1":
                return "dun1";
            case "dungeon 2":
                return "dun2";
            case "dungeon 3":
                return "dun3";
            case "dungeon 4":
                return "dun4";
            case "dungeon 5":
                return "dun5";
            default:
                return null;
        }
    }

    private void ClearRows()
    {
        for (int index = 0; index < 15; index++)
        {
            SetLabel(rankLabels, index, (index + 1).ToString());
            SetLabel(usernameLabels, index, "-");
            SetLabel(scoreLabels, index, "-");
        }

        SetLabel(currentPlayerIndicator, "Not in the top 15");
    }

    private void SetCurrentPlayerIndicator(
        List<Dictionary<string, object>> rows)
    {
        if (currentPlayerIndicator == null)
            return;

        string currentUserId = FBAuthentication.Instance != null
            ? FBAuthentication.Instance.CurrentUserId
            : null;
        string currentUsername =
            FBAuthentication.Instance != null &&
            FBAuthentication.Instance.CurrentProfile != null
                ? FirebaseDataHelper.GetString(
                    FBAuthentication.Instance.CurrentProfile,
                    "username"
                )
                : null;

        for (int index = 0; index < rows.Count && index < 15; index++)
        {
            Dictionary<string, object> row = rows[index];
            string rowUserId = FirebaseDataHelper.GetString(row, "userId");
            string rowUsername = FirebaseDataHelper.GetString(row, "username");

            if ((!string.IsNullOrEmpty(currentUserId) &&
                 rowUserId == currentUserId) ||
                (!string.IsNullOrEmpty(currentUsername) &&
                 string.Equals(
                     rowUsername,
                     currentUsername,
                     StringComparison.OrdinalIgnoreCase
                 )))
            {
                currentPlayerIndicator.text =
                    "You are ranked #" + (index + 1);
                return;
            }
        }

        currentPlayerIndicator.text = "Not in the top 15";
    }

    private void ShowLoading()
    {
        LoadingScreenManager.Instance.Show("Loading leaderboard...");
    }

    private void HideLoading()
    {
        LoadingScreenManager.Instance.Hide();
    }

    private void SetDefaultLeaderboardOption()
    {
        if (leaderboardDropdown == null)
            return;

        for (int index = 0;
            index < leaderboardDropdown.options.Count;
            index++)
        {
            if (NormalizeLeaderboardField(
                    leaderboardDropdown.options[index].text
                ) == "total_trash")
            {
                leaderboardDropdown.SetValueWithoutNotify(index);
                return;
            }
        }
    }

    private void SetLabel(TMP_Text[] labels, int index, string value)
    {
        if (labels != null && index < labels.Length && labels[index] != null)
            labels[index].text = value;
    }

    private void SetLabel(TMP_Text label, string value)
    {
        if (label != null)
            label.text = value;
    }

    private void ShowError(string message)
    {
        HideLoading();
        Debug.LogError(message);

        if (DialogueManager.Instance != null)
            DialogueManager.Instance.ShowErrorDialog(message);
    }
}
