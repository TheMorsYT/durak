using System.Collections.Generic;
using Durak.Architecture.Shared.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Durak.Architecture.Shared.UI.Presenters
{
    public sealed class MatchProfilesPresenter
    {
        private static readonly Vector2 AvatarSize = new Vector2(100f, 100f);

        private readonly GameObject[] profileObjects;
        private readonly TMP_Text[] nicknameTexts;
        private readonly Image[] avatarImages;
        private readonly bool hideSeatZeroNickname;

        public MatchProfilesPresenter(
            GameObject[] profileObjects,
            TMP_Text[] nicknameTexts,
            Image[] avatarImages,
            bool hideSeatZeroNickname = false)
        {
            this.profileObjects = profileObjects;
            this.nicknameTexts = nicknameTexts;
            this.avatarImages = avatarImages;
            this.hideSeatZeroNickname = hideSeatZeroNickname;
        }

        public void Apply(IReadOnlyList<SeatSnapshot> seats, IReadOnlyList<int> seatOrder = null)
        {
            if (nicknameTexts != null)
            {
                for (int i = 0; i < nicknameTexts.Length; i++)
                {
                    if (nicknameTexts[i] != null)
                    {
                        nicknameTexts[i].text = string.Empty;
                    }
                }
            }

            bool useSevenNicknameSlots = hideSeatZeroNickname &&
                                         seats != null &&
                                         nicknameTexts != null &&
                                         seats.Count > 1 &&
                                         nicknameTexts.Length == seats.Count - 1;

            int maxSlots = Mathf.Max(
                seats != null ? seats.Count : 0,
                profileObjects != null ? profileObjects.Length : 0,
                nicknameTexts != null ? nicknameTexts.Length : 0,
                avatarImages != null ? avatarImages.Length : 0);

            for (int slot = 0; slot < maxSlots; slot++)
            {
                int sourceSeatIndex = ResolveSeatIndexForUiSlot(slot, seatOrder);
                SeatSnapshot seat = seats != null && sourceSeatIndex >= 0 && sourceSeatIndex < seats.Count
                    ? seats[sourceSeatIndex]
                    : SeatSnapshot.Empty(sourceSeatIndex >= 0 ? sourceSeatIndex : slot);

                if (profileObjects != null && slot < profileObjects.Length && profileObjects[slot] != null)
                {
                    profileObjects[slot].SetActive(seat.IsOccupied);
                }

                if (nicknameTexts != null)
                {
                    if (useSevenNicknameSlots)
                    {
                        if (slot > 0)
                        {
                            int nicknameSlot = slot - 1;
                            if (nicknameSlot < nicknameTexts.Length && nicknameTexts[nicknameSlot] != null)
                            {
                                nicknameTexts[nicknameSlot].text = seat.IsOccupied ? (seat.Nickname ?? string.Empty) : string.Empty;
                            }
                        }
                    }
                    else if (slot < nicknameTexts.Length && nicknameTexts[slot] != null)
                    {
                        nicknameTexts[slot].text = hideSeatZeroNickname && slot == 0
                            ? string.Empty
                            : (seat.Nickname ?? string.Empty);
                    }
                }

                if (avatarImages == null || slot >= avatarImages.Length || avatarImages[slot] == null)
                {
                    continue;
                }

                Image avatar = avatarImages[slot];
                avatar.sprite = seat.IsOccupied ? AvatarCatalog.GetAt(seat.AvatarIndex) : null;
                avatar.preserveAspect = true;

                RectTransform rect = avatar.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = AvatarSize;
                }
            }
        }

        private static int ResolveSeatIndexForUiSlot(int slot, IReadOnlyList<int> seatOrder)
        {
            if (seatOrder == null || slot < 0 || slot >= seatOrder.Count)
            {
                return slot;
            }

            return seatOrder[slot];
        }
    }
}
