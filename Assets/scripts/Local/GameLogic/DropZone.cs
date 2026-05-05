using Durak.Architecture.Singleplayer.Core;
using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler
{
    private enum DropRole
    {
        Defender,
        Attacker
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!TryBuildContext(eventData, out MatchControllerSP controller, out CardMovement movement, out Card card, out DropRole role))
        {
            return;
        }

        bool accepted = role == DropRole.Defender
            ? TryDefend(controller, movement, card)
            : TryAttackOrToss(controller, movement, card);

        if (!accepted)
        {
            movement.RejectLocally();
        }
    }

    private static bool TryBuildContext(
        PointerEventData eventData,
        out MatchControllerSP controller,
        out CardMovement movement,
        out Card card,
        out DropRole role)
    {
        controller = MatchControllerSP.Instance;
        movement = null;
        card = null;
        role = DropRole.Attacker;

        if (eventData?.pointerDrag == null || controller == null || controller.IsDealInProgress || controller.IsGameOver)
        {
            return false;
        }

        movement = eventData.pointerDrag.GetComponent<CardMovement>();
        card = eventData.pointerDrag.GetComponent<Card>();
        if (movement == null || card == null)
        {
            return false;
        }

        bool draggedFromLocalHand = movement.IsDraggingCard && movement.DragStartParent == controller.PlayerHand;
        bool inLocalHandNow = controller.IsInLocalPlayerHand(card.transform);
        if ((!inLocalHandNow && !draggedFromLocalHand) || controller.IsCardOnTable(card.transform))
        {
            return false;
        }

        role = controller.Context != null && controller.Context.Turn.DefenderId == MatchControllerSP.LocalPlayerId
            ? DropRole.Defender
            : DropRole.Attacker;

        return true;
    }

    private static bool TryDefend(MatchControllerSP controller, CardMovement movement, Card defendingCard)
    {
        if (!controller.TryFindLocalDefenseTarget(defendingCard, out Card targetCard) || targetCard == null)
        {
            return false;
        }

        movement.defaultParent = targetCard.transform;
        movement.MarkPendingAction();
        return controller.RequestDefendCardFromPlayer(defendingCard, targetCard);
    }

    private static bool TryAttackOrToss(MatchControllerSP controller, CardMovement movement, Card card)
    {
        if (!controller.CanLocalPlayerPlayCard(card))
        {
            return false;
        }

        movement.defaultParent = controller.TableArea;
        movement.MarkPendingAction();
        return controller.RequestPlayCardFromPlayer(card);
    }
}
