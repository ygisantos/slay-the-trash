using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum MapType
{
    Enemy,
    Heal,
    Event,
    Boss
}

[CreateAssetMenu(menuName = "Data/Map Data")]
public class MapData : ScriptableObject
{
    [Header("Map Visuals")]
    public Sprite mainImage;
    [Header("Map Properties")]
    public MapType type;
    public int healthReward = 5;
}
