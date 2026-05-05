using System;
using System.Collections.Generic;
using Durak.Architecture.Shared.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Durak.Architecture.Shared.UI.Presenters
{
    public sealed class MatchSeatLayoutPresenter
    {
        [Serializable]
        private sealed class SeatLayoutConfig
        {
            public Vector2 sizeDelta;
            public Vector3 anchoredPosition3D;
            public Quaternion localRotation;
            public RectOffset padding;
            public TextAnchor childAlignment;
            public float spacing;
            public bool childControlWidth;
            public bool childControlHeight;
            public bool childScaleWidth;
            public bool childScaleHeight;
            public bool childForceExpandWidth;
            public bool childForceExpandHeight;
            public bool hasHandLayoutAdjuster;
            public float handDefaultSpacing;
            public float handCardWidth;
        }

        private readonly Transform[] seats;
        private readonly SeatLayoutConfig[] seatConfigs;
        private readonly SeatLayoutConfig opponentSeatConfig;
        private int lastLocalSeatIndex = -1;
        private int lastActivePlayers = -1;

        public MatchSeatLayoutPresenter(Transform[] seats)
        {
            this.seats = seats;
            if (seats == null)
            {
                seatConfigs = Array.Empty<SeatLayoutConfig>();
                return;
            }

            seatConfigs = new SeatLayoutConfig[seats.Length];
            for (int i = 0; i < seats.Length; i++)
            {
                seatConfigs[i] = CaptureSeatLayoutConfig(seats[i]);
            }

            opponentSeatConfig = ResolveOpponentSeatConfig(seatConfigs);
        }

        public void ApplyPerspective(IReadOnlyList<SeatSnapshot> seatsSnapshot, ulong localClientId)
        {
            if (seats == null || seats.Length == 0 || seatsSnapshot == null || seatsSnapshot.Count == 0)
            {
                return;
            }

            int localSeatIndex = ResolveAssignmentIndex(seatsSnapshot, localClientId);
            if (localSeatIndex < 0 || localSeatIndex >= seats.Length)
            {
                return;
            }

            int activePlayers = Mathf.Clamp(CountOccupied(seatsSnapshot), 1, seats.Length);
            if (localSeatIndex == lastLocalSeatIndex && activePlayers == lastActivePlayers)
            {
                return;
            }

            lastLocalSeatIndex = localSeatIndex;
            lastActivePlayers = activePlayers;

            for (int seatIndex = 0; seatIndex < seats.Length; seatIndex++)
            {
                Transform seat = seats[seatIndex];
                if (seat == null)
                {
                    continue;
                }

                int configIndex = (seatIndex - localSeatIndex + seats.Length) % seats.Length;
                SeatLayoutConfig config = seatConfigs[configIndex];
                ApplySeatLayoutConfig(seat, config, opponentSeatConfig, seatIndex == localSeatIndex);

                RectTransform rectTransform = seat.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                }
            }
        }

        private static int ResolveAssignmentIndex(IReadOnlyList<SeatSnapshot> seatsSnapshot, ulong localClientId)
        {
            for (int i = 0; i < seatsSnapshot.Count; i++)
            {
                if (seatsSnapshot[i].ClientId == localClientId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int CountOccupied(IReadOnlyList<SeatSnapshot> seatsSnapshot)
        {
            if (seatsSnapshot == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < seatsSnapshot.Count; i++)
            {
                if (seatsSnapshot[i].IsOccupied)
                {
                    count++;
                }
            }

            return count;
        }

        private static SeatLayoutConfig CaptureSeatLayoutConfig(Transform seat)
        {
            if (seat == null)
            {
                return null;
            }

            SeatLayoutConfig config = new SeatLayoutConfig();
            RectTransform rect = seat.GetComponent<RectTransform>();
            if (rect != null)
            {
                config.sizeDelta = rect.sizeDelta;
                config.anchoredPosition3D = rect.anchoredPosition3D;
                config.localRotation = rect.localRotation;
            }

            HorizontalLayoutGroup layout = seat.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                config.padding = new RectOffset(layout.padding.left, layout.padding.right, layout.padding.top, layout.padding.bottom);
                config.childAlignment = layout.childAlignment;
                config.spacing = layout.spacing;
                config.childControlWidth = layout.childControlWidth;
                config.childControlHeight = layout.childControlHeight;
                config.childScaleWidth = layout.childScaleWidth;
                config.childScaleHeight = layout.childScaleHeight;
                config.childForceExpandWidth = layout.childForceExpandWidth;
                config.childForceExpandHeight = layout.childForceExpandHeight;
            }

            HandLayoutAdjuster adjuster = seat.GetComponent<HandLayoutAdjuster>();
            if (adjuster != null)
            {
                config.hasHandLayoutAdjuster = true;
                config.handDefaultSpacing = adjuster.defaultSpacing;
                config.handCardWidth = adjuster.cardWidth;
            }

            return config;
        }

        private static SeatLayoutConfig ResolveOpponentSeatConfig(SeatLayoutConfig[] configs)
        {
            if (configs == null || configs.Length == 0)
            {
                return null;
            }

            for (int i = 1; i < configs.Length; i++)
            {
                if (configs[i] != null)
                {
                    return configs[i];
                }
            }

            return configs[0];
        }

        private static void ApplySeatLayoutConfig(Transform seat, SeatLayoutConfig config, SeatLayoutConfig opponentConfig, bool isLocalSeat)
        {
            if (seat == null || config == null)
            {
                return;
            }

            RectTransform rect = seat.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition3D = config.anchoredPosition3D;
                rect.localRotation = config.localRotation;
                rect.sizeDelta = config.sizeDelta;
            }

            HorizontalLayoutGroup layout = seat.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                if (config.padding != null)
                {
                    layout.padding = new RectOffset(config.padding.left, config.padding.right, config.padding.top, config.padding.bottom);
                }

                layout.childAlignment = config.childAlignment;
                layout.spacing = config.spacing;
                layout.childControlWidth = config.childControlWidth;
                layout.childControlHeight = config.childControlHeight;
                layout.childScaleWidth = config.childScaleWidth;
                layout.childScaleHeight = config.childScaleHeight;
                layout.childForceExpandWidth = config.childForceExpandWidth;
                layout.childForceExpandHeight = config.childForceExpandHeight;

                if (!isLocalSeat)
                {
                    SeatLayoutConfig source = opponentConfig ?? config;
                    if (source.padding != null)
                    {
                        layout.padding = new RectOffset(source.padding.left, source.padding.right, source.padding.top, source.padding.bottom);
                    }

                    layout.spacing = Mathf.Min(source.spacing, -75f);
                }
            }

            HandLayoutAdjuster adjuster = seat.GetComponent<HandLayoutAdjuster>();
            if (adjuster != null)
            {
                adjuster.enabled = isLocalSeat;
                if (isLocalSeat)
                {
                    adjuster.defaultSpacing = config.hasHandLayoutAdjuster ? config.handDefaultSpacing : config.spacing;
                    if (config.hasHandLayoutAdjuster && config.handCardWidth > 0f)
                    {
                        adjuster.cardWidth = config.handCardWidth;
                    }
                }
            }
        }
    }
}
