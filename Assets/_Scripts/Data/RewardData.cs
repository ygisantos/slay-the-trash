using UnityEngine;

public enum RewardType
{
    AddHealth,
    AddShield
}

[System.Serializable]
public struct RewardData
{
    public RewardType rewardType;
    public int value;

    public RewardData(RewardType type, int amount)
    {
        rewardType = type;
        value = amount;
    }
}
