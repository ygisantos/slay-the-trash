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

        if (imageSR != null)
            imageSR.sprite = card.Image;

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


        SoundManager.PlaySound(SoundType.CARDPRESS);


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
        if (!interactable)
            return;


        // Re-enable collider
        if (cardCollider != null)
        {
            cardCollider.enabled = true;
        }


        // Make all cards interactable again
        if (CardViewCreator.Instance != null)
        {
            CardViewCreator.Instance.CardInteractable();
        }


        if (Interactions.Instance == null ||
            !Interactions.Instance.PlayerCanInteract())
        {
            ResetCardPosition();

            if (wrapper != null)
                wrapper.SetActive(true);

            isDragging = false;

            if (Interactions.Instance != null)
                Interactions.Instance.PlayerIsDragging = false;

            return;
        }


        // ========================================================
        // TAP ONLY
        // ========================================================

        if (!hasDragged)
        {
            ResetCardPosition();


            // Restore normal card
            if (wrapper != null)
            {
                wrapper.SetActive(true);
            }


            // ----------------------------------------------------
            // IMPORTANT:
            //
            // A TAP should show the enlarged card so the player
            // can read the description.
            // ----------------------------------------------------

            if (CardViewHoverSystem.Instance != null)
            {
                Vector3 pos = transform.position;

                pos.y = -2f;
                pos.z = transform.position.z - 0.5f;

                CardViewHoverSystem.Instance.Show(
                    Card,
                    pos
                );
            }


            isDragging = false;
            hasDragged = false;


            if (Interactions.Instance != null)
            {
                Interactions.Instance.PlayerIsDragging = false;
            }


            return;
        }


        // ========================================================
        // MANUAL TARGETING
        // ========================================================

        if (Card != null && Card.ManualTargetEffect != null)
        {
            EnemyView target =
                ManualTargetSystem.Instance?.EndTargeting(screenPos);


            if (target != null &&
                ManaSystem.Instance != null &&
                ManaSystem.Instance.HasEnoughMana(Card.Mana))
            {
                PlayCardGA playCardGA =
                    new PlayCardGA(Card, target);

                if (ActionSystem.Instance != null)
                {
                    ActionSystem.Instance.Perform(playCardGA);
                }
            }
            else
            {
                ResetCardPosition();
            }


            isDragging = false;


            if (Interactions.Instance != null)
            {
                Interactions.Instance.PlayerIsDragging = false;
            }
        }


        // ========================================================
        // NORMAL CARD
        // ========================================================

        else
        {
            if (cam == null)
            {
                cam = Camera.main;
            }


            if (cam != null)
            {
                Ray ray =
                    cam.ScreenPointToRay(screenPos);


                if (ManaSystem.Instance != null &&
                    ManaSystem.Instance.HasEnoughMana(Card.Mana) &&
                    Physics.Raycast(
                        ray,
                        out RaycastHit hit,
                        100f,
                        dropLayer
                    ))
                {
                    if (ActionSystem.Instance != null)
                    {
                        ActionSystem.Instance.Perform(
                            new PlayCardGA(Card)
                        );
                    }
                }
                else
                {
                    ResetCardPosition();
                }
            }
            else
            {
                ResetCardPosition();
            }


            isDragging = false;


            if (Interactions.Instance != null)
            {
                Interactions.Instance.PlayerIsDragging = false;
            }
        }
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