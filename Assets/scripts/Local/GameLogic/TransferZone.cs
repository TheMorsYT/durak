using UnityEngine;
using UnityEngine.EventSystems;

public class TransferZoneHandler : MonoBehaviour, IDropHandler
{
    private void OnEnable()
    {
        transform.SetAsLastSibling();
    }

    public void OnDrop(PointerEventData eventData)
    {
        Card draggedCard = eventData.pointerDrag.GetComponent<Card>();
        CardMovement cardMovement = eventData.pointerDrag.GetComponent<CardMovement>();

        if (draggedCard != null && cardMovement != null)
        {
            cardMovement.defaultParent = GameManager.Instance.tableArea;

            GameManager.Instance.TransferAttack(draggedCard);
        }
    }
}