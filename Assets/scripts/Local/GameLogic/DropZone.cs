using System.Collections.Generic;
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

    [HideInInspector] public Card targetAttackCard;

    public void OnDrop(PointerEventData eventData)
    {
        if (!TryBuildContext(eventData, out MatchControllerSP controller, out CardMovement movement, out Card card, out DropRole role))
        {
            return;
        }

        bool accepted = role == DropRole.Defender
            ? TryDefend(controller, movement, card, eventData)
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

    private bool TryDefend(MatchControllerSP controller, CardMovement movement, Card defendingCard, PointerEventData eventData)
    {
        Card targetCard = null;

        if (targetAttackCard != null)
        {
            targetCard = targetAttackCard;
        }
        else
        {
            if (!TryFindAttackCardAtDropPosition(controller, eventData, out targetCard) || targetCard == null)
            {
                return false;
            }
        }

        movement.defaultParent = targetCard.transform;
        movement.MarkPendingAction();
        
        return controller.RequestDefendCardFromPlayer(defendingCard, targetCard);
    }

    private static bool TryFindAttackCardAtDropPosition(MatchControllerSP controller, PointerEventData eventData, out Card attackCard)
    {
        attackCard = null;
        if (controller == null || eventData == null)
        {
            return false;
        }

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current?.RaycastAll(eventData, results);
        for (int i = 0; i < results.Count; i++)
        {
            if (TryResolveTableAttackCard(controller, results[i].gameObject, out attackCard))
            {
                return true;
            }
        }

        if (eventData.hovered != null)
        {
            for (int i = 0; i < eventData.hovered.Count; i++)
            {
                if (TryResolveTableAttackCard(controller, eventData.hovered[i], out attackCard))
                {
                    return true;
                }
            }
        }

        return TryFindAttackCardByRect(controller, eventData, out attackCard);
    }

    private static bool TryResolveTableAttackCard(MatchControllerSP controller, GameObject hitObject, out Card attackCard)
    {
        attackCard = null;
        if (controller == null || hitObject == null)
        {
            return false;
        }

        Card card = hitObject.GetComponentInParent<Card>();
        if (card == null || card.transform.parent != controller.TableArea || controller.IsAttackRootDefended(card.transform))
        {
            return false;
        }

        attackCard = card;
        return true;
    }

    private static bool TryFindAttackCardByRect(MatchControllerSP controller, PointerEventData eventData, out Card attackCard)
    {
        attackCard = null;
        List<Transform> roots = controller.GetTableAttackCards();
        Camera camera = eventData.pressEventCamera ?? eventData.enterEventCamera;
        int bestSibling = int.MinValue;

        for (int i = 0; i < roots.Count; i++)
        {
            Transform root = roots[i];
            if (root == null || controller.IsAttackRootDefended(root))
            {
                continue;
            }

            RectTransform rectTransform = root as RectTransform;
            Card card = root.GetComponent<Card>();
            if (rectTransform == null || card == null)
            {
                continue;
            }

            if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, eventData.position, camera))
            {
                continue;
            }

            int sibling = root.GetSiblingIndex();
            if (attackCard == null || sibling > bestSibling)
            {
                attackCard = card;
                bestSibling = sibling;
            }
        }

        return attackCard != null;
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
