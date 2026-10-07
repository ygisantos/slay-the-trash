using System;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    private static DataManager instance;

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
            else if (pair.Value is Dictionary<string, object> ||
                     pair.Value is System.Collections.IEnumerable)
            {
                // Maps and arrays (currencies, unlocked_skills) need real serialization.
                System.Text.StringBuilder builder =
                    new System.Text.StringBuilder();

                WriteJson(builder, pair.Value);
                value = builder.ToString();
                type = "json";
            }

            cache.entries.Add(
                new ProfileEntry
                {
                    key = pair.Key,
                    value = value,
                    type = type
                }
            );

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
            case "json":
                return ReadJson(entry.value);
            case "null":
                return null;
            default:
                return entry.value;
        }
    }

    // ---- Minimal JSON for nested profile values ----

    private static void WriteJson(System.Text.StringBuilder sb, object value)
    {
        switch (value)
        {
            case null:
                sb.Append("null");
                break;
            case bool b:
                sb.Append(b ? "true" : "false");
                break;
            case string s:
                WriteJsonString(sb, s);
                break;
            case int or long or short or byte:
                sb.Append(Convert.ToInt64(value));
                break;
            case float or double or decimal:
                sb.Append(Convert.ToDouble(value).ToString("R",
                    System.Globalization.CultureInfo.InvariantCulture));
                break;
            case Dictionary<string, object> map:
                sb.Append('{');
                bool firstPair = true;
                foreach (KeyValuePair<string, object> pair in map)
                {
                    if (!firstPair) sb.Append(',');
                    firstPair = false;
                    WriteJsonString(sb, pair.Key);
                    sb.Append(':');
                    WriteJson(sb, pair.Value);
                }
                sb.Append('}');
                break;
            case System.Collections.IEnumerable list:
                sb.Append('[');
                bool firstItem = true;
                foreach (object item in list)
                {
                    if (!firstItem) sb.Append(',');
                    firstItem = false;
                    WriteJson(sb, item);
                }
                sb.Append(']');
                break;
            case Firebase.Firestore.Timestamp ts:
                WriteJsonString(sb, ts.ToDateTime().ToString("O"));
                break;
            default:
                WriteJsonString(sb, value.ToString());
                break;
        }
    }

    private static void WriteJsonString(System.Text.StringBuilder sb, string s)
    {
        sb.Append('"');
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default: sb.Append(c); break;
            }
        }
        sb.Append('"');
    }

    private static object ReadJson(string json)
    {
        int index = 0;
        return ReadJsonValue(json, ref index);
    }

    private static object ReadJsonValue(string json, ref int i)
    {
        SkipSpaces(json, ref i);
        char c = json[i];

        if (c == '{')
        {
            Dictionary<string, object> map = new Dictionary<string, object>();
            i++;
            SkipSpaces(json, ref i);

            if (json[i] == '}') { i++; return map; }

            while (true)
            {
                SkipSpaces(json, ref i);
                string key = ReadJsonString(json, ref i);
                SkipSpaces(json, ref i);
                i++; // ':'
                map[key] = ReadJsonValue(json, ref i);
                SkipSpaces(json, ref i);

                if (json[i++] == '}')
                    return map;
            }
        }

        if (c == '[')
        {
            List<object> list = new List<object>();
            i++;
            SkipSpaces(json, ref i);

            if (json[i] == ']') { i++; return list; }

            while (true)
            {
                list.Add(ReadJsonValue(json, ref i));
                SkipSpaces(json, ref i);

                if (json[i++] == ']')
                    return list;
            }
        }

        if (c == '"')
            return ReadJsonString(json, ref i);

        if (string.CompareOrdinal(json, i, "true", 0, 4) == 0) { i += 4; return true; }
        if (string.CompareOrdinal(json, i, "false", 0, 5) == 0) { i += 5; return false; }
        if (string.CompareOrdinal(json, i, "null", 0, 4) == 0) { i += 4; return null; }

        int start = i;
        while (i < json.Length && "+-0123456789.eE".IndexOf(json[i]) >= 0)
            i++;

        string number = json.Substring(start, i - start);

        if (int.TryParse(number, out int intValue))
            return intValue;

        if (long.TryParse(number, out long longValue))
            return longValue;

        return double.Parse(
            number,
            System.Globalization.CultureInfo.InvariantCulture
        );
    }

    private static string ReadJsonString(string json, ref int i)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        i++; // opening quote

        while (json[i] != '"')
        {
            char c = json[i++];

            if (c == '\\')
            {
                char escaped = json[i++];
                sb.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => escaped
                });
            }
            else
            {
                sb.Append(c);
            }
        }

        i++; // closing quote
        return sb.ToString();
    }

    private static void SkipSpaces(string json, ref int i)
    {
        while (i < json.Length && char.IsWhiteSpace(json[i]))
            i++;
    }

    public void ClearAll()
    {
        PlayerPrefs.DeleteKey(PROFILE_KEY);
        PlayerPrefs.DeleteKey(PROFILE_KEYS_KEY);
        PlayerPrefs.Save();
    }
}