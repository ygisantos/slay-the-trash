using System.Collections.Generic;

public static class FirebaseDataHelper
{
    public static string GetString(
        Dictionary<string, object> data,
        string key)
    {
        if (data == null)
            return null;

        if (!data.TryGetValue(key, out object value))
            return null;

        return value?.ToString();
    }

    public static int GetInt(
        Dictionary<string, object> data,
        string key)
    {
        if (data == null || !data.TryGetValue(key, out object value))
            return 0;

        return value is long longValue
            ? (int)longValue
            : int.TryParse(value?.ToString(), out int result) ? result : 0;
    }

    public static bool GetBool(
        Dictionary<string, object> data,
        string key)
    {
        if (data == null)
            return false;

        if (!data.TryGetValue(key, out object value))
            return false;

        if (value is bool boolValue)
            return boolValue;

        return bool.TryParse(
            value?.ToString(),
            out bool result
        ) && result;
    }
}
