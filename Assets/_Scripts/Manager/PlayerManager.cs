using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [SerializeField] private HeroData heroData;
    [SerializeField] private PerkData perkData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public HeroData GetHeroData()
    {
        return heroData;
    }

    public PerkData GetPerkData()
    {
        return perkData;
    }
}
