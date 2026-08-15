using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Map Level Pool")]
public class MapLevelPool : ScriptableObject
{
    [SerializeField] public List<MapLevelData> mapLevels = new List<MapLevelData>();
}

[Serializable]
public struct MapLevelData
{
    public List<MapType> MapNames;
}
