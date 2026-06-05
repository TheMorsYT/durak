using Durak.Architecture.Shared.Domain;
using Durak.Architecture.Shared.FSM;
using UnityEngine;

namespace Durak.Architecture.Shared.UI.Presenters
{
    public sealed class MatchActionsPresenter
    {
        private readonly GameObject takeButton;
        private readonly GameObject bitoButton;
        private readonly GameObject passButton;
        private readonly GameObject transferZone;
        private readonly bool allowFollowUpBito;

        private bool localHasVotedBito;
        private ulong lastAttackerId = MatchDefaults.InvalidClientId;
        private ulong lastDefenderId = MatchDefaults.InvalidClientId;
        private MatchPhase lastPhase = MatchPhase.Bootstrap;

        public MatchActionsPresenter(
            GameObject takeButton,
            GameObject bitoButton,
            GameObject passButton,
            GameObject transferZone,
            bool allowFollowUpBito = true)
        {
            this.takeButton = takeButton;
            this.bitoButton = bitoButton;
            this.passButton = passButton;
            this.transferZone = transferZone;
            this.allowFollowUpBito = allowFollowUpBito;
        }

        public void MarkLocalBitoVote()
        {
            localHasVotedBito = true;
        }

        public void Refresh(MatchContext context, ulong localClientId, bool canTransferNow)
        {
            if (context == null || context.Seats == null)
            {
                HideAll();
                return;
            }

            ResetVoteIfTurnChanged(context);
            if (!context.Turn.AttackerPassed || context.Table.AttackCardsCount <= 0)
            {
                localHasVotedBito = false;
            }

            bool hasCardsOnTable = context.Table.AttackCardsCount > 0;
            if (context.IsGameOver || context.IsDealInProgress)
            {
                HideAll();
                return;
            }

            if (!hasCardsOnTable && context.Phase != MatchPhase.Attacking)
            {
                HideAll();
                return;
            }


            bool amAttacker = (localClientId != MatchDefaults.InvalidClientId) && 
                              (localClientId == context.Turn.AttackerId);
            bool amDefender = (localClientId != MatchDefaults.InvalidClientId) && 
                              (localClientId == context.Turn.DefenderId);
            
            bool allDefended = context.Table.AllCardsDefended;
            bool defenderTaking = context.Turn.IsDefenderTaking;
            bool inFollowUpTossPhase = context.Phase == MatchPhase.FollowUpThrowIn || defenderTaking;

            bool showTake = false;
            bool showBito = false;
            bool showPass = false;
            bool showTransfer = false;

            if (inFollowUpTossPhase)
            {
                showTake = false;
                showTransfer = false;
                showBito = false;
                showPass = allowFollowUpBito && hasCardsOnTable && amAttacker && !localHasVotedBito;
            }
            else if (context.Phase == MatchPhase.Defending)
            {
                showTake = amDefender && hasCardsOnTable && !allDefended;
                showTransfer = amDefender && !allDefended && canTransferNow;
                showBito = amAttacker && hasCardsOnTable && allDefended && !localHasVotedBito;
            }
            else if (context.Phase == MatchPhase.Attacking)
            {
                showTake = amDefender && hasCardsOnTable && !allDefended;
                showTransfer = amDefender && !allDefended && canTransferNow;
                showBito = amAttacker && hasCardsOnTable && allDefended && !localHasVotedBito;
            }

            SetActive(takeButton, showTake);
            SetActive(bitoButton, showBito);
            SetActive(passButton, showPass);
            SetActive(transferZone, showTransfer);
        }

        public void HideAll()
        {
            SetActive(takeButton, false);
            SetActive(bitoButton, false);
            SetActive(passButton, false);
            SetActive(transferZone, false);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private static int ResolveLocalCards(MatchContext context, ulong localClientId)
        {
            for (int i = 0; i < context.Seats.Count; i++)
            {
                SeatSnapshot seat = context.Seats[i];
                if (seat.ClientId == localClientId)
                {
                    return seat.CardCount;
                }
            }

            return 0;
        }

        private void ResetVoteIfTurnChanged(MatchContext context)
        {
            if (context.Turn.AttackerId != lastAttackerId ||
                context.Turn.DefenderId != lastDefenderId ||
                context.Phase != lastPhase)
            {
                localHasVotedBito = false;
            }

            lastAttackerId = context.Turn.AttackerId;
            lastDefenderId = context.Turn.DefenderId;
            lastPhase = context.Phase;
        }
    }
}
