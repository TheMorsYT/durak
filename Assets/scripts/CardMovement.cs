using UnityEngine;
using UnityEngine.EventSystems;

public class CardMovement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public Transform defaultParent;
    private CanvasGroup canvasGroup;
    private Card myCard;

    private int originalSiblingIndex;
    private Transform originalHand;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        myCard = GetComponent<Card>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (GameManager.Instance != null && GameManager.Instance.isDealing)
        {
            eventData.pointerDrag = null;
            return;
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();

        defaultParent = transform.parent;
        originalHand = defaultParent;
        originalSiblingIndex = transform.GetSiblingIndex();

        transform.SetParent(transform.root);
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;

        if (GameManager.Instance != null && GameManager.Instance.CanTransfer(myCard))
        {
            if (GameManager.Instance.transferZone != null)
            {
                GameManager.Instance.transferZone.SetActive(true);
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (defaultParent == originalHand)
        {
            int newIndex = originalHand.childCount;

            for (int i = 0; i < originalHand.childCount; i++)
            {
                Transform sibling = originalHand.GetChild(i);

                if (eventData.position.x < sibling.position.x)
                {
                    newIndex = i;
                    break;
                }
            }

            transform.SetParent(originalHand);
            transform.SetSiblingIndex(newIndex);
        }
        else
        {
            transform.SetParent(defaultParent);
        }

        if (GameManager.Instance != null && GameManager.Instance.transferZone != null)
        {
            GameManager.Instance.transferZone.SetActive(false);
        }
    }
}