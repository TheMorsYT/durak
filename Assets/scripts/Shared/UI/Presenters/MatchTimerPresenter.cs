using System.Collections.Generic;
using Durak.Architecture.Shared.Domain;
using Durak.Architecture.Shared.Events;
using Durak.Architecture.Shared.FSM;
using UnityEngine;
using UnityEngine.UI;

namespace Durak.Architecture.Shared.UI.Presenters
{
    public sealed class MatchTimerPresenter
    {
        private readonly Image[] attackRings;
        private readonly Image[] defendRings;

        private readonly float[] attackCurrentFill;
        private readonly float[] attackTargetFill;
        private readonly float[] defendCurrentFill;
        private readonly float[] defendTargetFill;
        private readonly bool[] attackVisible;
        private readonly bool[] defendVisible;

        private readonly float fillLerpSpeed;
        private readonly int slotCount;

        public MatchTimerPresenter(Image[] attackRings, Image[] defendRings, float fillLerpSpeed = 10f)
        {
            this.attackRings = attackRings;
            this.defendRings = defendRings;
            this.fillLerpSpeed = Mathf.Max(0.01f, fillLerpSpeed);
            slotCount = Mathf.Max(
                attackRings != null ? attackRings.Length : 0,
                defendRings != null ? defendRings.Length : 0,
                1);

            attackCurrentFill = new float[slotCount];
            attackTargetFill = new float[slotCount];
            defendCurrentFill = new float[slotCount];
            defendTargetFill = new float[slotCount];
            attackVisible = new bool[slotCount];
            defendVisible = new bool[slotCount];
        }

        public void SetVisibilityFromContext(MatchContext context, IReadOnlyList<int> seatOrder = null)
        {
            ClearVisibility();
            if (context == null || context.IsGameOver || context.IsDealInProgress)
            {
                ApplyVisibleStateImmediate();
                return;
            }

            int attackerSlot = ResolveSlot(context, context.Turn.AttackerId, seatOrder);
            int defenderSlot = ResolveSlot(context, context.Turn.DefenderId, seatOrder);

            switch (context.Phase)
            {
                case MatchPhase.Attacking:
                    SetAttackVisible(attackerSlot, true, attackTargetFill[SafeIndex(attackerSlot)] <= 0f ? 1f : attackTargetFill[SafeIndex(attackerSlot)]);
                    SetDefendVisible(defenderSlot, true, 1f);
                    break;

                case MatchPhase.Defending:
                    SetDefendVisible(defenderSlot, true, defendTargetFill[SafeIndex(defenderSlot)] <= 0f ? 1f : defendTargetFill[SafeIndex(defenderSlot)]);
                    SetAttackVisible(attackerSlot, true, 1f);
                    break;

                case MatchPhase.FollowUpThrowIn:
                    for (int slot = 0; slot < slotCount; slot++)
                    {
                        SeatSnapshot seat = ResolveSeatForUiSlot(context, seatOrder, slot);
                        if (seat == null)
                        {
                            continue;
                        }

                        bool isTimerOwner = seat.ClientId == context.Timer.OwnerClientId;
                        bool showRed = seat.IsOccupied &&
                                       seat.ClientId != context.Turn.DefenderId &&
                                       (seat.CardCount > 0 || isTimerOwner);
                        SetAttackVisible(slot, showRed, attackTargetFill[slot] <= 0f ? 1f : attackTargetFill[slot]);
                    }

                    break;

                case MatchPhase.RoundResolution:
                    SetAttackVisible(
                        attackerSlot,
                        true,
                        attackTargetFill[SafeIndex(attackerSlot)] <= 0f
                            ? 1f
                            : attackTargetFill[SafeIndex(attackerSlot)]);
                    break;
            }

            ApplyVisibleStateImmediate();
        }

        public void ApplyTimer(TimerChangedEvent timerEvent, MatchContext context, IReadOnlyList<int> seatOrder = null)
        {
            float normalizedFill = timerEvent.DurationSeconds > 0.001f
                ? Mathf.Clamp01(timerEvent.RemainingSeconds / timerEvent.DurationSeconds)
                : 0f;

            if (!timerEvent.IsRunning || context == null)
            {
                return;
            }

            int ownerSlot = ResolveSlot(context, timerEvent.OwnerClientId, seatOrder);
            if (ownerSlot < 0 || ownerSlot >= slotCount)
            {
                return;
            }

            if (timerEvent.Role == TurnTimerRole.Defend)
            {
                defendTargetFill[ownerSlot] = normalizedFill;
                return;
            }

            if (context.Phase == MatchPhase.FollowUpThrowIn)
            {
                for (int slot = 0; slot < slotCount; slot++)
                {
                    if (attackVisible[slot])
                    {
                        attackTargetFill[slot] = normalizedFill;
                    }
                }

                return;
            }

            attackTargetFill[ownerSlot] = normalizedFill;
        }

