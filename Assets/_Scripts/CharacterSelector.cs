
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

public class CharacterSelector : MonoBehaviour
{
    [System.Serializable]
    public class CharacterInfo
    {
        public string characterName;
        public UnityEvent onConfirm;
    }

    
    [System.Serializable]
    public class CharacterButtonUI
    {
        public UnityEngine.UI.Button button;
        public TMPro.TMP_Text buttonText;

        [HideInInspector] public Vector3 originalScale;
        [HideInInspector] public Vector3 originalTextScale;
        [HideInInspector] public Color originalButtonColor;
        [HideInInspector] public Color originalTextColor;
    }

    [Header("Character Buttons (4 total)")]
    [SerializeField] private CharacterButtonUI[] characterButtons;

    [Header("Button Animation")]
    [SerializeField] private float buttonPopScale = 1.12f;
    [SerializeField] private float buttonAnimDuration = 0.2f;
    [SerializeField] private Color selectedButtonColor =
        new Color(1f, 0.8f, 0.3f, 1f);
    [SerializeField] private Color selectedTextColor = Color.white;
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private float selectedTextPopScale = 1.12f;


    [Header("Character Info (4 total)")]
    public CharacterInfo[] characters;

    [Header("Stats Description")]
    [Tooltip("TMP text that displays the selected character's stat modifiers.")]
    [SerializeField] private TMPro.TMP_Text statsDescription;

    [Header("Character Image Objects (4 total)")]
    public GameObject[] characterImages;

    [Header("Selection Animation")]
    [SerializeField] private float popScale = 1.12f;
    [SerializeField] private float selectDuration = 0.3f;
    [SerializeField] private float rotationAngle = 5f;
    [SerializeField] private float idleFloatAmount = 6f;
    [SerializeField] private float idleFloatDuration = 1.2f;

    [Header("Confirmation Animation")]
    [SerializeField] private float confirmScale = 1.2f;
    [SerializeField] private float confirmDuration = 0.15f;

    private int currentIndex = -1;
    private bool waitingForConfirm = false;

    private Vector3[] originalScales;
    private Quaternion[] originalRotations;
    private Vector3[] originalPositions;

