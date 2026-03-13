using UnityEngine;
using UnityEngine.EventSystems;

public class TransferZoneMP : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        CardMP draggedCard = eventData.pointerDrag.GetComponent<CardMP>();
        CardMovementMP cardMovement = eventData.pointerDrag.GetComponent<CardMovementMP>();

        if (draggedCard != null && cardMovement != null && draggedCard.IsOwner)
        {
            if (GameManagerMP.Instance != null)
            {
                cardMovement.defaultParent = GameManagerMP.Instance.tableArea;
                GameManagerMP.Instance.TransferAttackServerRpc(draggedCard.NetworkObjectId);
            }
        }
    }
}