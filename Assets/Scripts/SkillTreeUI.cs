using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

public class SkillTreeUI : MonoBehaviour
{
    [Serializable]
    public class SkillData
    {
        public string id;
        public string skillName;

        [TextArea(2, 4)]
        public string description;

        public string effect;
        public int cost;
        public int layer;

        public List<string> requiredSkills;

        // UI-only fields
        [NonSerialized]
        public Sprite iconSprite;

        public Button clickableObject;
    }

    [Serializable]
    private class SkillTreeData
    {
        public List<SkillData> scoringSkills;
        public List<SkillData> gameplaySkills;
    }

    public List<SkillData> scoringSkills = new();
    public List<SkillData> gameplaySkills = new();

    [Header("Skill Information UI")]
    [SerializeField] private TextMeshProUGUI skillTreeTitle;
    [SerializeField] private TextMeshProUGUI skillTreeDescription;
    [SerializeField] private TextMeshProUGUI skillTreeEffect;
    [SerializeField] private TextMeshProUGUI skillTreeCost;


    // =========================================================
    // LOAD JSON
    // Call this manually when you want to load the skill data.
    // =========================================================


    [Button]
    public void LoadSkillData()
    {
        string path = Path.Combine(
            Application.streamingAssetsPath,
            "skill_tree.json"
        );

        if (!File.Exists(path))
        {
            Debug.LogError($"Skill tree JSON not found: {path}");
            return;
        }

        string json = File.ReadAllText(path);

        SkillTreeData data = JsonUtility.FromJson<SkillTreeData>(json);

        scoringSkills = data.scoringSkills;
        gameplaySkills = data.gameplaySkills;

        Debug.Log(
            $"Loaded {scoringSkills.Count} scoring skills " +
            $"and {gameplaySkills.Count} gameplay skills."
        );
    }


    // =========================================================
    // SETUP BUTTONS
    // =========================================================

    public void SetupSkillButtons()
    {
        SetupSkillList(scoringSkills);
        SetupSkillList(gameplaySkills);
    }

    private void SetupSkillList(List<SkillData> skills)
    {
        foreach (SkillData skill in skills)
        {
            if (skill.clickableObject == null)
            {
                Debug.LogWarning(
                    $"No Button assigned for skill: {skill.id}"
                );

                continue;
            }

            // Prevent duplicate listeners if SetupSkillButtons()
            // is called multiple times.
            skill.clickableObject.onClick.RemoveAllListeners();

            SkillData selectedSkill = skill;

            skill.clickableObject.onClick.AddListener(
                () => ShowSkillInfo(selectedSkill)
            );
        }
    }


    // =========================================================
    // SHOW SKILL INFORMATION
    // =========================================================

    public void ShowSkillInfo(SkillData skill)
    {
        if (skill == null)
            return;

        if (skillTreeTitle != null)
            skillTreeTitle.text = skill.skillName;

        if (skillTreeDescription != null)
            skillTreeDescription.text = skill.description;

        if (skillTreeEffect != null)
            skillTreeEffect.text = skill.effect;

        if (skillTreeCost != null)
            skillTreeCost.text = skill.cost.ToString();
    }
}