using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [SerializeField] private EnemyLevelPool pool;
    [SerializeField] private EnemyLibrary library;

    [HideInInspector] public bool isLevelCleared = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public List<EnemyData> GetEnemyDataList(int targetLevel)
    {
        // 1. Get enemy names from the EnemyLevelPool using the level parameter
        List<string> enemyNames = GetEnemyNamesFromPool(targetLevel);

        List<EnemyData> result = new List<EnemyData>();

        // 2. Match names to EnemyData from EnemyLibrary ScriptableObject
        foreach (string enemyName in enemyNames)
        {
            foreach (var ne in library.enemies)
            {
                if (ne.Name == enemyName)
                {
                    result.Add(ne.Data);
                    break;
                }
            }
        }
        isLevelCleared = result.Count > 0 ? false : true;
        return result;
    }

    private List<string> GetEnemyNamesFromPool(int targetLevel)
    {
        List<string> names = new List<string>();

        if (pool.enemyLevel.Count == 0)
            return names;

        // Clamp target level properly
        int index = Mathf.Clamp(targetLevel - 1, 0, pool.enemyLevel.Count - 1);

        if (pool.enemyLevel[index].Randomness.Count > 0)
        {
            names.AddRange(pool.enemyLevel[index].Randomness[0].EnemyNames);
        }

        return names;
    }
}
