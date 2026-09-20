using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class LogInManager : MonoBehaviour
{
    public static LogInManager Instance;

    private const int MaxUsers = 20;

    private List<string> usernames = new List<string>();
    private List<string> hashedPasswords = new List<string>();

    // Backed by DataManager's cached profile instead of its own storage.
    public string CurrentUsername
    {
        get => FirebaseDataHelper.GetString(DataManager.Instance?.GetProfile(), "username");
        set
        {
            Dictionary<string, object> profile =
                DataManager.Instance?.GetProfile() ?? new Dictionary<string, object>();

            profile["username"] = value;
            DataManager.Instance?.SetProfile(profile);
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();

            if (!string.IsNullOrEmpty(CurrentUsername))
            {
                Transitioner.Instance.TransitionToScene("MainMenuScene");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // -----------------------------------------------------------------------------
    // PUBLIC FUNCTIONS
    // -----------------------------------------------------------------------------

    /// <summary>
    /// Adds or updates a user with hashed password
    /// </summary>
    public void AddOrUpdateUser(string username, string rawPassword)
    {
        string hash = HashPassword(rawPassword);
        int index = usernames.IndexOf(username);

        if (index >= 0)
        {
            // Update existing user password
            hashedPasswords[index] = hash;
        }
        else
        {
            if (usernames.Count >= MaxUsers)
            {
                Debug.LogWarning("Cannot add more than 20 users!");
                return;
            }

            usernames.Add(username);
            hashedPasswords.Add(hash);
        }

        SaveData();
    }

    /// <summary>
    /// Validates login by comparing hashed passwords.
    /// </summary>
    public bool ValidateLogin(string username, string rawPassword)
    {
        int index = usernames.IndexOf(username);

        if (index >= 0)
        {
            string inputHash = HashPassword(rawPassword);

            if (inputHash == hashedPasswords[index])
            {
                CurrentUsername = username;
                SaveData();
                return true;
            }
        }

        return false;
    }

    // -----------------------------------------------------------------------------
    // PASSWORD HASHING
    // -----------------------------------------------------------------------------

    private string HashPassword(string password)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[] hash = sha.ComputeHash(bytes);

            // Convert to readable hex string
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)            
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }
    }

    // -----------------------------------------------------------------------------
    // SAVE & LOAD
    // -----------------------------------------------------------------------------

    private void SaveData()
    {
        PlayerPrefs.SetInt("UserCount", usernames.Count);

        for (int i = 0; i < usernames.Count; i++)
        {
            PlayerPrefs.SetString("User" + i, usernames[i]);
            PlayerPrefs.SetString("PassHash" + i, hashedPasswords[i]);
        }

        PlayerPrefs.Save();
    }

    private void LoadData()
    {
        usernames.Clear();
        hashedPasswords.Clear();

        int count = PlayerPrefs.GetInt("UserCount", 0);

        for (int i = 0; i < count; i++)
        {
            usernames.Add(PlayerPrefs.GetString("User" + i, ""));
            hashedPasswords.Add(PlayerPrefs.GetString("PassHash" + i, ""));
        }
    }
}