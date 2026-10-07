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

        // Runtime state
        [NonSerialized]
        public bool isUnlocked;

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

    [Header("Skill Visuals")]
    [SerializeField] private Color unlockedSkillColor = Color.white;
    [SerializeField] private Color availableSkillColor = new Color(0.55f, 1f, 0.55f, 1f);
    [SerializeField] private Color nextSkillColor = new Color(1f, 0.85f, 0.45f, 1f);
    [SerializeField] private Color lockedSkillColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    [Header("Skill Information UI")]
    [SerializeField] private TextMeshProUGUI skillTreeTitle;
    [SerializeField] private TextMeshProUGUI skillTreeDescription;
    [SerializeField] private TextMeshProUGUI skillTreeEffect;
    [SerializeField] private TextMeshProUGUI skillTreeCost;

    [Header("Skill Buttons")]
    [SerializeField] private Button unlockSkillButton;
    [SerializeField] private TextMeshProUGUI unlockSkillButtonText;

    [SerializeField] private Button respecButton;

    [Header("Water")]
    [SerializeField] private TextMeshProUGUI waterText;

    [Header("Panels")]
    [SerializeField] private GameObject scoringPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject scoringTab;
    [SerializeField] private GameObject gameplayTab;

    private bool isScoringPanelActive = false;

    [SerializeField] private Color activeTabColor;
    [SerializeField] private Color inactiveTabColor;

    // Available water comes from the Firebase profile
    private int water =>
        FBAuthentication.Instance != null
            ? FBAuthentication.Instance.GetAvailableWater()
            : 0;

    private bool isSaving;

    // Currently selected skill
    private SkillData selectedSkill;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        scoringPanel.SetActive(true);
        gameplayPanel.SetActive(false);

        scoringTab.GetComponent<Image>().color = activeTabColor;
        gameplayTab.GetComponent<Image>().color = inactiveTabColor;

        isScoringPanelActive = true;

        ClearSkillInfo();

        UpdateWaterUI();

        scoringTab.GetComponent<Button>().interactable =
            !isScoringPanelActive;

        gameplayTab.GetComponent<Button>().interactable =
            isScoringPanelActive;

        // Unlock button
        if (unlockSkillButton != null)
        {
            unlockSkillButton.onClick.RemoveAllListeners();
            unlockSkillButton.onClick.AddListener(UnlockSelectedSkill);
        }

        // Respec button
        if (respecButton != null)
        {
            respecButton.onClick.RemoveAllListeners();
            respecButton.onClick.AddListener(ConfirmRespec);
        }

        ApplySavedUnlocks();
        UpdateWaterUI();
        UpdateAllSkillVisuals();
    }

    private void ApplySavedUnlocks()
    {
        if (FBAuthentication.Instance == null)
            return;

        List<string> saved = FBAuthentication.Instance.GetUnlockedSkillIds();

        foreach (SkillData skill in scoringSkills)
            if (skill != null)
                skill.isUnlocked = saved.Contains(skill.id);

        foreach (SkillData skill in gameplaySkills)
            if (skill != null)
                skill.isUnlocked = saved.Contains(skill.id);
    }


    // =========================================================
    // PANEL TOGGLE
    // =========================================================

    public void TogglePanels()
    {
        isScoringPanelActive = !isScoringPanelActive;

        scoringPanel.SetActive(isScoringPanelActive);
        gameplayPanel.SetActive(!isScoringPanelActive);

        scoringTab.GetComponent<Image>().color =
            isScoringPanelActive
                ? activeTabColor
                : inactiveTabColor;

        gameplayTab.GetComponent<Image>().color =
            isScoringPanelActive
                ? inactiveTabColor
                : activeTabColor;

        scoringTab.GetComponent<Button>().interactable =
            !isScoringPanelActive;

        gameplayTab.GetComponent<Button>().interactable =
            isScoringPanelActive;

        ClearSkillInfo();
    }


    // =========================================================
    // LOAD JSON
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

        SkillTreeData data =
            JsonUtility.FromJson<SkillTreeData>(json);

        if (data == null)
        {
            Debug.LogError("Failed to parse skill_tree.json.");
            return;
        }

        scoringSkills = data.scoringSkills ?? new List<SkillData>();
        gameplaySkills = data.gameplaySkills ?? new List<SkillData>();

        ApplySavedUnlocks();

        Debug.Log(
            $"Loaded {scoringSkills.Count} scoring skills " +
            $"and {gameplaySkills.Count} gameplay skills."
        );

        UpdateAllSkillVisuals();
    }


    // =========================================================
    // SETUP BUTTONS
    // =========================================================

    public void SetupSkillButtons()
    {
        SetupSkillList(scoringSkills);
        SetupSkillList(gameplaySkills);

        UpdateAllSkillVisuals();
    }

    private void SetupSkillList(List<SkillData> skills)
    {
        if (skills == null)
            return;

        foreach (SkillData skill in skills)
        {
            if (skill == null)
                continue;

            if (skill.clickableObject == null)
            {
                Debug.LogWarning(
                    $"No Button assigned for skill: {skill.id}"
                );

                continue;
            }

            skill.clickableObject.onClick.RemoveAllListeners();

            SkillData selectedSkill = skill;

            skill.clickableObject.onClick.AddListener(
                () => ShowSkillInfo(selectedSkill)
            );
        }
    }


    // =========================================================
    // SKILL VISUALS
    // =========================================================

    private void UpdateSkillVisual(SkillData skill)
    {
        if (skill == null || skill.clickableObject == null)
            return;

        bool requirementsMet = AreRequirementsMet(skill);

        bool canUnlock =
            !skill.isUnlocked &&
            requirementsMet &&
            water >= skill.cost;

        // -----------------------------------------------------
        // VISUAL STATE
        // -----------------------------------------------------

        // Unlocked: normal. Buyable: green. Next (needs more water): amber. Locked: gray.
        Color color;

        if (skill.isUnlocked)
            color = unlockedSkillColor;
        else if (canUnlock)
            color = availableSkillColor;
        else if (requirementsMet)
            color = nextSkillColor;
        else
            color = lockedSkillColor;

        // Apply the color to the Button and every child Graphic.
        Graphic[] graphics =
            skill.clickableObject.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            if (graphic != null)
                graphic.color = color;
        }
    }

    private void UpdateAllSkillVisuals()
    {
        if (scoringSkills != null)
        {
            foreach (SkillData skill in scoringSkills)
            {
                UpdateSkillVisual(skill);
            }
        }

        if (gameplaySkills != null)
        {
            foreach (SkillData skill in gameplaySkills)
            {
                UpdateSkillVisual(skill);
            }
        }
    }


    // =========================================================
    // SHOW SKILL INFORMATION
    // =========================================================

    public void ShowSkillInfo(SkillData skill)
    {
        if (skill == null)
            return;

        selectedSkill = skill;

        if (skillTreeTitle != null)
            skillTreeTitle.text = skill.skillName;

        if (skillTreeDescription != null)
            skillTreeDescription.text = skill.description;

        if (skillTreeEffect != null)
            skillTreeEffect.text = skill.effect;

        if (skillTreeCost != null)
            skillTreeCost.text = skill.cost.ToString();

        UpdateUnlockButton();
    }


    // =========================================================
    // UNLOCK SKILL
    // =========================================================

    public void UnlockSelectedSkill()
    {
        if (selectedSkill == null)
        {
            Debug.LogWarning("No skill selected.");
            return;
        }

        // Already unlocked
        if (selectedSkill.isUnlocked)
        {
            Debug.Log(
                $"{selectedSkill.skillName} is already unlocked."
            );

            return;
        }

        // Check prerequisites first
        if (!AreRequirementsMet(selectedSkill))
        {
            Debug.Log(
                $"Requirements not met for {selectedSkill.skillName}."
            );

            UpdateUnlockButton();
            return;
        }

        // Check Water
        if (water < selectedSkill.cost)
        {
            Debug.Log(
                $"Not enough Water to unlock " +
                $"{selectedSkill.skillName}."
            );

            UpdateUnlockButton();
            return;
        }

        if (isSaving || FBAuthentication.Instance == null)
            return;

        // Spend water and save the unlock to Firebase
        SkillData skill = selectedSkill;
        isSaving = true;

        FBAuthentication.Instance.SaveSkillUnlock(
            skill.id,
            skill.cost,
            () =>
            {
                isSaving = false;
                skill.isUnlocked = true;

                UpdateWaterUI();
                UpdateUnlockButton();
                UpdateAllSkillVisuals();

                ShowToast($"{skill.skillName} unlocked!");

                // TODO:
                // Apply the actual skill effect here.
            },
            error =>
            {
                isSaving = false;
                Debug.LogError($"Failed to save skill unlock: {error}");
                ShowToast("Could not save skill. Try again.");
            }
        );
    }

    private void ShowToast(string message)
    {
        if (DynamicPopupToast.Instance != null)
            DynamicPopupToast.Instance.ShowToast(message);
    }


    // =========================================================
    // UPDATE UNLOCK BUTTON
    // =========================================================

    private void UpdateUnlockButton()
    {
        if (unlockSkillButton == null)
            return;

        if (selectedSkill == null)
        {
            unlockSkillButton.interactable = false;

            SetUnlockButtonText("");

            return;
        }

        // -----------------------------------------------------
        // ALREADY UNLOCKED
        // -----------------------------------------------------

        if (selectedSkill.isUnlocked)
        {
            unlockSkillButton.interactable = false;

            SetUnlockButtonText("Unlocked");

            return;
        }

        // -----------------------------------------------------
        // REQUIREMENTS NOT MET
        // -----------------------------------------------------

        if (!AreRequirementsMet(selectedSkill))
        {
            unlockSkillButton.interactable = false;

            SetUnlockButtonText("Unlock previous skill");

            return;
        }

        // -----------------------------------------------------
        // NOT ENOUGH WATER
        // -----------------------------------------------------

        if (water < selectedSkill.cost)
        {
            unlockSkillButton.interactable = false;

            SetUnlockButtonText("Not enough Water");

            return;
        }

        // -----------------------------------------------------
        // READY TO UNLOCK
        // -----------------------------------------------------

        unlockSkillButton.interactable = true;

        SetUnlockButtonText("Unlock");
    }

    private void SetUnlockButtonText(string text)
    {
        if (unlockSkillButtonText != null)
            unlockSkillButtonText.text = text;
    }


    // =========================================================
    // CHECK REQUIREMENTS
    // =========================================================

    private bool AreRequirementsMet(SkillData skill)
    {
        if (skill == null)
            return false;

        // No requirements
        if (skill.requiredSkills == null ||
            skill.requiredSkills.Count == 0)
        {
            return true;
        }

        foreach (string requiredID in skill.requiredSkills)
        {
            SkillData requiredSkill =
                FindSkillByID(requiredID);

            if (requiredSkill == null)
            {
                Debug.LogWarning(
                    $"Required skill not found: {requiredID}"
                );

                return false;
            }

            if (!requiredSkill.isUnlocked)
            {
                return false;
            }
        }

        return true;
    }


    // =========================================================
    // FIND SKILL
    // =========================================================

    private SkillData FindSkillByID(string id)
    {
        if (scoringSkills != null)
        {
            foreach (SkillData skill in scoringSkills)
            {
                if (skill != null && skill.id == id)
                    return skill;
            }
        }

        if (gameplaySkills != null)
        {
            foreach (SkillData skill in gameplaySkills)
            {
                if (skill != null && skill.id == id)
                    return skill;
            }
        }

        return null;
    }


    // =========================================================
    // RESPEC CONFIRMATION
    // =========================================================

    private void ConfirmRespec()
    {
        if (!HasUnlockedSkills())
        {
            if (DynamicPopupToast.Instance != null)
            {
                DynamicPopupToast.Instance.ShowToast(
                    "No unlocked skills to respec."
                );
            }

            return;
        }

        if (DialogueManager.Instance == null)
        {
            Debug.LogError(
                "DialogueManager instance could not be found."
            );

            return;
        }

        DialogueManager.Instance.ShowDialogue(
            "Are you sure you want to respec all skills? " +
            "All Water spent on unlocked skills will be refunded.",
            "Respec",
            "Cancel",
            RespecSkills
        );
    }


    // =========================================================
    // RESPEC
    // =========================================================

    public void RespecSkills()
    {
        int refundedWater = 0;

        ResetSkillList(
            scoringSkills,
            ref refundedWater
        );

        ResetSkillList(
            gameplaySkills,
            ref refundedWater
        );

        if (isSaving || FBAuthentication.Instance == null)
            return;

        isSaving = true;

        FBAuthentication.Instance.SaveSkillRespec(
            refundedWater,
            () =>
            {
                isSaving = false;

                ClearSkills(scoringSkills);
                ClearSkills(gameplaySkills);

                selectedSkill = null;

                ClearSkillInfo();

                UpdateWaterUI();
                UpdateAllSkillVisuals();

                ShowToast($"+{refundedWater} Water refunded.");
            },
            error =>
            {
                isSaving = false;
                Debug.LogError($"Failed to save respec: {error}");
                ShowToast("Could not save respec. Try again.");
            }
        );
    }


    private void ResetSkillList(
        List<SkillData> skills,
        ref int refundedWater)
    {
        if (skills == null)
            return;

        foreach (SkillData skill in skills)
        {
            if (skill != null && skill.isUnlocked)
                refundedWater += skill.cost;
        }
    }

    private void ClearSkills(List<SkillData> skills)
    {
        if (skills == null)
            return;

        foreach (SkillData skill in skills)
        {
            if (skill != null)
                skill.isUnlocked = false;
        }
    }


    private bool HasUnlockedSkills()
    {
        if (HasUnlockedSkillInList(scoringSkills))
            return true;

        if (HasUnlockedSkillInList(gameplaySkills))
            return true;

        return false;
    }

    private bool HasUnlockedSkillInList(List<SkillData> skills)
    {
        if (skills == null)
            return false;

        foreach (SkillData skill in skills)
        {
            if (skill != null && skill.isUnlocked)
                return true;
        }

        return false;
    }


    // =========================================================
    // WATER UI
    // =========================================================

    private void UpdateWaterUI()
    {
        if (waterText != null)
        {
            waterText.text = water.ToString();
        }
    }


    // =========================================================
    // CLEAR INFO
    // =========================================================

    private void ClearSkillInfo()
    {
        selectedSkill = null;

        if (skillTreeTitle != null)
        {
            skillTreeTitle.text =
                isScoringPanelActive
                    ? "Scoring Skills"
                    : "Gameplay Skills";
        }

        if (skillTreeDescription != null)
        {
            skillTreeDescription.text =
                "Select a skill to view its details.";
        }

        if (skillTreeEffect != null)
            skillTreeEffect.text = "";

        if (skillTreeCost != null)
            skillTreeCost.text = "";

        UpdateUnlockButton();
    }
}