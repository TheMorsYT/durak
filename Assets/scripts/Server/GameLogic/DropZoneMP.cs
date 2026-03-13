using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using Unity.Netcode;

public class DropZoneMP : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        CardMovementMP cardMove = eventData.pointerDrag.GetComponent<CardMovementMP>();
        CardMP cardData = eventData.pointerDrag.GetComponent<CardMP>();

        if (cardMove != null && cardData != null && cardData.IsOwner)
        {
            GameManagerMP gm = GameManagerMP.Instance;
            if (gm == null) return;

            ulong myId = NetworkManager.Singleton.LocalClientId;
            bool amIAttacker = (myId == gm.currentAttackerId.Value);
            bool amIDefender = (myId == gm.currentDefenderId.Value);

            bool isOther = (!amIAttacker && !amIDefender);

            if (amIDefender)
            {
                foreach (Transform tableCard in gm.tableArea)
                {
                    CardMP targetCard = tableCard.GetComponent<CardMP>();

                    if (targetCard != null && tableCard.childCount == 0)
                    {
                        if (CanPlayerBeat(cardData, targetCard, gm.trumpSuit))
                        {
                            cardMove.defaultParent = tableCard;
                            gm.DefendCardServerRpc(cardData.NetworkObjectId, targetCard.NetworkObjectId);
                            break;
                        }
                    }
                }
            }
            else
            {
                if (isOther && !gm.attackerPassed.Value) return;

                var cardsOnTable = gm.tableArea.GetComponentsInChildren<CardMP>();
                bool canToss = false;

                if (cardsOnTable.Length == 0 && amIAttacker)
                {
                    canToss = true; 
                }
                else if (cardsOnTable.Length > 0 && cardsOnTable.Length < 6)
                {
                    var tableRanks = cardsOnTable.Select(c => c.value).Distinct();
                    if (tableRanks.Contains(cardData.value)) canToss = true;
                }

                if (canToss)
                {
                    cardMove.defaultParent = this.transform;
                    gm.PlayCardServerRpc(cardData.NetworkObjectId);
                }
            }
        }
    }

    private bool CanPlayerBeat(CardMP playerCard, CardMP tableCard, CardMP.CardSuit trump)
    {
        if (playerCard.value == CardMP.CardValue.Joker) return true;
        if (tableCard.value == CardMP.CardValue.Joker) return false;

        if (playerCard.suit == trump && tableCard.suit != trump) return true;
        if (playerCard.suit == tableCard.suit) return playerCard.value > tableCard.value;
        return false;
    }
}