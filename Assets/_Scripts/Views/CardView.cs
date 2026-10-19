using UnityEngine;
using UnityEngine.Rendering;
using TMPro;
using DG.Tweening;

public class CardView : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private TMP_Text mana;
    [SerializeField] private SpriteRenderer imageSR;

    public GameObject wrapper;

    [SerializeField] private SpriteRenderer cardBaseSR;
    [SerializeField] private SpriteRenderer cardBadgeSR;

    [HideInInspector] public bool interactable = true;
    [HideInInspector] public SortingGroup sg;

    [SerializeField] private LayerMask dropLayer;

    public Card Card { get; private set; }

    private Vector3 dragStartPosition;
    private Quaternion dragStartRotation;

    private bool isDragging = false;
    private bool hasDragged = false;

    private Vector2 dragStartScreenPos;

    private Collider cardCollider;
    private Camera cam;


    // ============================================================
    // SETUP
    // ============================================================

    public void Setup(Card card)
    {
        sg = GetComponent<SortingGroup>();
        cardCollider = GetComponent<Collider>();

        Card = card;

        if (title != null)
            title.text = card.Title;

        if (description != null)
            description.text = card.Description;

        if (mana != null)
            mana.text = card.Mana.ToString();

        bool hasCardFace = imageSR != null && card.Image != null;

        if (hasCardFace)
        {
            // Card faces are complete card images (frame + art + category label).
            // They come from different source resolutions, so normalise to the card
            // base's world width instead of relying on pixels-per-unit.
            imageSR.sprite = card.Image;

            float targetWidth = 4f;
            Vector3 targetPosition = new Vector3(0f, -0.8f, 0f);

            if (cardBaseSR != null)
            {
                targetPosition = cardBaseSR.transform.localPosition;
                if (cardBaseSR.sprite != null)
                {
                    targetWidth = cardBaseSR.sprite.rect.width /
                                  Mathf.Max(1f, cardBaseSR.sprite.pixelsPerUnit);
                }
            }

            float spriteWidth = card.Image.rect.width /
                                Mathf.Max(1f, card.Image.pixelsPerUnit);
            float fit = targetWidth / Mathf.Max(0.0001f, spriteWidth);

            imageSR.transform.localScale = new Vector3(fit, fit, 1f);
            imageSR.transform.localPosition = targetPosition;
        }

        // The face carries its own frame, so hide the static base — but only when a
        // face was actually drawn. Otherwise a card whose sprite failed to load would
        // render as nothing at all; this way it still shows the frame.
        if (cardBaseSR != null)
        {
            cardBaseSR.enabled = !hasCardFace;
            if (!hasCardFace)
            {
                Debug.LogWarning(
                    $"[CardView] '{card.Title}' has no card image; showing the base frame instead.");
            }
        }

        if (cardBadgeSR != null)
            cardBadgeSR.sprite = card.cardBadge;
    }


    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        sg = GetComponent<SortingGroup>();
        cardCollider = GetComponent<Collider>();
        cam = Camera.main;

        if (wrapper == null)
        {
            Debug.LogError(
                $"[CardView] Wrapper is not assigned on {gameObject.name}"
            );
        }
    }


    // ============================================================
    // START DRAG / TAP
    // ============================================================

    public void StartDrag(Vector2 screenPos)
    {
        if (!interactable)
            return;

        if (Interactions.Instance == null)
            return;

        if (!Interactions.Instance.PlayerCanInteract())
            return;


        Debug.Log(
            $"[CardView] START DRAG: {gameObject.name}"
        );


        SoundManager.Instance.PlaySound(SoundType.CARDPRESS);


        // Prevent other cards from being interacted with
        if (CardViewCreator.Instance != null)
        {
            CardViewCreator.Instance.CardNotInteractable(this);
        }


        // Hide any existing hover card first
        if (CardViewHoverSystem.Instance != null)
        {
            CardViewHoverSystem.Instance.Hide();
        }


        // Disable collider while dragging
        if (cardCollider != null)
        {
            cardCollider.enabled = false;
        }


        dragStartScreenPos = screenPos;
        hasDragged = false;
        isDragging = true;


        if (Interactions.Instance != null)
        {
            Interactions.Instance.PlayerIsDragging = true;
        }


        // Make sure the normal card is visible
        if (wrapper != null)
        {
            wrapper.SetActive(true);

            Debug.Log(
                $"[CardView] Wrapper after StartDrag = {wrapper.activeSelf}"
            );
        }


        // Save original transform
        dragStartPosition = transform.position;
        dragStartRotation = transform.rotation;


        // Reset rotation while dragging
        transform.rotation = Quaternion.Euler(0, 0, 0);


        // Move card to touch position
        if (cam == null)
        {
            cam = Camera.main;
        }


        if (cam != null)
        {
            Vector3 worldPos = cam.ScreenToWorldPoint(
                new Vector3(
                    screenPos.x,
                    screenPos.y,
                    10f
                )
            );

            Debug.Log(
                $"[CardView] Position BEFORE = {dragStartPosition}"
            );

            Debug.Log(
                $"[CardView] Screen = {screenPos}, " +
                $"World = {worldPos}, " +
                $"Camera = {cam.transform.position}"
            );

            transform.position = worldPos;

            Debug.Log(
                $"[CardView] Position AFTER = {transform.position}, " +
                $"Scale = {transform.localScale}"
            );
        }


        // ========================================================
        // MANUAL TARGETING
        // ========================================================

        if (Card != null && Card.ManualTargetEffect != null)
        {
            if (ManualTargetSystem.Instance != null)
            {
                Debug.Log("[CardView] ManualDetected");

                ManualTargetSystem.Instance.StartTargeting(
                    transform.position
                );
            }
            else
            {
                Debug.Log("[CardView] ManualNotDetected");
            }
        }
    }


    // ============================================================
    // DRAG
    // ============================================================

    public void DragTo(Vector2 screenPos)
    {
        if (!interactable)
            return;

        if (Interactions.Instance == null)
            return;

        if (!Interactions.Instance.PlayerCanInteract())
            return;

        if (!isDragging)
            return;


        if (cam == null)
        {
            cam = Camera.main;
        }


        if (cam != null)
        {
            Vector3 worldPos = cam.ScreenToWorldPoint(
                new Vector3(
                    screenPos.x,
                    screenPos.y,
                    10f
                )
            );

            transform.position = worldPos;
        }


        const float screenDragThreshold = 15f;


        if (!hasDragged &&
            Vector2.Distance(
                screenPos,
                dragStartScreenPos
            ) > screenDragThreshold)
        {
            hasDragged = true;
        }
    }


    // ============================================================
    // END DRAG
    // ============================================================

    
    public void EndDrag(Vector2 screenPos)
    {
        // Always clean up the drag state, even if the card
        // became non-interactable during the drag.
        if (!isDragging)
            return;

        if (cardCollider != null)
            cardCollider.enabled = true;

        if (CardViewCreator.Instance != null)
            CardViewCreator.Instance.CardInteractable();

        bool canInteract =
            interactable &&
            Interactions.Instance != null &&
            Interactions.Instance.PlayerCanInteract();

        if (!canInteract)
        {
            ResetCardPosition();

            if (wrapper != null)
                wrapper.SetActive(true);

            isDragging = false;
            hasDragged = false;

            if (Interactions.Instance != null)
                Interactions.Instance.PlayerIsDragging = false;

            if (CardViewHoverSystem.Instance != null)
                CardViewHoverSystem.Instance.Hide();

            return;
        }

        // TAP: show the enlarged card preview.
        if (!hasDragged)
        {
            ResetCardPosition();

            if (wrapper != null)
                wrapper.SetActive(true);

            if (CardViewHoverSystem.Instance != null)
            {
                Vector3 pos = transform.position;
                pos.y = -2f;
                pos.z = transform.position.z - 0.5f;

                CardViewHoverSystem.Instance.Show(Card, pos);
            }

            isDragging = false;
            hasDragged = false;
            Interactions.Instance.PlayerIsDragging = false;
            return;
        }

        // MANUAL TARGETING
        if (Card != null && Card.ManualTargetEffect != null)
        {
            EnemyView target =
                ManualTargetSystem.Instance?.EndTargeting(screenPos);

            if (target != null &&
                ManaSystem.Instance != null &&
                ManaSystem.Instance.HasEnoughMana(Card.Mana) &&
                ActionSystem.Instance != null &&
                Interactions.Instance.PlayerCanInteract())
            {
                ActionSystem.Instance.Perform(
                    new PlayCardGA(Card, target)
                );
            }
            else
            {
                ResetCardPosition();
            }
        }
        // NORMAL CARD
        else
        {
            bool played = false;

            if (cam == null)
                cam = Camera.main;

            if (cam != null &&
                ManaSystem.Instance != null &&
                ManaSystem.Instance.HasEnoughMana(Card.Mana) &&
                ActionSystem.Instance != null &&
                Interactions.Instance.PlayerCanInteract())
            {
                Ray ray = cam.ScreenPointToRay(screenPos);

                if (Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    100f,
                    dropLayer))
                {
                    ActionSystem.Instance.Perform(
                        new PlayCardGA(Card)
                    );

                    played = true;
                }
            }

            if (!played)
                ResetCardPosition();
        }

        isDragging = false;
        hasDragged = false;
        Interactions.Instance.PlayerIsDragging = false;

        if (wrapper != null)
            wrapper.SetActive(true);
    }

    // ============================================================
    // RESET POSITION
    // ============================================================

    private void ResetCardPosition()
    {
        transform.position = dragStartPosition;
        transform.rotation = dragStartRotation;
    }


    // ============================================================
    // DESKTOP HOVER
    // ============================================================

    private void OnMouseEnter()
    {
        if (!interactable)
            return;

        if (Interactions.Instance == null ||
            !Interactions.Instance.PlayerCanHover())
            return;

        if (CardViewHoverSystem.Instance == null)
            return;


        // IMPORTANT:
        // Do NOT disable the wrapper here.
        //
        // Previously this was:
        //
        // wrapper.SetActive(false);
        //
        // That caused the mobile card to become invisible because
        // Unity can trigger mouse-style events during touch input.


        Vector3 pos = transform.position;

        pos.y = -2f;
        pos.z = transform.position.z - 0.5f;


        CardViewHoverSystem.Instance.Show(
            Card,
            pos
        );
    }


    // ============================================================
    // DESKTOP HOVER EXIT
    // ============================================================

    private void OnMouseExit()
    {
        if (!interactable)
            return;

        if (Interactions.Instance != null &&
            !Interactions.Instance.PlayerCanHover())
            return;


        if (CardViewHoverSystem.Instance != null)
        {
            CardViewHoverSystem.Instance.Hide();
        }


        // Keep the actual card visible.
        if (wrapper != null)
        {
            wrapper.SetActive(true);
        }
    }


    // ============================================================
    // DISABLE
    // ============================================================

    private void OnDisable()
    {
        if (isDragging)
        {
            isDragging = false;

            if (Interactions.Instance != null)
            {
                Interactions.Instance.PlayerIsDragging = false;
            }
        }
    }


    // ============================================================
    // DESTROY
    // ============================================================

    private void OnDestroy()
    {
        transform.DOKill();

        if (isDragging)
        {
            isDragging = false;

            if (Interactions.Instance != null)
            {
                Interactions.Instance.PlayerIsDragging = false;
            }
        }
    }
}