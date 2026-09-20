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
    private Collider cardCollider;
    private Camera cam;
    
    public void Setup(Card card)
    {
        sg = GetComponent<SortingGroup>();
        cardCollider = GetComponent<Collider>();
        Card = card;
        title.text = card.Title;
        description.text = card.Description;
        mana.text = card.Mana.ToString();
        imageSR.sprite = card.Image;
        cardBadgeSR.sprite = card.cardBadge;
    }
    
    private void Awake()
    {
        sg = GetComponent<SortingGroup>();
        cardCollider = GetComponent<Collider>();
        cam = Camera.main;
        
        if (wrapper == null)
        {
            Debug.LogError("Wrapper is not assigned on " + gameObject.name);
        }
    }

    // =======================
    //  NEW INPUT SYSTEM METHODS
    // =======================
    public void StartDrag(Vector2 screenPos)
    {
        if (!interactable || Interactions.Instance == null || !Interactions.Instance.PlayerCanInteract()) return;

        SoundManager.PlaySound(SoundType.CARDPRESS);

        if (CardViewCreator.Instance != null)
            CardViewCreator.Instance.CardNotInteractable(this);

        if (CardViewHoverSystem.Instance != null)
            CardViewHoverSystem.Instance.Hide();

        // Disable collider so it doesn't block raycasts
        if (cardCollider != null)
            cardCollider.enabled = false;

        // Common drag setup for ALL cards
        isDragging = true;
        if (Interactions.Instance != null)
            Interactions.Instance.PlayerIsDragging = true;

        if (wrapper != null)
            wrapper.SetActive(true);

        dragStartPosition = transform.position;
        dragStartRotation = transform.rotation;

        transform.rotation = Quaternion.Euler(0, 0, 0);
        
        // Convert screen position to world position
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10));
        transform.position = worldPos;

        // Start manual targeting if applicable
        if (Card.ManualTargetEffect != null)
        {
            if (ManualTargetSystem.Instance != null)
            {
                Debug.Log("ManualDetected");
                ManualTargetSystem.Instance.StartTargeting(transform.position);
            }
            else
            {
                Debug.Log("ManualNotDetected");
            }
        }
    }

    public void DragTo(Vector2 screenPos)
    {
        if (!interactable || !Interactions.Instance.PlayerCanInteract()) return;
        if (!isDragging) return;

        // Convert screen position to world position
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10));
        transform.position = worldPos;
    }

    public void EndDrag(Vector2 screenPos)
    {
        if (!interactable) return;

        // Re-enable collider
        if (cardCollider != null)
            cardCollider.enabled = true;

        if (CardViewCreator.Instance != null)
            CardViewCreator.Instance.CardInteractable();

        if (CardViewHoverSystem.Instance != null)
            CardViewHoverSystem.Instance.Hide();

        if (Interactions.Instance == null || !Interactions.Instance.PlayerCanInteract())
        {
            ResetCardPosition();
            return;
        }

        if (Card.ManualTargetEffect != null)
        {
            EnemyView target = ManualTargetSystem.Instance?.EndTargeting(screenPos);

            if (target != null && ManaSystem.Instance != null && ManaSystem.Instance.HasEnoughMana(Card.Mana))
            {
                PlayCardGA playCardGA = new(Card, target);
                if (ActionSystem.Instance != null)
                    ActionSystem.Instance.Perform(playCardGA);
            }
            else
            {
                ResetCardPosition();
            }

            isDragging = false;
            if (Interactions.Instance != null)
                Interactions.Instance.PlayerIsDragging = false;
        }
        else
        {
            Ray ray = cam.ScreenPointToRay(screenPos);

            if (isDragging && ManaSystem.Instance != null && ManaSystem.Instance.HasEnoughMana(Card.Mana) &&
                Physics.Raycast(ray, out RaycastHit hit, 100f, dropLayer))
            {
                if (ActionSystem.Instance != null)
                    ActionSystem.Instance.Perform(new PlayCardGA(Card));
            }
            else
            {
                ResetCardPosition();
            }

            isDragging = false;
            if (Interactions.Instance != null)
                Interactions.Instance.PlayerIsDragging = false;
        }
    }

    private void ResetCardPosition()
    {
        transform.position = dragStartPosition;
        transform.rotation = dragStartRotation;
    }

    // =======================
    //  MOUSE EVENTS (Keep for hover functionality)
    // =======================
    void OnMouseEnter()
    {
        if (!interactable) return;
        if (Interactions.Instance == null || !Interactions.Instance.PlayerCanHover()) return;
        if (CardViewHoverSystem.Instance == null) return;

        if (wrapper != null)
            wrapper.SetActive(false);

        Vector3 pos = transform.position;
        pos.y = -2f;
        pos.z = transform.position.z - 0.5f;
        CardViewHoverSystem.Instance.Show(Card, pos);
    }

    void OnMouseExit()
    {
        if (!interactable) return;
        if (Interactions.Instance != null && !Interactions.Instance.PlayerCanHover()) return;

        if (CardViewHoverSystem.Instance != null)
            CardViewHoverSystem.Instance.Hide();
        if (wrapper != null)
            wrapper.SetActive(true);
    }

    private void OnDisable()
    {
        if (isDragging)
        {
            isDragging = false;
            if (Interactions.Instance != null)
                Interactions.Instance.PlayerIsDragging = false;
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();
        if (isDragging)
        {
            isDragging = false;
            if (Interactions.Instance != null)
                Interactions.Instance.PlayerIsDragging = false;
        }
    }
}