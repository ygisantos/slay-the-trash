
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndTurnButtonUI : MonoBehaviour
{
    private enum State
    {
        Ready,
        Busy,
        Pressed,
        EnemyTurn
    }

    [Header("Colors")]
    [SerializeField] private Color readyColor = new Color(0.45f, 0.9f, 0.45f, 1f);
    [SerializeField] private Color busyColor = new Color(0.6f, 0.75f, 0.6f, 1f);
    [SerializeField] private Color pressedColor = new Color(1f, 0.75f, 0.25f, 1f);
    [SerializeField] private Color enemyTurnColor = new Color(0.45f, 0.45f, 0.5f, 1f);

    [Header("Timing")]
    [SerializeField] private float pressDelay = 0.35f;
    [SerializeField] private float colorSpeed = 12f;

    [Header("Turn Timer")]
    [SerializeField, Min(1f)] private float turnDuration = 30f;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Color timerStartColor = Color.green;
    [SerializeField] private Color timerMiddleColor = Color.yellow;
    [SerializeField] private Color timerEndColor = Color.red;
    [SerializeField, Min(1)] private int warningThreshold = 5;
    [SerializeField] private float timerPulseScale = 1.2f;
    [SerializeField] private float timerPulseDuration = 0.15f;

    [Header("Final Countdown Warning")]
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private string warningPrefix = "TURN ENDS IN";
    [SerializeField] private float warningPulseScale = 1.3f;
    [SerializeField] private float warningPulseDuration = 0.2f;

    [Header("Camera Warning")]
    [SerializeField] private bool pulseCamera = true;
    [SerializeField] private float cameraShakeDuration = 0.12f;
    [SerializeField] private float cameraShakeStrength = 0.035f;

    [Header("Labels")]
    [SerializeField] private string pressedLabel = "Ending...";
    [SerializeField] private string enemyTurnLabel = "Enemy Turn";

    private Button button;
    private Graphic graphic;
    private TMP_Text label;
    private Text legacyLabel;

    private string readyLabel;
    private Vector3 baseScale;
    private Vector3 timerBaseScale;
    private Vector3 warningBaseScale;

    private State state = State.Ready;
    private State previousState = State.Ready;

    private Color displayColor;
    private float scaleMultiplier = 1f;
    private float timeRemaining;

    private bool pressLocked;
    private bool enemyTurnRunning;
    private bool autoEndTriggered;
    private bool timerRunning;

    private int lastDisplayedSecond = -1;

    private void Awake()
    {
        button = GetComponent<Button>();

        graphic = button != null && button.targetGraphic != null
            ? button.targetGraphic
            : GetComponent<Graphic>();

        label = GetComponentInChildren<TMP_Text>(true);
        legacyLabel = label == null
            ? GetComponentInChildren<Text>(true)
            : null;

        readyLabel = GetLabel();
        baseScale = transform.localScale;

        if (button != null)
            button.transition = Selectable.Transition.None;

        if (timerText != null)
            timerBaseScale = timerText.transform.localScale;

        if (warningText != null)
            warningBaseScale = warningText.transform.localScale;

        displayColor = readyColor;
    }

    private void OnEnable()
    {
        pressLocked = false;
        enemyTurnRunning = false;
        autoEndTriggered = false;
        scaleMultiplier = 1f;

        timeRemaining = turnDuration;
        timerRunning = true;
        lastDisplayedSecond = -1;

        state = ComputeState();
        previousState = state;

        displayColor = readyColor;

        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.transform.localScale = timerBaseScale;
        }

        HideWarning();
        UpdateTimerDisplay(true);
        ApplyState(false);
    }

    private void OnDisable()
    {
        DOTween.Kill(this);

        if (timerText != null)
        {
            timerText.transform.DOKill();
            timerText.transform.localScale = timerBaseScale;
        }

        if (warningText != null)
        {
            warningText.transform.DOKill();
            warningText.transform.localScale = warningBaseScale;
            warningText.gameObject.SetActive(false);
        }

        transform.localScale = baseScale;
    }

    private void Update()
    {
        State desired = ComputeState();

        if (desired != state)
        {
            previousState = state;
            state = desired;

            if (state == State.Ready &&
                previousState == State.EnemyTurn)
            {
                ResetTimer();
            }

            ApplyState(true);
        }

        UpdateButtonVisuals();
        UpdateTurnTimer();
    }

    private void UpdateButtonVisuals()
    {
        Color target = state switch
        {
            State.Ready => readyColor,
            State.Busy => busyColor,
            State.Pressed => pressedColor,
            _ => enemyTurnColor
        };

        float pulse = 0f;

        if (state == State.Ready)
        {
            pulse = Mathf.Sin(Time.unscaledTime * 3f) * 0.5f + 0.5f;
            target = Color.Lerp(target, Color.white, pulse * 0.25f);
        }

        displayColor = Color.Lerp(
            displayColor,
            target,
            1f - Mathf.Exp(-colorSpeed * Time.unscaledDeltaTime)
        );

        if (graphic != null)
            graphic.color = displayColor;

        transform.localScale =
            baseScale * (scaleMultiplier * (1f + pulse * 0.03f));
    }

    private void UpdateTurnTimer()
    {
        if (!timerRunning || pressLocked || enemyTurnRunning)
            return;

        // The timer runs during the player's turn, including
        // while their card or effect is resolving.
        if (state != State.Ready && state != State.Busy)
            return;

        timeRemaining -= Time.unscaledDeltaTime;
        timeRemaining = Mathf.Max(0f, timeRemaining);

        UpdateTimerDisplay(false);

        int seconds = Mathf.CeilToInt(timeRemaining);

        // Trigger the large countdown warning at 5 seconds or less.
        if (timeRemaining > 0f && seconds <= warningThreshold)
        {
            ShowWarning(seconds);
        }
        else
        {
            HideWarning();
        }

        // Automatically end the turn at zero.
        if (timeRemaining <= 0f && !autoEndTriggered)
        {
            autoEndTriggered = true;
            timerRunning = false;
            HideWarning();

            if (CardSystem.Instance != null &&
                CardSystem.Instance.IsCombatActive)
            {
                StartCoroutine(PressRoutine());
            }
        }
    }

    private void UpdateTimerDisplay(bool force)
    {
        if (timerText == null)
            return;

        int seconds = Mathf.CeilToInt(timeRemaining);

        timerText.text = seconds.ToString();

        float progress = 1f - (timeRemaining / turnDuration);

        Color timerColor;

        if (progress < 0.5f)
        {
            timerColor = Color.Lerp(
                timerStartColor,
                timerMiddleColor,
                progress * 2f
            );
        }
        else
        {
            timerColor = Color.Lerp(
                timerMiddleColor,
                timerEndColor,
                (progress - 0.5f) * 2f
            );
        }

        timerText.color = timerColor;

        // Pulse once when the displayed second changes.
        if (force || seconds != lastDisplayedSecond)
        {
            lastDisplayedSecond = seconds;
            PulseTimer();
        }
    }

    private void PulseTimer()
    {
        if (timerText == null)
            return;

        timerText.transform.DOKill();
        timerText.transform.localScale = timerBaseScale;

        timerText.transform
            .DOScale(timerBaseScale * timerPulseScale, timerPulseDuration)
            .SetEase(Ease.OutQuad)
            .SetLoops(2, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void ShowWarning(int seconds)
    {
        if (warningText == null)
            return;

        if (!warningText.gameObject.activeSelf)
        {
            warningText.gameObject.SetActive(true);
            warningText.transform.localScale = warningBaseScale;
            lastWarningSecond = -1;
        }

        warningText.text = $"{warningPrefix}\n{seconds}";

        // Animate and pulse the camera once per countdown second.
        if (seconds != lastWarningSecond)
        {
            lastWarningSecond = seconds;

            warningText.transform.DOKill();
            warningText.transform.localScale = warningBaseScale;

            warningText.transform
                .DOScale(
                    warningBaseScale * warningPulseScale,
                    warningPulseDuration
                )
                .SetEase(Ease.OutBack)
                .SetLoops(2, LoopType.Yoyo)
                .SetUpdate(true);

            if (pulseCamera)
            {
                Shake.Instance?.ShakeCamera(
                    cameraShakeDuration,
                    cameraShakeStrength
                );
            }
        }

        // The warning becomes more urgent as time runs out.
        float urgency = 1f -
            Mathf.Clamp01((float)seconds / warningThreshold);

        warningText.color = Color.Lerp(
            timerMiddleColor,
            timerEndColor,
            urgency
        );
    }

    private int lastWarningSecond = -1;

    private void HideWarning()
    {
        lastWarningSecond = -1;

        if (warningText == null)
            return;

        warningText.transform.DOKill();
        warningText.transform.localScale = warningBaseScale;
        warningText.gameObject.SetActive(false);
    }

    private void ResetTimer()
    {
        timeRemaining = turnDuration;
        timerRunning = true;
        autoEndTriggered = false;
        lastDisplayedSecond = -1;

        HideWarning();
        UpdateTimerDisplay(true);
    }

    private State ComputeState()
    {
        if (pressLocked && !enemyTurnRunning)
            return State.Pressed;

        if (enemyTurnRunning)
            return State.EnemyTurn;

        if (ActionSystem.Instance != null &&
            ActionSystem.Instance.IsPerforming)
            return State.Busy;

        return State.Ready;
    }

    private void ApplyState(bool animate)
    {
        if (button != null)
            button.interactable = state == State.Ready;

        switch (state)
        {
            case State.Pressed:
                SetLabel(pressedLabel);
                break;

            case State.EnemyTurn:
                SetLabel(enemyTurnLabel);
                break;

            default:
                SetLabel(readyLabel);
                break;
        }

        if (animate &&
            state == State.Ready &&
            previousState == State.EnemyTurn)
        {
            Pop(1.2f);
        }
    }

    public void OnClick()
    {
        if (pressLocked || state != State.Ready)
            return;

        if (CardSystem.Instance == null ||
            !CardSystem.Instance.IsCombatActive)
            return;

        if (ActionSystem.Instance == null ||
            ActionSystem.Instance.IsPerforming)
            return;

        StartCoroutine(PressRoutine());
    }

    private IEnumerator PressRoutine()
    {
        if (pressLocked)
            yield break;

        pressLocked = true;
        timerRunning = false;

        HideWarning();

        state = State.Pressed;
        ApplyState(false);

        SoundManager.Instance?.PlaySound(SoundType.CLICK);

        // Quick squash to acknowledge the click.
        DOTween.Kill(this);

        scaleMultiplier = 1f;

        DOTween.To(
                () => scaleMultiplier,
                v => scaleMultiplier = v,
                0.88f,
                0.08f
            )
            .SetLoops(2, LoopType.Yoyo)
            .SetUpdate(true)
            .SetId(this);

        yield return new WaitForSecondsRealtime(pressDelay);

        // Wait until all player actions have resolved.
        while (ActionSystem.Instance != null &&
               ActionSystem.Instance.IsPerforming)
        {
            yield return null;
        }

        if (ActionSystem.Instance == null ||
            CardSystem.Instance == null ||
            !CardSystem.Instance.IsCombatActive)
        {
            pressLocked = false;
            timerRunning = false;
            yield break;
        }

        enemyTurnRunning = true;

        Shake.Instance?.ShakeCamera(0.15f, 0.04f);

        EnemyTurnGA enemyTurnGA = new();

        ActionSystem.Instance.Perform(enemyTurnGA, () =>
        {
            enemyTurnRunning = false;
            pressLocked = false;

            // The next player turn will reset the timer
            // when the state transitions back to Ready.
        });
    }

    private void Pop(float strength)
    {
        DOTween.Kill(this);
        scaleMultiplier = 1f;

        DOTween.To(
                () => scaleMultiplier,
                v => scaleMultiplier = v,
                strength,
                0.12f
            )
            .SetEase(Ease.OutQuad)
            .SetLoops(2, LoopType.Yoyo)
            .SetUpdate(true)
            .SetId(this);
    }

    private string GetLabel()
    {
        if (label != null)
            return label.text;

        if (legacyLabel != null)
            return legacyLabel.text;

        return "";
    }

    private void SetLabel(string text)
    {
        if (label != null)
            label.text = text;
        else if (legacyLabel != null)
            legacyLabel.text = text;
    }
}
