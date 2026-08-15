using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Reward Library", fileName = "RewardLibrary")]
public class RewardLibrary : ScriptableObject
{
    public List<RewardData> rewards = new List<RewardData>();
}
