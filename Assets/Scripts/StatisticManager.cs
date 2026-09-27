using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StatisticManager : MonoBehaviour
{
    [SerializeField] private GameObject[] statCell;

    // The 10 PointFields from FBLeaderboard, in order matching the stat cells
    private static readonly string[] StatFields =
    {
        "total_trash",
        "recicled_trash",
        "non_bio_trash",
        "bio_trash",
        "dailies",
        "dun1",
        "dun2",
        "dun3",
        "dun4",
        "dun5"
    };

    public void startStatCell()
    {
        Dictionary<string, object> profile = DataManager.Instance.GetProfile();

        if (profile == null || !profile.TryGetValue("username", out object usernameObj) || usernameObj == null)
        {
            Debug.LogWarning("StatisticManager: No username found in profile.");
            return;
        }

        string username = usernameObj.ToString();

        FBLeaderboard.Instance.Read(
            username,
            onSuccess: pointsData => setStatCells(pointsData),
            onError: err => Debug.LogError("StatisticManager: Failed to load stats — " + err)
        );
    }


    public void setStatCells(Dictionary<string, object> pointsData)
    {
        for (int i = 0; i < statCell.Length; i++)
        {
            TextMeshProUGUI textMesh = statCell[i].transform.GetChild(1).GetComponent<TextMeshProUGUI>();


            string field = i < StatFields.Length ? StatFields[i] : string.Empty;
            string value = "0";

            if (!string.IsNullOrEmpty(field) && pointsData != null && pointsData.TryGetValue(field, out object raw) && raw != null)
                value = raw.ToString();

            textMesh.text = value;
        }
    }
}
