using UnityEngine;

public class RewardManager : MonoBehaviour
{
    public static RewardManager Instance;

    [Header("Reward Sources")]
    public RewardLibrary rewardLibrary;
    public HeroView hero;
    public string rewardTxt;
    public RewardType lastRewardType;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void GiveRandomReward()
    {
        int index = Random.Range(0, rewardLibrary.rewards.Count);
        GiveReward(rewardLibrary.rewards[index]);
    }

    public void GiveReward(RewardData reward)
    {
        if (hero == null) return;
        lastRewardType = reward.rewardType;
        switch (lastRewardType)
        {
            case RewardType.AddHealth:
                AddHealthReward(hero, reward.value);
                break;

            case RewardType.AddShield:
                AddShieldReward(hero, reward.value);
                break;
        }
    }

    private void AddHealthReward(HeroView hero, int amount)
    {
        hero.AddHealth(amount);
        SetRewardText($"+{amount} Health");
    }

    private void AddShieldReward(HeroView hero, int amount)
    {
        hero.AddStatusEffect(StatusEffectType.ARMOR, amount);
        SetRewardText($"+{amount} Shield");
    }
    private void SetRewardText(string text)
    {
        rewardTxt = text;
        Debug.Log("Reward: " + text); // Optional
    }
}
