using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Map Library")]
public class MapLibrary : ScriptableObject
{
    public List<NamedMap> maps = new List<NamedMap>();
}

[Serializable]
public struct NamedMap
{
    public MapType Name;
    public MapData Data;

    public NamedMap(MapType name, MapData data)
    {
        Name = name;
        Data = data;
    }
}
