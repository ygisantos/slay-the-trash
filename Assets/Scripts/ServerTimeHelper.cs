using System;
using System.Collections;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.Networking;

public static class ServerTimeHelper
{
    private const string TimeApi =
        "https://timeapi.io/api/Time/current/zone?timeZone=UTC";

    private static DateTime serverTimeAtSync;
    private static float realtimeAtSync;

    public static bool IsReady =>
        serverTimeAtSync != default;

    public static DateTime GetUtcNow()
    {
        if (!IsReady)
            return DateTime.UtcNow;

        float elapsed =
            Time.realtimeSinceStartup - realtimeAtSync;

        return serverTimeAtSync.AddSeconds(elapsed);
    }

    public static Timestamp GetFirestoreTimestamp()
    {
        return Timestamp.FromDateTime(GetUtcNow());
    }

    public static IEnumerator SyncServerTimeCoroutine(
        Action<bool> onComplete = null)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(TimeApi))
        {
            request.timeout = 10;

            yield return request.SendWebRequest();

            bool success = false;

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;

                try
                {
                    TimeApiResponse response =
                        JsonUtility.FromJson<TimeApiResponse>(json);

                    if (response != null &&
                        !string.IsNullOrEmpty(response.dateTime))
                    {
                        serverTimeAtSync = DateTime.Parse(
                            response.dateTime,
                            null,
                            System.Globalization.DateTimeStyles.RoundtripKind
                        ).ToUniversalTime();

                        realtimeAtSync = Time.realtimeSinceStartup;
                        success = true;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"Failed to parse server time: {e.Message}"
                    );
                }
            }

            if (!success)
            {
                Debug.LogError(
                    $"Failed to get server time: {request.error}"
                );
            }

            onComplete?.Invoke(success);
        }
    }

    [Serializable]
    private class TimeApiResponse
    {
        public string dateTime;
    }
}
