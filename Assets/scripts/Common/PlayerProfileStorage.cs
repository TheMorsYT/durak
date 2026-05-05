using System;
using UnityEngine;

public enum MatchResultType
{
    Win = 0,
    Loss = 1,
    Draw = 2
}

public readonly struct PlayerStatsSnapshot
{
    public readonly int SingleWins;
    public readonly int SingleLosses;
    public readonly int SingleDraws;
    public readonly int MultiWins;
    public readonly int MultiLosses;
    public readonly int MultiDraws;

    public PlayerStatsSnapshot(
        int singleWins,
        int singleLosses,
        int singleDraws,
        int multiWins,
        int multiLosses,
        int multiDraws)
    {
        SingleWins = singleWins;
        SingleLosses = singleLosses;
        SingleDraws = singleDraws;
        MultiWins = multiWins;
        MultiLosses = multiLosses;
        MultiDraws = multiDraws;
    }
}

public static class PlayerProfileStorage
{
    private const string KeyNickname = "Profile.Nickname";
    private const string KeyAvatarIndex = "Profile.AvatarIndex";

    private const string KeySingleWins = "Stats.Single.Wins";
    private const string KeySingleLosses = "Stats.Single.Losses";
    private const string KeySingleDraws = "Stats.Single.Draws";

    private const string KeyMultiWins = "Stats.Multi.Wins";
    private const string KeyMultiLosses = "Stats.Multi.Losses";
    private const string KeyMultiDraws = "Stats.Multi.Draws";

    public const int MaxNicknameLength = 18;
    private const string DefaultNickname = "Player";

    public static string GetNickname()
    {
        string raw = PlayerPrefs.GetString(KeyNickname, DefaultNickname);
        return SanitizeNickname(raw);
    }

    public static void SetNickname(string nickname)
    {
        string cleaned = SanitizeNickname(nickname);
        PlayerPrefs.SetString(KeyNickname, cleaned);
        PlayerPrefs.Save();
    }

    public static string SanitizeNickname(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            return DefaultNickname;
        }

        string trimmed = nickname.Trim();
        if (trimmed.Length > MaxNicknameLength)
        {
            trimmed = trimmed.Substring(0, MaxNicknameLength);
        }

        return trimmed;
    }

    public static int GetAvatarIndex()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(KeyAvatarIndex, 0));
    }

    public static void SetAvatarIndex(int index)
    {
        PlayerPrefs.SetInt(KeyAvatarIndex, Mathf.Max(0, index));
        PlayerPrefs.Save();
    }

    public static PlayerStatsSnapshot GetStatsSnapshot()
    {
        return new PlayerStatsSnapshot(
            PlayerPrefs.GetInt(KeySingleWins, 0),
            PlayerPrefs.GetInt(KeySingleLosses, 0),
            PlayerPrefs.GetInt(KeySingleDraws, 0),
            PlayerPrefs.GetInt(KeyMultiWins, 0),
            PlayerPrefs.GetInt(KeyMultiLosses, 0),
            PlayerPrefs.GetInt(KeyMultiDraws, 0));
    }

    public static void RecordSingleResult(MatchResultType result)
    {
        IncrementResultCounters(result, KeySingleWins, KeySingleLosses, KeySingleDraws);
    }

    public static void RecordMultiResult(MatchResultType result)
    {
        IncrementResultCounters(result, KeyMultiWins, KeyMultiLosses, KeyMultiDraws);
    }

    public static string BuildSingleStatsBilingual()
    {
        PlayerStatsSnapshot stats = GetStatsSnapshot();
        return $"Singleplayer / Одиночна гра\n" +
               $"Wins / Перемог: {stats.SingleWins}\n" +
               $"Losses / Поразок: {stats.SingleLosses}\n" +
               $"Draws / Нічиїх: {stats.SingleDraws}";
    }

    public static string BuildMultiStatsBilingual()
    {
        PlayerStatsSnapshot stats = GetStatsSnapshot();
        return $"Multiplayer / Мультиплеєр\n" +
               $"Wins / Перемог: {stats.MultiWins}\n" +
               $"Losses / Поразок: {stats.MultiLosses}\n" +
               $"Draws / Нічиїх: {stats.MultiDraws}";
    }

    private static void IncrementResultCounters(
        MatchResultType result,
        string winsKey,
        string lossesKey,
        string drawsKey)
    {
        string key = result switch
        {
            MatchResultType.Win => winsKey,
            MatchResultType.Loss => lossesKey,
            _ => drawsKey
        };

        int next = PlayerPrefs.GetInt(key, 0) + 1;
        PlayerPrefs.SetInt(key, next);
        PlayerPrefs.Save();
    }
}
