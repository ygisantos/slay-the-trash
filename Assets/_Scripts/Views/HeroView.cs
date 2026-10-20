using UnityEngine;

public class HeroView : CombatantView
{
    public void Setup(HeroData heroData)
    {
        SetupBase(
            CharacterStats.ScaleHealth(heroData.Health),
            heroData.Image,
            SoundType.PLAYERHURT
        );
    }

}