    private void Awake()
    {
        SetupButtonUI();
        int count = characterImages != null ? characterImages.Length : 0;

        originalScales = new Vector3[count];
        originalRotations = new Quaternion[count];
        originalPositions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            if (characterImages[i] == null)
                continue;

            Transform t = characterImages[i].transform;

            originalScales[i] = t.localScale;
            originalRotations[i] = t.localRotation;
            originalPositions[i] = t.localPosition;
        }
    }

    private void Start()
    {
        for (int i = 0; i < characterImages.Length; i++)
        {
            if (characterImages[i] != null)
                characterImages[i].SetActive(false);
        }

        // Reflect a character that was already chosen earlier in the session.
        if (SelectedCharacter.index >= 0)
            ShowStats(SelectedCharacter.index);
    }

    public void OnCharacterButtonPressed(int index)
    {
        if (characterImages == null ||
            index < 0 ||
            index >= characterImages.Length ||
            characters == null ||
            index >= characters.Length ||
            characterImages[index] == null)
        {
            Debug.LogWarning($"Invalid character index: {index}", this);
            return;
        }

        // FIRST TAP: select the character.
        if (currentIndex != index || !waitingForConfirm)
        {
            AnimateButtonSelection(index);
            currentIndex = index;
            waitingForConfirm = true;

            ShowStats(index);

            SoundManager.Instance?.PlaySound(SoundType.CLICK);

            ShowCharacterImage(index);
            AnimateSelection(index);
            return;
        }

        // SECOND TAP: confirm the selected character.
        AnimateButtonConfirmation(index);
        waitingForConfirm = false;
        

        SoundManager.Instance?.PlaySound(SoundType.CLICK);

        SelectedCharacter.index = index;

        AnimateConfirmation(index);

        // Preserve the Inspector-configured event.
        characters[index].onConfirm?.Invoke();
    }

    
    private void SetupButtonUI()
    {
        if (characterButtons == null) return;

        foreach (var item in characterButtons)
            CaptureButtonBaseline(item);
    }

    // Records the authored scale/colour so the animations can return to them.
    // These elements are authored small and scaled up by their parent, so the
    // text scale must be preserved rather than reset to 1.
    private void CaptureButtonBaseline(CharacterButtonUI item)
    {
        if (item == null) return;

        if (item.button != null)
        {
            item.originalScale = item.button.transform.localScale;
            if (item.originalScale == Vector3.zero)
                item.originalScale = Vector3.one;

            if (item.button.targetGraphic != null)
                item.originalButtonColor = item.button.targetGraphic.color;
        }

        if (item.buttonText != null)
        {
            item.originalTextColor = item.buttonText.color;

            item.originalTextScale = item.buttonText.transform.localScale;
            if (item.originalTextScale == Vector3.zero)
                item.originalTextScale = Vector3.one;
        }
    }

    private void AnimateButtonSelection(int selectedIndex)
    {
        if (characterButtons == null) return;

        for (int i = 0; i < characterButtons.Length; i++)
        {
            var item = characterButtons[i];
            if (item == null) continue;

            // If the array was assigned after Awake the baselines are still zero,
            // which would scale the element away instead of animating it.
            if (item.originalScale == Vector3.zero)
                CaptureButtonBaseline(item);

            bool selected = i == selectedIndex;

            if (item.button != null)
            {
                Transform t = item.button.transform;
                t.DOKill();

                t.DOScale(
                    item.originalScale *
                    (selected ? buttonPopScale : 1f),
                    buttonAnimDuration
                )
                .SetEase(selected ? Ease.OutBack : Ease.OutQuad)
                .SetUpdate(true);

                if (item.button.targetGraphic != null)
                {
                    item.button.targetGraphic.DOKill();

                    item.button.targetGraphic.DOColor(
                        selected
                            ? selectedButtonColor
                            : item.originalButtonColor,
                        buttonAnimDuration
                    ).SetUpdate(true);
                }
            }

            if (item.buttonText != null)
            {
                // If the array was assigned after Awake the baseline is still zero.
                if (item.originalTextScale == Vector3.zero)
                    CaptureButtonBaseline(item);

                Transform textTransform = item.buttonText.transform;
                textTransform.DOKill();

                textTransform.DOScale(
                    item.originalTextScale *
                    (selected ? selectedTextPopScale : 1f),
                    buttonAnimDuration
                )
                .SetEase(Ease.OutBack)
                .SetUpdate(true);

                item.buttonText.DOKill();

                item.buttonText.DOColor(
                    selected
                        ? selectedTextColor
                        : item.originalTextColor,
                    buttonAnimDuration
                ).SetUpdate(true);
            }
        }
    }

    private void AnimateButtonConfirmation(int index)
    {
        if (characterButtons == null ||
            index < 0 ||
            index >= characterButtons.Length)
            return;

        var item = characterButtons[index];
        if (item == null) return;

        if (item.originalScale == Vector3.zero || item.originalTextScale == Vector3.zero)
            CaptureButtonBaseline(item);

        if (item.button != null)
        {
            Transform t = item.button.transform;
            t.DOKill();

            t.DOPunchScale(
                item.originalScale * 0.12f,
                0.3f,
                8,
                0.7f
            ).SetUpdate(true);
        }

        if (item.buttonText != null)
        {
            Transform t = item.buttonText.transform;
            t.DOKill();

            t.DOPunchScale(item.originalTextScale * 0.15f, 0.3f, 8, 0.7f)
                .SetUpdate(true);
        }
    }


    private void ShowCharacterImage(int index)
    {
        for (int i = 0; i < characterImages.Length; i++)
        {
            if (characterImages[i] == null)
                continue;

            bool selected = i == index;

            if (!selected)
            {
                StopAnimation(i);
                characterImages[i].SetActive(false);
            }
            else
            {
                characterImages[i].SetActive(true);
            }
        }
    }

    private void AnimateSelection(int index)
    {
        Transform target = characterImages[index].transform;

        StopAnimation(index);

        target.localScale = originalScales[index];
        target.localRotation = originalRotations[index];
        target.localPosition = originalPositions[index];

        // Pop into view.
        target.DOScale(originalScales[index] * popScale, selectDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);

        // Small tilt for a lively selection effect.
        target.DOLocalRotate(
                originalRotations[index].eulerAngles +
                new Vector3(0f, 0f, rotationAngle),
                selectDuration * 0.7f
            )
            .SetEase(Ease.OutQuad)
            .SetLoops(2, LoopType.Yoyo)
            .SetUpdate(true);

        // Gentle floating animation while selected.
        target.DOLocalMoveY(
                originalPositions[index].y + idleFloatAmount,
                idleFloatDuration
            )
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void AnimateConfirmation(int index)
    {
        Transform target = characterImages[index].transform;

        target.DOKill();

        target.DOScale(
                originalScales[index] * confirmScale,
                confirmDuration
            )
            .SetEase(Ease.OutQuad)
            .SetLoops(2, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void StopAnimation(int index)
    {
        if (characterImages[index] == null)
            return;

        Transform target = characterImages[index].transform;

        target.DOKill();

        target.localScale = originalScales[index];
        target.localRotation = originalRotations[index];
        target.localPosition = originalPositions[index];
    }

    public void ConfirmCharacter(int i)
    {
        SelectedCharacter.index = i;
    }

    // Fills the description panel with the highlighted character's stat modifiers.
    private void ShowStats(int index)
    {
        if (statsDescription == null)
            return;

        statsDescription.text = CharacterStats.Describe(index);
    }

    private void OnDisable()
    {
        if (characterImages == null)
            return;

        for (int i = 0; i < characterImages.Length; i++)
        {
            if (characterImages[i] == null)
                continue;

            StopAnimation(i);
        }
    }
}
