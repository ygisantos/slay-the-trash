using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    public int level = 0;
    public MapLibrary library;
    public MapLevelPool levelPool;
    public bool isBossLevel = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // ------------------------------------------------------------------
    // Get MapData at specific level + column
    // ------------------------------------------------------------------
    public MapData GetMapData(int level, int column)
    {
        // Validate level
        if (level < 0 || level >= levelPool.mapLevels.Count)
        {
            Debug.LogWarning("MapManager: Level out of range → " + level);
            return null;
        }

        // Validate column
        MapLevelData levelData = levelPool.mapLevels[level];
        if (column < 0 || column >= levelData.MapNames.Count)
        {
            Debug.LogWarning("MapManager: Column out of range → " + column);
            return null;
        }

        // Get the map type from pool
        MapType targetType = levelData.MapNames[column];

        // Find matching map in MapLibrary
        foreach (var map in library.maps)
        {
            if (map.Name == targetType)
                return map.Data;
        }

        Debug.LogWarning("MapManager: No MapLibrary entry found for: " + targetType);
        return null;
    }
}
