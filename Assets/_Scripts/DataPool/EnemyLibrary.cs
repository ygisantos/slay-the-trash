using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Enemy Library")]
public class EnemyLibrary : ScriptableObject
{
    [SerializeField] public List<NamedEnemy> enemies = new List<NamedEnemy>();
}

[Serializable]
public struct NamedEnemy
{
    public string Name;
    public EnemyData Data;

    public NamedEnemy(string name, EnemyData data)
    {
        Name = name;
        Data = data;
    }
}
