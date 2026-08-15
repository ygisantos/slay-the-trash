using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }  //PascalCase na

    protected virtual void Awake()
    {
        if (Instance != null)  //PascalCase na
        {
            Destroy(gameObject);
            return;
        }
        Instance = this as T;  //PascalCase na
    }

    protected virtual void OnApplicationQuit()  // Fixed typo:"OnApplicationOut"
    {
        Instance = null;  //PascalCase na
        Destroy(gameObject);
    }
}

public abstract class PersistentSingleton<T> : Singleton<T> where T : MonoBehaviour
{
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }
}