using System;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    private static DataManager instance;

    private const string USERNAME_KEY = "username";
    private const string PROFILE_KEY = "user_profile";
    private const string PROFILE_KEYS_KEY = "user_profile_keys";

    [Serializable]
    private class ProfileEntry
    {
        public string key;
        public string value;
        public string type;
    }

    [Serializable]
    private class ProfileCache
    {
        public List<ProfileEntry> entries = new List<ProfileEntry>();
    }

    public static DataManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<DataManager>();

                if (instance == null)
                {
                    GameObject singletonPrefab =
                        Resources.Load<GameObject>("DataManager");

                    if (singletonPrefab == null)
                    {
                        Debug.LogError(
                            "DataManager prefab not found in Resources folder."
                        );

                        return null;
                    }

                    GameObject clone = Instantiate(singletonPrefab);
                    instance = clone.GetComponent<DataManager>();
                }
            }

            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // =========================
    // USERNAME
    // =========================

    public void SetUsername(string username)
    {
        PlayerPrefs.SetString(USERNAME_KEY, username);
        PlayerPrefs.Save();
    }

    public string GetUsername()
    {
        return PlayerPrefs.GetString(USERNAME_KEY, string.Empty);
    }

    public void SetProfile(Dictionary<string, object> profile)
    {
        if (profile == null)
        {
            ClearAll();
            return;
        }

        ProfileCache cache = new ProfileCache();

        foreach (KeyValuePair<string, object> pair in profile)
        {
            if (pair.Value == null)
            {
                cache.entries.Add(
                    new ProfileEntry
                    {
                        key = pair.Key,
                        value = string.Empty,
                        type = "null"
                    }
                );

                continue;
            }

            string value = pair.Value.ToString();
            string type = pair.Value.GetType().Name;

            if (pair.Value is bool boolValue)
                value = boolValue ? "true" : "false";
            else if (pair.Value is DateTime dateTime)
                value = dateTime.ToString("O");
            else if (pair.Value is Firebase.Firestore.Timestamp timestamp)
                value = timestamp.ToDateTime().ToString("O");
            else if (pair.Value is string stringValue)
                value = stringValue;

            cache.entries.Add(
                new ProfileEntry
                {
                    key = pair.Key,
                    value = value,
                    type = type
                }
            );

            if (pair.Key == "username")
                SetUsername(value);
        }

        string json = JsonUtility.ToJson(cache);
        PlayerPrefs.SetString(PROFILE_KEY, json);
        PlayerPrefs.Save();
    }

    public Dictionary<string, object> GetProfile()
    {
        Dictionary<string, object> profile =
            new Dictionary<string, object>();

        string json = PlayerPrefs.GetString(PROFILE_KEY, string.Empty);

        if (string.IsNullOrEmpty(json))
            return profile;

        try
        {
            ProfileCache cache =
                JsonUtility.FromJson<ProfileCache>(json);

            if (cache == null || cache.entries == null)
                return profile;

            foreach (ProfileEntry entry in cache.entries)
            {
                if (string.IsNullOrEmpty(entry.key))
                    continue;

                profile[entry.key] = ParseProfileValue(entry);
            }
        }
        catch (Exception)
        {
            Debug.LogWarning("Profile cache is invalid or corrupted.");
        }

        return profile;
    }

    private object ParseProfileValue(ProfileEntry entry)
    {
        if (entry == null)
            return null;

        if (string.IsNullOrEmpty(entry.value))
            return null;

        switch (entry.type)
        {
            case "Boolean":
                return bool.Parse(entry.value);
            case "Int32":
                return int.Parse(entry.value);
            case "Int64":
                return long.Parse(entry.value);
            case "Single":
                return float.Parse(entry.value);
            case "Double":
                return double.Parse(entry.value);
            case "DateTime":
                return DateTime.Parse(entry.value);
            case "Timestamp":
                return entry.value;
            case "null":
                return null;
            default:
                return entry.value;
        }
    }

    public void ClearUsername()
    {
        PlayerPrefs.DeleteKey(USERNAME_KEY);
        PlayerPrefs.Save();
    }

    public void ClearAll()
    {
        PlayerPrefs.DeleteKey(USERNAME_KEY);
        PlayerPrefs.DeleteKey(PROFILE_KEY);
        PlayerPrefs.DeleteKey(PROFILE_KEYS_KEY);
        PlayerPrefs.Save();
    }
}