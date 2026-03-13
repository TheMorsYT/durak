using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Netcode;

public class CardMovementMP : NetworkBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public Transform defaultParent;
    private CanvasGroup canvasGroup;
    private CardMP myCard;
    private LayoutElement layoutElement;

    private int originalSiblingIndex;
    private Transform originalHand;
    private Canvas dragCanvas;

    void Awake()
    {
       
        canvasGroup = GetComponent<CanvasGroup>();
        layoutElement = GetComponent<LayoutElement>();
        dragCanvas = GetComponent<Canvas>();
        myCard = GetComponent<CardMP>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsOwner) { eventData.pointerDrag = null; return; }

        bool isAttackCard = transform.parent != null && GameManagerMP.Instance != null && transform.parent == GameManagerMP.Instance.tableArea;
        bool isDefendCard = transform.parent != null && transform.parent.GetComponent<CardMP>() != null;
        if (isAttackCard || isDefendCard) { eventData.pointerDrag = null; return; }

        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();

        defaultParent = transform.parent;
        originalHand = defaultParent;
        originalSiblingIndex = transform.GetSiblingIndex();

        layoutElement.ignoreLayout = true;


        if (dragCanvas != null)
        {
            dragCanvas.overrideSorting = true;
            dragCanvas.sortingOrder = 100;
        }

        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        if (layoutElement != null) layoutElement.ignoreLayout = false;

        if (dragCanvas != null) dragCanvas.overrideSorting = false;

        if (defaultParent == originalHand)
        {
            int newIndex = originalHand.childCount;
            for (int i = 0; i < originalHand.childCount; i++)
            {
                if (eventData.position.x < originalHand.GetChild(i).position.x)
                {
                    newIndex = i;
                    break;
                }
            }
            transform.SetSiblingIndex(newIndex);

            LayoutRebuilder.ForceRebuildLayoutImmediate(originalHand.GetComponent<RectTransform>());
        }
    }
}