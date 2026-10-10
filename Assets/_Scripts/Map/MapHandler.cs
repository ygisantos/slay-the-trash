
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapHandler : MonoBehaviour
{
    [System.Serializable]
    public struct LevelMaps
    {
        public List<MapView> maps;
    }

    // =========================================================
    // DYNAMIC BATTLE BANNER
    // =========================================================

    [Header("Dynamic Battle Banner")]
    [SerializeField] private Color bannerBackgroundColor =
        new Color(0.08f, 0.08f, 0.12f, 0.95f);

    [SerializeField] private Color bannerTextColor = Color.white;
    [SerializeField] private float bannerDisplayDuration = 1.5f;
    [SerializeField] private float bannerFadeDuration = 0.25f;
    [SerializeField] private float bannerAnimationDuration = 0.4f;
    [SerializeField] private Vector2 bannerSize = new Vector2(650f, 130f);
    [SerializeField] private float bannerFontSize = 48f;

    private RectTransform activePlayer;
    private GameObject bannerObject;
    private CanvasGroup bannerCanvasGroup;
    private TMP_Text bannerTitle;

    private Sequence bannerSequence;

    // =========================================================
    // HIERARCHY
    // =========================================================

    [Header("Hierarchy")]
    public Transform mapParent;
    public Transform mainMap;

    // =========================================================
    // PLAYER
    // =========================================================

    [Header("Player Icon")]
    public RectTransform player;
    public GameObject healthPrefab;

    // =========================================================
    // MAP DATA
    // =========================================================

    [Header("Generated Data")]
    public List<LevelMaps> levels = new List<LevelMaps>();

    // =========================================================
    // MOVEMENT SETTINGS
    // =========================================================

    [Header("Movement Settings")]
    public float glideDuration = 2f;
    public float scrollAmount = -600f;
    public float scrollDuration = 2f;
    public MatchSetupSystem matchSetupSystem;

    // =========================================================
    // PLAYER IDLE ANIMATION
    // =========================================================

    [Header("Player Idle Animation")]
    [SerializeField] private float idleFloatAmount = 8f;
    [SerializeField] private float idleFloatDuration = 1.2f;
    [SerializeField] private float idlePulseScale = 1.06f;
    [SerializeField] private float idlePulseDuration = 0.8f;

    // =========================================================
    // PLAYER MOVEMENT ANIMATION
    // =========================================================

    [Header("Player Movement Animation")]
    [SerializeField] private float hopHeight = 65f;
    [SerializeField] private float movementTilt = 12f;
    [SerializeField] private float arrivalPunchScale = 1.2f;

    // =========================================================
    // NODE ANIMATION
    // =========================================================

    [Header("Node Selection Animation")]
    [SerializeField] private float nodePulseScale = 1.2f;
    [SerializeField] private float nodePulseDuration = 0.3f;

    // =========================================================
    // BATTLE TRANSITION
    // =========================================================

    [Header("Battle Transition")]
    [SerializeField] private string enemyIntroText = "BATTLE START!";
    [SerializeField] private string bossIntroText = "BOSS ENCOUNTER!";
    [SerializeField] private float zoomScale = 1.12f;
    [SerializeField] private float zoomDuration = 0.3f;
    [SerializeField] private float battleShakeDuration = 0.2f;
    [SerializeField] private float battleShakeStrength = 0.06f;

    // =========================================================
    // INTERNAL STATE
    // =========================================================

    private Vector3 playerBaseScale;
    private Vector3 playerBasePosition;
    private Quaternion playerBaseRotation;

    private Vector3 mapBaseScale;
    private Vector3 mapBasePosition;

    private Sequence playerIdleSequence;
    private Sequence movementSequence;
    private Tween nodePulseTween;

    private Coroutine movementCoroutine;
    private Coroutine battleCoroutine;

    private MapView selectedNode;

    private bool isMoving;
    private bool isBattleTransitioning;

    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Awake()
    {
        if (player != null)
        {
            playerBaseScale = player.localScale;
            playerBasePosition = player.localPosition;
            playerBaseRotation = player.localRotation;
        }

        if (mainMap != null)
        {
            mapBaseScale = mainMap.localScale;
            mapBasePosition = mainMap.localPosition;
        }
    }

    private void Start()
    {
        BuildMapLevels();
        LockAllLevelsExceptFirst();
        StartPlayerIdle();
    }

    // =========================================================
    // BUILD MAP
    // =========================================================

    private void BuildMapLevels()
    {
        levels.Clear();

        if (mapParent == null)
        {
            Debug.LogError("MapParent is not assigned.", this);
            return;
        }

        if (MapManager.Instance == null)
        {
            Debug.LogError("MapManager.Instance is missing.", this);
            return;
        }

        for (int i = 0; i < mapParent.childCount; i++)
        {
            Transform levelObject = mapParent.GetChild(i);

            LevelMaps level = new LevelMaps
            {
                maps = new List<MapView>()
            };

            for (int j = 0; j < levelObject.childCount; j++)
            {
                Transform slot = levelObject.GetChild(j);

                MapView mapView = slot.GetComponent<MapView>();

                if (mapView == null)
                    mapView = slot.GetComponentInChildren<MapView>();

                if (mapView == null)
                    continue;

                MapData data = MapManager.Instance.GetMapData(i, j);

                if (data != null)
                    mapView.Setup(data);

                level.maps.Add(mapView);
                mapView.mapHandler = this;
            }

            levels.Add(level);
        }
    }

    private RectTransform GetActivePlayer()
    {
        if (player == null)
        {
            Debug.LogError("MapIcon (player) is not assigned.", this);
            return null;
        }

        // Find the currently active character among MapIcon's children.
        for (int i = 0; i < player.childCount; i++)
        {
            Transform child = player.GetChild(i);

            if (child.gameObject.activeInHierarchy)
            {
                RectTransform character = child as RectTransform;

                if (character != null)
                    return character;
            }
        }

        Debug.LogWarning("No active character found under MapIcon.", this);
        return null;
    }
    // =========================================================
    // CREATE DYNAMIC BATTLE BANNER
    // =========================================================

    private void CreateBattleBanner()
    {
        if (bannerObject != null)
            return;

        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning(
                "Cannot create battle banner: No Canvas was found.",
                this
            );

            return;
        }

        // Create banner background and CanvasGroup.
        bannerObject = new GameObject(
            "DynamicBattleBanner",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image)
        );

        bannerObject.transform.SetParent(canvas.transform, false);

        RectTransform bannerRect =
            bannerObject.GetComponent<RectTransform>();

        bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
        bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
        bannerRect.pivot = new Vector2(0.5f, 0.5f);
        bannerRect.anchoredPosition = Vector2.zero;
        bannerRect.sizeDelta = bannerSize;
        bannerRect.localScale = Vector3.zero;

        Image background = bannerObject.GetComponent<Image>();
        background.color = bannerBackgroundColor;
        background.raycastTarget = false;

        bannerCanvasGroup = bannerObject.GetComponent<CanvasGroup>();
        bannerCanvasGroup.alpha = 0f;
        bannerCanvasGroup.interactable = false;
        bannerCanvasGroup.blocksRaycasts = false;

        // Create title text.
        GameObject textObject = new GameObject(
            "BannerTitle",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        textObject.transform.SetParent(bannerObject.transform, false);

        RectTransform textRect =
            textObject.GetComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 10f);
        textRect.offsetMax = new Vector2(-20f, -10f);

        bannerTitle = textObject.GetComponent<TextMeshProUGUI>();

        bannerTitle.text = string.Empty;
        bannerTitle.alignment = TextAlignmentOptions.Center;
        bannerTitle.fontSize = bannerFontSize;
        bannerTitle.fontStyle = FontStyles.Bold;
        bannerTitle.color = bannerTextColor;
        bannerTitle.enableAutoSizing = true;
        bannerTitle.fontSizeMin = 24f;
        bannerTitle.fontSizeMax = bannerFontSize;
        bannerTitle.raycastTarget = false;

        bannerObject.SetActive(false);
    }

    // =========================================================
    // SHOW DYNAMIC BATTLE BANNER
    // =========================================================

    private IEnumerator ShowBattleBanner(string title)
    {
        CreateBattleBanner();

        if (bannerObject == null || bannerTitle == null)
            yield break;

        bannerSequence?.Kill();

        bannerTitle.text = title;

        bannerObject.SetActive(true);

        // Keep the banner above other UI on this Canvas.
        bannerObject.transform.SetAsLastSibling();

        bannerCanvasGroup.alpha = 0f;
        bannerObject.transform.localScale = Vector3.zero;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            bannerCanvasGroup.DOFade(1f, bannerFadeDuration)
        );

        sequence.Join(
            bannerObject.transform
                .DOScale(Vector3.one, bannerAnimationDuration)
                .SetEase(Ease.OutBack)
        );

        sequence.AppendInterval(bannerDisplayDuration);

        sequence.Append(
            bannerCanvasGroup.DOFade(0f, bannerFadeDuration)
        );

        sequence.Join(
            bannerObject.transform
                .DOScale(0.85f, bannerFadeDuration)
                .SetEase(Ease.InBack)
        );

        bannerSequence = sequence;
        sequence.SetUpdate(true);

        yield return sequence.WaitForCompletion();

        if (bannerObject != null)
            bannerObject.SetActive(false);

        bannerSequence = null;
    }

    // =========================================================
    // NODE LOCKING
    // =========================================================

    public void LockAllLevelsExceptFirst()
    {
        LockAllLevels();

        if (levels.Count == 0)
            return;

        foreach (MapView mapView in levels[0].maps)
        {
            if (mapView != null)
                mapView.SetInteractable();
        }
    }

    public void LockAllLevels()
    {
        foreach (LevelMaps level in levels)
        {
            foreach (MapView mapView in level.maps)
            {
                if (mapView != null)
                    mapView.SetNotInteractable();
            }
        }
    }

    // =========================================================
    // PLAYER IDLE ANIMATION
    // =========================================================

    private void StartPlayerIdle()
    {
        if (player == null || isMoving || isBattleTransitioning)
            return;

        StopPlayerIdle();

        player.localPosition = playerBasePosition;
        player.localRotation = playerBaseRotation;
        player.localScale = playerBaseScale;

        playerIdleSequence = DOTween.Sequence();

        playerIdleSequence.Append(
            player.DOLocalMoveY(
                playerBasePosition.y + idleFloatAmount,
                idleFloatDuration
            ).SetEase(Ease.InOutSine)
        );

        playerIdleSequence.Append(
            player.DOLocalMoveY(
                playerBasePosition.y,
                idleFloatDuration
            ).SetEase(Ease.InOutSine)
        );

        playerIdleSequence.SetLoops(-1);
        playerIdleSequence.SetUpdate(true);

        player.DOScale(
            playerBaseScale * idlePulseScale,
            idlePulseDuration
        )
        .SetEase(Ease.InOutSine)
        .SetLoops(-1, LoopType.Yoyo)
        .SetUpdate(true)
        .SetId("PlayerIdlePulse");
    }


    private void StopPlayerIdle()
    {
        playerIdleSequence?.Kill();
        playerIdleSequence = null;

        DOTween.Kill("PlayerIdlePulse");

        if (player != null)
        {
            player.DOKill();
            player.localScale = playerBaseScale;
            player.localRotation = playerBaseRotation;
        }
    }


    // =========================================================
    // SELECT MAP NODE AND MOVE PLAYER
    // =========================================================
    
    public void MovePlayerTo(MapView mapView)
    {
        if (player == null || mapView == null)
            return;

        if (isMoving || isBattleTransitioning)
            return;

        activePlayer = GetActivePlayer();

        if (activePlayer == null)
            return;

        selectedNode = mapView;
        isMoving = true;

        StopPlayerIdle();

        activePlayer.DOKill();

        if (mainMap != null)
            mainMap.DOKill();

        LockAllLevels();
        movementCoroutine = StartCoroutine(GlidePlayerAndScroll(mapView));
    }

    private IEnumerator GlidePlayerAndScroll(MapView target)
    {
        RectTransform character = activePlayer;

        if (target == null || character == null)
        {
            isMoving = false;
            movementCoroutine = null;
            yield break;
        }

        if (mainMap == null)
        {
            Debug.LogError("[MapMovement] Main Map is not assigned.", this);
            isMoving = false;
            movementCoroutine = null;
            yield break;
        }

        AnimateNodeSelection(target);

        // Convert the selected node's world position into MapIcon's local space.
        Transform characterParent = character.parent;

        Vector3 targetLocalPosition = characterParent != null
            ? characterParent.InverseTransformPoint(target.transform.position)
            : target.transform.position;

        Debug.Log(
            $"[MapMovement] Character: {character.name} | " +
            $"Parent: {characterParent?.name} | " +
            $"Current Local: {character.localPosition} | " +
            $"Target Local: {targetLocalPosition}"
        );

        Vector3 startLocalPosition = character.localPosition;
        Vector3 endLocalPosition = targetLocalPosition;

        Vector3 mapStartPosition = mainMap.localPosition;
        Vector3 mapEndPosition = mapStartPosition
            + new Vector3(scrollAmount, 0f, 0f);

        Vector3 characterBaseScale = character.localScale;
        Quaternion characterBaseRotation = character.localRotation;

        character.DOKill();

        // Move the active character to the selected node.
        Sequence move = DOTween.Sequence();

        move.Append(
            character.DOLocalMove(endLocalPosition, glideDuration)
                .SetEase(Ease.InOutCubic)
        );

        // Hop animation.
        float startY = startLocalPosition.y;
        float endY = endLocalPosition.y;

        Tween hop = DOTween.To(
            () => 0f,
            value =>
            {
                if (character != null)
                {
                    Vector3 pos = character.localPosition;

                    pos.y = Mathf.Lerp(startY, endY, value)
                        + Mathf.Sin(value * Mathf.PI) * hopHeight;

                    character.localPosition = pos;
                }
            },
            1f,
            glideDuration
        ).SetEase(Ease.Linear);

        move.Join(hop);

        // Tilt during movement.
        move.Join(
            character.DOLocalRotate(
                characterBaseRotation.eulerAngles
                    + new Vector3(0f, 0f, movementTilt),
                glideDuration * 0.5f
            ).SetEase(Ease.OutQuad)
        );

        // Restore rotation.
        move.Insert(
            glideDuration * 0.5f,
            character.DOLocalRotate(
                characterBaseRotation.eulerAngles,
                glideDuration * 0.5f
            ).SetEase(Ease.InOutQuad)
        );

        // Scroll the map at the same time.
        mainMap.DOKill();

        move.Join(
            mainMap.DOLocalMove(mapEndPosition, scrollDuration)
                .SetEase(Ease.InOutCubic)
        );

        movementSequence = move;
        move.SetUpdate(true);

        yield return move.WaitForCompletion();

        // Finalize the character's transform.
        if (character != null)
        {
            character.localPosition = endLocalPosition;
            character.localRotation = characterBaseRotation;
            character.localScale = characterBaseScale;

            character.DOPunchScale(
                characterBaseScale * (arrivalPunchScale - 1f),
                0.3f,
                6,
                0.6f
            ).SetUpdate(true);
        }

        isMoving = false;
        movementCoroutine = null;
        movementSequence = null;

        MapViewLogic(target);
    }
    // =========================================================
    // NODE SELECTION PULSE
    // =========================================================

    private void AnimateNodeSelection(MapView target)
    {
        if (target == null)
            return;

        Transform node = target.transform;

        nodePulseTween?.Kill();

        Vector3 originalScale = node.localScale;

        nodePulseTween = node
            .DOScale(originalScale * nodePulseScale, nodePulseDuration)
            .SetEase(Ease.OutBack)
            .SetLoops(2, LoopType.Yoyo)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (node != null)
                    node.localScale = originalScale;
            });
    }

    // =========================================================
    // BATTLE TRANSITION
    // =========================================================

    private IEnumerator StartBattleSequence(bool isBoss)
    {
        if (isBattleTransitioning)
            yield break;

        isBattleTransitioning = true;

        StopPlayerIdle();

        string title = isBoss
            ? bossIntroText
            : enemyIntroText;

        // Zoom into the map while displaying the banner.
        if (mainMap != null)
        {
            mainMap.DOKill();

            mainMap.DOScale(
                mapBaseScale * zoomScale,
                zoomDuration
            )
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
        }

        // Show the dynamically created title.
        yield return ShowBattleBanner(title);

        // Camera shake using the existing project system.
        if (Shake.Instance != null)
        {
            Shake.Instance.ShakeCamera(
                battleShakeDuration,
                battleShakeStrength
            );
        }

        // Restore map scale before starting combat.
        if (mainMap != null)
        {
            yield return mainMap.DOScale(
                mapBaseScale,
                zoomDuration
            )
            .SetEase(Ease.InOutCubic)
            .SetUpdate(true)
            .WaitForCompletion();

            mainMap.localScale = mapBaseScale;
        }

        // Begin combat.
        if (matchSetupSystem != null)
        {
            matchSetupSystem.StartGame();
        }
        else
        {
            Debug.LogWarning(
                "MatchSetupSystem is not assigned.",
                this
            );
        }

        isBattleTransitioning = false;
        battleCoroutine = null;

        // Preserves your current behavior of unlocking the next nodes
        // after starting combat. Move this to combat victory logic
        // instead if nodes should unlock only after winning.
        UnlockNextNodesFromCurrentSelection();
    }

    // =========================================================
    // MAP EVENTS
    // =========================================================

    public void MapViewLogic(MapView mapView)
    {
        if (mapView == null)
            return;

        switch (mapView.mapName)
        {
            case "Enemy":
                battleCoroutine = StartCoroutine(
                    StartBattleSequence(false)
                );
                break;

            case "Boss":
                MapManager.Instance.isBossLevel = true;

                battleCoroutine = StartCoroutine(
                    StartBattleSequence(true)
                );
                break;

            case "Event":
                QuestionManager.Instance.QuestionActivate();
                UnlockNextNodes(mapView);
                break;

            case "Heal":
                RewardManager.Instance.GiveRandomReward();
                SpawnHealthAtPlayer();
                UnlockNextNodes(mapView);
                break;

            default:
                Debug.Log(mapView.mapName);
                UnlockNextNodes(mapView);
                break;
        }
    }

    // =========================================================
    // UNLOCK NEXT NODES
    // =========================================================

    private void UnlockNextNodes(MapView mapView)
    {
        if (mapView == null)
            return;

        LockAllLevels();

        foreach (MapView nextNode in mapView.nextMapNode)
        {
            if (nextNode != null)
                nextNode.SetInteractable();
        }

        StartPlayerIdle();
    }

    private void UnlockNextNodesFromCurrentSelection()
    {
        if (selectedNode == null)
            return;

        UnlockNextNodes(selectedNode);
    }

    // =========================================================
    // HEALTH PICKUP
    // =========================================================

    public void SpawnHealthAtPlayer()
    {
        if (player == null || healthPrefab == null)
        {
            Debug.LogWarning(
                "Player or HealthPrefab is missing!",
                this
            );

            return;
        }

        Instantiate(
            healthPrefab,
            player.position,
            Quaternion.identity,
            transform
        );
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDisable()
    {
        StopPlayerIdle();

        movementSequence?.Kill();
        movementSequence = null;

        bannerSequence?.Kill();
        bannerSequence = null;

        nodePulseTween?.Kill();
        nodePulseTween = null;

        if (player != null)
            player.DOKill();

        if (mainMap != null)
        {
            mainMap.DOKill();
            mainMap.localScale = mapBaseScale;
        }

        if (bannerObject != null)
        {
            bannerObject.SetActive(false);
        }
    }
}
