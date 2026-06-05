using Durak.Architecture.Singleplayer.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardMovement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static bool IsDraggingTransferCandidate { get; private set; }
    public bool IsDraggingCard => isDragging;
    public Transform DragStartParent => originalHand;

    [HideInInspector] public Transform defaultParent;

    private CanvasGroup canvasGroup;
    private LayoutElement layoutElement;
    private Canvas dragCanvas;

    private Transform originalHand;
    private int originalSiblingIndex;
    private Transform dragLayerParent;

    private bool pendingAction;
    private bool localRejectRequested;
    private bool isDragging;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        layoutElement = GetComponent<LayoutElement>();
        dragCanvas = GetComponent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        MatchControllerSP controller = MatchControllerSP.Instance;
        if (controller == null || !controller.IsInLocalPlayerHand(transform) || controller.IsCardOnTable(transform) || controller.IsDealInProgress || controller.IsGameOver)
        {
            eventData.pointerDrag = null;
            return;
        }

        Card card = GetComponent<Card>();
        IsDraggingTransferCandidate = card != null && controller.CanLocalPlayerTransfer(card);

        defaultParent = transform.parent;
        originalHand = defaultParent;
        originalSiblingIndex = transform.GetSiblingIndex();
        pendingAction = false;
        localRejectRequested = false;
        isDragging = true;
        layoutElement = GetComponent<LayoutElement>();

        Canvas rootCanvas = originalHand != null
            ? originalHand.GetComponentInParent<Canvas>()?.rootCanvas
            : GetComponentInParent<Canvas>()?.rootCanvas;
        dragLayerParent = rootCanvas != null ? rootCanvas.transform : transform.root;
        if (dragLayerParent != null)
        {
            transform.SetParent(dragLayerParent, true);
        }

        transform.SetAsLastSibling();

        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = true;
        }

        if (dragCanvas != null)
        {
            dragCanvas.overrideSorting = true;
            dragCanvas.sortingOrder = 100;
        }

        canvasGroup.blocksRaycasts = false;
        SoundManager.Instance?.PlayClick();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        IsDraggingTransferCandidate = false;
        canvasGroup.blocksRaycasts = true;

        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = false;
        }

        if (dragCanvas != null)
        {
            dragCanvas.overrideSorting = false;
        }

        if (originalHand == null)
        {
            return;
        }

        if (localRejectRequested)
        {
            SnapBackToOriginalHand(originalSiblingIndex);
            localRejectRequested = false;
            pendingAction = false;
            return;
        }

        if (pendingAction)
        {
            Transform targetParent = defaultParent != null ? defaultParent : originalHand;
            transform.SetParent(targetParent, false);
            ApplyFixedCardLayout(transform);
            ForceRebuild(originalHand);
            ForceRebuild(targetParent);
            return;
        }

        if (defaultParent != originalHand)
        {
            SnapBackToOriginalHand(originalSiblingIndex);
            return;
        }

        int newIndex = originalHand.childCount;
        if (eventData != null)
        {
            for (int i = 0; i < originalHand.childCount; i++)
            {
                if (eventData.position.x < originalHand.GetChild(i).position.x)
                {
                    newIndex = i;
                    break;
                }
            }
        }

        transform.SetParent(originalHand, false);
        ApplyFixedCardLayout(transform);
        transform.SetSiblingIndex(newIndex);
        ForceRebuild(originalHand);

        if (PlayerPrefs.GetInt("SortMethod", 0) != 0)
        {
            MatchControllerSP.Instance?.SortPlayerHand();
        }
    }

    public void MarkPendingAction()
    {
        pendingAction = true;
        localRejectRequested = false;
    }

    public void RejectLocally()
    {
        localRejectRequested = true;
        pendingAction = false;
        if (!isDragging)
        {
            SnapBackToOriginalHand(originalSiblingIndex);
        }
    }

    private void OnDisable()
    {
        if (isDragging)
        {
            IsDraggingTransferCandidate = false;
        }
    }

    private void LateUpdate()
    {
        if (!pendingAction || originalHand == null)
        {
            return;
        }

        if (transform.parent != originalHand)
        {
            pendingAction = false;
            localRejectRequested = false;
        }
    }

    private void SnapBackToOriginalHand(int siblingIndex)
    {
        if (originalHand == null)
        {
            return;
        }

        transform.SetParent(originalHand, false);
        ApplyFixedCardLayout(transform);
        int clamped = Mathf.Clamp(siblingIndex, 0, Mathf.Max(0, originalHand.childCount - 1));
        transform.SetSiblingIndex(clamped);
        ForceRebuild(originalHand);
    }

    private static void ApplyFixedCardLayout(Transform cardTransform)
    {
        if (cardTransform == null)
        {
            return;
        }

        RectTransform rectTransform = cardTransform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = Card.FixedSize;
            rectTransform.localScale = Vector3.one;
        }
        else
        {
            cardTransform.localScale = Vector3.one;
        }

        LayoutElement fixedLayout = cardTransform.GetComponent<LayoutElement>();
        if (fixedLayout == null)
        {
            fixedLayout = cardTransform.gameObject.AddComponent<LayoutElement>();
        }

        fixedLayout.minWidth = Card.FixedSize.x;
        fixedLayout.minHeight = Card.FixedSize.y;
        fixedLayout.preferredWidth = Card.FixedSize.x;
        fixedLayout.preferredHeight = Card.FixedSize.y;
        fixedLayout.flexibleWidth = 0f;
        fixedLayout.flexibleHeight = 0f;
        fixedLayout.ignoreLayout = false;
    }

    private static void ForceRebuild(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        RectTransform rect = parent.GetComponent<RectTransform>();
        if (rect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
    }
}
