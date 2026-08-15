using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Enemy Level Pool")]
public class EnemyLevelPool : ScriptableObject
{
    [SerializeField] public List<EnemyLevelData> enemyLevel = new List<EnemyLevelData>();
}

[Serializable]
public struct EnemyLevelData
{
    public List<RandomnessData> Randomness;
}

[Serializable]
public struct RandomnessData
{
    public List<string> EnemyNames;
}
