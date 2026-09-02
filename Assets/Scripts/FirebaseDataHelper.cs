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
