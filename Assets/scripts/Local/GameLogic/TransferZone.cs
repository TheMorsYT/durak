using Durak.Architecture.Singleplayer.Core;
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
        if (eventData?.pointerDrag == null)
        {
            return;
        }

        MatchControllerSP controller = MatchControllerSP.Instance;
        if (controller == null || controller.IsDealInProgress || controller.IsGameOver)
        {
            return;
        }

        Card card = eventData.pointerDrag.GetComponent<Card>();
        CardMovement movement = eventData.pointerDrag.GetComponent<CardMovement>();
        if (card == null || movement == null)
        {
            return;
        }

        bool draggedFromLocalHand = movement.IsDraggingCard && movement.DragStartParent == controller.PlayerHand;
        bool inLocalHandNow = controller.IsInLocalPlayerHand(card.transform);
        if ((!inLocalHandNow && !draggedFromLocalHand) ||
            controller.IsCardOnTable(card.transform) ||
            !controller.CanLocalPlayerTransfer(card))
        {
            movement.RejectLocally();
            return;
        }

        movement.defaultParent = controller.TableArea;
        movement.MarkPendingAction();
        bool accepted = controller.RequestTransferFromPlayer(card);
        if (!accepted)
        {
            movement.RejectLocally();
        }
    }
}
