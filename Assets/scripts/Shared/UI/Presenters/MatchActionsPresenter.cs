using Durak.Architecture.Shared.Domain;
using Durak.Architecture.Shared.FSM;
using UnityEngine;

namespace Durak.Architecture.Shared.UI.Presenters
{
    public sealed class MatchActionsPresenter
    {
        private readonly GameObject takeButton;
        private readonly GameObject bitoButton;
        private readonly GameObject transferZone;
        private readonly bool allowFollowUpBito;

        private bool localHasVotedBito;
        private ulong lastAttackerId = MatchDefaults.InvalidClientId;
        private ulong lastDefenderId = MatchDefaults.InvalidClientId;
        private MatchPhase lastPhase = MatchPhase.Bootstrap;

        public MatchActionsPresenter(
            GameObject takeButton,
            GameObject bitoButton,
            GameObject transferZone,
            bool allowFollowUpBito = true)
        {
            this.takeButton = takeButton;
            this.bitoButton = bitoButton;
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

            bool amAttacker = localClientId == context.Turn.AttackerId;
            bool amDefender = localClientId == context.Turn.DefenderId;
            bool allDefended = context.Table.AllCardsDefended;
            bool inTakePhase = context.Phase == MatchPhase.FollowUpThrowIn || context.Turn.IsDefenderTaking;
            bool canTakeWindow = hasCardsOnTable && !context.Turn.IsDefenderTaking;
            bool defendingWindow = canTakeWindow && !allDefended;
            bool canVoteAsThrower =
                localClientId != MatchDefaults.InvalidClientId &&
                !amDefender &&
                !localHasVotedBito;

            bool showTake = false;
            bool showBito = false;
            bool showTransfer = false;

            if (inTakePhase)
            {
                showTake = false;
                showTransfer = false;
                showBito = allowFollowUpBito && hasCardsOnTable && canVoteAsThrower;
            }
            else if (context.Phase == MatchPhase.Defending)
            {
                showTake = amDefender && canTakeWindow;
                showTransfer = amDefender && defendingWindow && canTransferNow;
                showBito = hasCardsOnTable && allDefended && canVoteAsThrower;
            }
            else if (context.Phase == MatchPhase.Attacking)
            {
                showTake = amDefender && canTakeWindow;
                showTransfer = amDefender && defendingWindow && canTransferNow;

                bool canPassOpeningAttack = amAttacker && !hasCardsOnTable;
                bool canVoteBitoAfterDefense = hasCardsOnTable && allDefended && canVoteAsThrower;
                showBito = canPassOpeningAttack || canVoteBitoAfterDefense;
            }

            SetActive(takeButton, showTake);
            SetActive(bitoButton, showBito);
            SetActive(transferZone, showTransfer);
        }

        public void HideAll()
        {
            SetActive(takeButton, false);
            SetActive(bitoButton, false);
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
