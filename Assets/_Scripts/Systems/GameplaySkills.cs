using System.Collections.Generic;

// Reads the unlocked gameplay skills from the player's profile.
public static class GameplaySkills
{
    public const int BaseHandSize = 5;

    private static HashSet<string> unlocked;

    public static void Refresh()
    {
        if (FBAuthentication.Instance == null)
        {
            unlocked = null;
            return;
        }

        unlocked = new HashSet<string>(FBAuthentication.Instance.GetUnlockedSkillIds());
    }

    private static bool Has(string id)
    {
        if (unlocked == null)
            Refresh();

        return unlocked != null && unlocked.Contains(id);
    }

    public static int BonusMaxHealth => Has("green_vitality") ? 15 : 0;
    public static int StartShield => Has("eco_barrier") ? 15 : 0;
    public static int ShieldPerBattle => Has("circular_defense") ? 5 : 0;
    public static int HealthPerBattle => Has("natures_renewal") ? 5 : 0;
    public static int BonusMana => Has("energy_reserve") ? 1 : 0;
    public static int HealthPerCard => Has("life_cycle") ? 1 : 0;
    public static int HandSize => BaseHandSize + (Has("knowledge_bank") ? 1 : 0);

    // One-line summary of the always-on skills, or null if none are unlocked.
    public static string DescribeActive()
    {
        List<string> parts = new List<string>();

        if (BonusMaxHealth > 0) parts.Add("+" + BonusMaxHealth + " Health");
        if (StartShield > 0) parts.Add("+" + StartShield + " Shield");
        if (BonusMana > 0) parts.Add("+" + BonusMana + " Mana");
        if (HandSize > BaseHandSize) parts.Add("+" + (HandSize - BaseHandSize) + " Card");

        return parts.Count > 0 ? "Skills active: " + string.Join(", ", parts) : null;
    }
}
