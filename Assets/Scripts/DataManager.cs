using UnityEngine;

public class DataManager : MonoBehaviour
{
    private static DataManager instance;

    private const string USERNAME_KEY = "username";

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

    public void ClearUsername()
    {
        PlayerPrefs.DeleteKey(USERNAME_KEY);
        PlayerPrefs.Save();
    }
}