        public void Tick(float deltaTime)
        {
            float step = fillLerpSpeed * Mathf.Max(0f, deltaTime);
            for (int slot = 0; slot < slotCount; slot++)
            {
                attackCurrentFill[slot] = Mathf.MoveTowards(attackCurrentFill[slot], attackTargetFill[slot], step);
                defendCurrentFill[slot] = Mathf.MoveTowards(defendCurrentFill[slot], defendTargetFill[slot], step);

                SetRing(GetAttackRing(slot), attackVisible[slot], attackCurrentFill[slot]);
                SetRing(GetDefendRing(slot), defendVisible[slot], defendCurrentFill[slot]);
            }
        }

        public void HideAll()
        {
            ClearVisibility();
            for (int slot = 0; slot < slotCount; slot++)
            {
                attackCurrentFill[slot] = 0f;
                attackTargetFill[slot] = 0f;
                defendCurrentFill[slot] = 0f;
                defendTargetFill[slot] = 0f;
            }

            ApplyVisibleStateImmediate();
        }

        private void ClearVisibility()
        {
            for (int slot = 0; slot < slotCount; slot++)
            {
                attackVisible[slot] = false;
                defendVisible[slot] = false;
            }
        }

        private void SetAttackVisible(int slot, bool visible, float targetFill)
        {
            if (slot < 0 || slot >= slotCount)
            {
                return;
            }

            attackVisible[slot] = visible;
            attackTargetFill[slot] = visible ? Mathf.Clamp01(targetFill) : 0f;
            if (!visible)
            {
                attackCurrentFill[slot] = 0f;
            }
        }

        private void SetDefendVisible(int slot, bool visible, float targetFill)
        {
            if (slot < 0 || slot >= slotCount)
            {
                return;
            }

            defendVisible[slot] = visible;
            defendTargetFill[slot] = visible ? Mathf.Clamp01(targetFill) : 0f;
            if (!visible)
            {
                defendCurrentFill[slot] = 0f;
            }
        }

        private void ApplyVisibleStateImmediate()
        {
            for (int slot = 0; slot < slotCount; slot++)
            {
                if (!attackVisible[slot])
                {
                    SetRing(GetAttackRing(slot), false, 0f);
                }

                if (!defendVisible[slot])
                {
                    SetRing(GetDefendRing(slot), false, 0f);
                }
            }
        }

        private int ResolveSlot(MatchContext context, ulong clientId, IReadOnlyList<int> seatOrder)
        {
            if (context == null || context.Seats == null)
            {
                return -1;
            }

            if (seatOrder != null && seatOrder.Count > 0)
            {
                int maxSlots = Mathf.Min(slotCount, seatOrder.Count);
                for (int slot = 0; slot < maxSlots; slot++)
                {
                    int seatIndex = seatOrder[slot];
                    if (seatIndex < 0 || seatIndex >= context.Seats.Count)
                    {
                        continue;
                    }

                    if (context.Seats[seatIndex].ClientId == clientId)
                    {
                        return slot;
                    }
                }

                return -1;
            }

            for (int i = 0; i < context.Seats.Count && i < slotCount; i++)
            {
                if (context.Seats[i].ClientId == clientId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static SeatSnapshot ResolveSeatForUiSlot(MatchContext context, IReadOnlyList<int> seatOrder, int slot)
        {
            if (context == null || context.Seats == null || slot < 0)
            {
                return null;
            }

            int seatIndex = slot;
            if (seatOrder != null && slot < seatOrder.Count)
            {
                seatIndex = seatOrder[slot];
            }

            if (seatIndex < 0 || seatIndex >= context.Seats.Count)
            {
                return null;
            }

            return context.Seats[seatIndex];
        }
        private int SafeIndex(int slot)
        {
            return slot >= 0 && slot < slotCount ? slot : 0;
        }

        private Image GetAttackRing(int slot)
        {
            if (attackRings == null || slot < 0 || slot >= attackRings.Length)
            {
                return null;
            }

            return attackRings[slot];
        }

        private Image GetDefendRing(int slot)
        {
            if (defendRings == null || slot < 0 || slot >= defendRings.Length)
            {
                return null;
            }

            return defendRings[slot];
        }

        private static void SetRing(Image ring, bool visible, float fill)
        {
            if (ring == null)
            {
                return;
            }

            ring.gameObject.SetActive(visible);
            ring.fillAmount = visible ? Mathf.Clamp01(fill) : 0f;
        }
    }
}
