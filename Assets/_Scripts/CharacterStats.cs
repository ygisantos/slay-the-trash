using System.Collections.Generic;
using UnityEngine;

// Per-character gameplay modifiers, shared by the character-select screen and combat.
// Index order matches CharacterSelector.characters and CharacterSwapper.CharacterType:
//   0 Witch, 1 Knight, 2 Hunter, 3 Cleric
public static class CharacterStats
{
    public struct Profile
    {
        public string Name;
        public string Epithet;
        public string Blurb;
        public float HealthMultiplier;
        public float ShieldMultiplier;
        public float DamageMultiplier;
    }

    private static readonly Profile[] Profiles =
    {
        new Profile
        {
            Name = "Witch",
            Epithet = "Balanced",
            Blurb = "Even across the board. No glaring weakness, no lopsided strength.",
            HealthMultiplier = 1f,
            ShieldMultiplier = 1f,
            DamageMultiplier = 1f,
        },
        new Profile
        {
            Name = "Knight",
            Epithet = "Bulwark",
            Blurb = "Built to outlast. Trades striking power for a much deeper health pool.",
            HealthMultiplier = 1.5f,
            ShieldMultiplier = 1.25f,
            DamageMultiplier = 0.75f,
        },
        new Profile
        {
            Name = "Hunter",
            Epithet = "Glass Cannon",
            Blurb = "Hits hardest and folds fastest. Spend your shield before your health.",
            HealthMultiplier = 0.5f,
            ShieldMultiplier = 1f,
            DamageMultiplier = 1.5f,
        },
        new Profile
        {
            Name = "Cleric",
            Epithet = "Warden",
            Blurb = "Sturdy and heavily shielded, but swings light. Wins long fights.",
            HealthMultiplier = 1.25f,
            ShieldMultiplier = 1.5f,
            DamageMultiplier = 0.75f,
        },
    };

    public static int Count => Profiles.Length;

    // Falls back to the Witch so an unset selection never yields zeroed stats.
    public static int CurrentIndex
    {
        get
        {
            int index = SelectedCharacter.index;
            return (index >= 0 && index < Profiles.Length) ? index : 0;
        }
    }

    public static Profile Current => Profiles[CurrentIndex];

    public static Profile Get(int index)
    {
        if (index < 0 || index >= Profiles.Length)
            return Profiles[0];
        return Profiles[index];
    }

    // ---- gameplay scaling (uses the currently selected character) ----

    public static int ScaleHealth(int baseHealth) =>
        Scale(baseHealth, Current.HealthMultiplier);

    public static int ScaleShield(int shield) =>
        Scale(shield, Current.ShieldMultiplier);

    public static int ScaleDamage(int damage) =>
        Scale(damage, Current.DamageMultiplier);

    private static int Scale(int value, float multiplier)
    {
        if (value <= 0)
            return value;

        int scaled = Mathf.RoundToInt(value * multiplier);
        return Mathf.Max(1, scaled);
    }

    // Card effects always report the hero as the caster (see EffectSystem), so the
    // caster alone cannot tell "hero hits an enemy" from "hero hurts itself".
    // Damage is only scaled when the action is aimed at something other than the hero.
    public static bool IsHeroAttack(CombatantView caster, List<CombatantView> targets)
    {
        if (!(caster is HeroView))
            return false;

        if (targets == null || targets.Count == 0)
            return false;

        for (int i = 0; i < targets.Count; i++)
        {
            CombatantView target = targets[i];
            if (target == null)
                continue;

            if (target is HeroView)
                return false;
        }

        return true;
    }

    // ---- display ----

    // Rich-text block for the character-select description panel.
    public static string Describe(int index)
    {
        Profile profile = Get(index);

        return
            $"<size=115%><b>{profile.Name}</b></size>  " +
            $"<size=80%><color=#93A7BC>{profile.Epithet}</color></size>\n\n" +
            $"Health    {Stat(profile.HealthMultiplier)}\n" +
            $"Shield    {Stat(profile.ShieldMultiplier)}\n" +
            $"Damage    {Stat(profile.DamageMultiplier)}\n\n" +
            $"<size=85%><color=#B9C6D4>{profile.Blurb}</color></size>";
    }

    private static string Stat(float multiplier)
    {
        string value = "x" + multiplier.ToString("0.00");

        if (Mathf.Approximately(multiplier, 1f))
            return $"<color=#C8D3DE>{value}</color>";

        return multiplier > 1f
            ? $"<color=#6BE38A>{value}</color>"
            : $"<color=#E8705A>{value}</color>";
    }
}
