using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Progression
{
    /// <summary>One player's best time on one level.</summary>
    public readonly struct LeaderboardEntry
    {
        public readonly string PlayerName;
        public readonly float Time;

        public LeaderboardEntry(string playerName, float time)
        {
            PlayerName = playerName;
            Time = time;
        }
    }

    /// <summary>Everything the end-of-level screen needs about the run that just finished.</summary>
    public readonly struct LevelResult
    {
        /// <summary>Name this session's player competes under, e.g. "player3".</summary>
        public readonly string PlayerName;
        /// <summary>Time of the run that just finished.</summary>
        public readonly float RunTime;
        /// <summary>The player's best time on this level, which may be from an earlier run.</summary>
        public readonly float BestTime;
        /// <summary>True when this run beat the player's previous best (or was their first).</summary>
        public readonly bool IsNewBest;
        /// <summary>1-based position of the player's best time on the board.</summary>
        public readonly int Rank;
        /// <summary>The whole board, fastest first.</summary>
        public readonly IReadOnlyList<LeaderboardEntry> Entries;

        public LevelResult(string playerName, float runTime, float bestTime, bool isNewBest, int rank,
                           IReadOnlyList<LeaderboardEntry> entries)
        {
            PlayerName = playerName;
            RunTime = runTime;
            BestTime = bestTime;
            IsNewBest = isNewBest;
            Rank = rank;
            Entries = entries;
        }
    }

    /// <summary>
    /// Local best-time leaderboard, one board per level, persisted in PlayerPrefs.
    ///
    /// Players are not asked for a name: the first time a session records a time it claims the next
    /// free number ("player1", "player2", ...) and keeps it for the rest of that play session, across
    /// every level. So one launch of the game is one competitor — quit and relaunch to hand the next
    /// person their own name. Re-running a level updates that player's row instead of adding another.
    /// </summary>
    public static class Leaderboard
    {
        // Counter for auto-naming. Shared by every level, so a player's name is stable across the game.
        private const string NextPlayerNumberKey = "Leaderboard.NextPlayerNumber";
        private const string LevelKeyPrefix = "Leaderboard.Level.";

        // Board is capped so PlayerPrefs never grows without bound; slow runs fall off the end.
        private const int MaxEntriesPerLevel = 100;

        // Records are "name=seconds", joined by '|'. Auto-generated names can't contain either.
        private const char RecordSeparator = '|';
        private const char FieldSeparator = '=';

        private static string sessionPlayerName;

        /// <summary>
        /// The name this play session competes under. Claimed lazily on first use — a session that
        /// never finishes a level doesn't burn a number.
        /// </summary>
        public static string CurrentPlayerName
        {
            get
            {
                if (!string.IsNullOrEmpty(sessionPlayerName)) return sessionPlayerName;

                int number = PlayerPrefs.GetInt(NextPlayerNumberKey, 1);
                sessionPlayerName = "player" + number;
                PlayerPrefs.SetInt(NextPlayerNumberKey, number + 1);
                PlayerPrefs.Save();

                return sessionPlayerName;
            }
        }

        /// <summary>Every recorded time for a level, fastest first.</summary>
        public static List<LeaderboardEntry> GetEntries(string levelKey)
        {
            var entries = new List<LeaderboardEntry>();

            string raw = PlayerPrefs.GetString(LevelKeyPrefix + levelKey, string.Empty);
            if (string.IsNullOrEmpty(raw)) return entries;

            foreach (string record in raw.Split(RecordSeparator))
            {
                int split = record.IndexOf(FieldSeparator);
                if (split <= 0) continue;

                string name = record.Substring(0, split);
                string time = record.Substring(split + 1);
                if (!float.TryParse(time, NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds))
                    continue;

                entries.Add(new LeaderboardEntry(name, seconds));
            }

            entries.Sort((a, b) => a.Time.CompareTo(b.Time));
            return entries;
        }

        /// <summary>
        /// Records a finished run under this session's player name, keeping only their best time,
        /// and returns the updated board plus where the player landed on it.
        /// </summary>
        public static LevelResult Submit(string levelKey, float runTime)
        {
            string playerName = CurrentPlayerName;
            List<LeaderboardEntry> entries = GetEntries(levelKey);

            int existing = entries.FindIndex(entry => entry.PlayerName == playerName);
            bool isNewBest = existing < 0 || runTime < entries[existing].Time;

            if (existing < 0)
            {
                entries.Add(new LeaderboardEntry(playerName, runTime));
            }
            else if (isNewBest)
            {
                entries[existing] = new LeaderboardEntry(playerName, runTime);
            }

            entries.Sort((a, b) => a.Time.CompareTo(b.Time));
            if (entries.Count > MaxEntriesPerLevel)
                entries.RemoveRange(MaxEntriesPerLevel, entries.Count - MaxEntriesPerLevel);

            Save(levelKey, entries);

            int rank = entries.FindIndex(entry => entry.PlayerName == playerName) + 1;
            float bestTime = rank > 0 ? entries[rank - 1].Time : runTime;

            return new LevelResult(playerName, runTime, bestTime, isNewBest, rank, entries);
        }

        private static void Save(string levelKey, List<LeaderboardEntry> entries)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0) builder.Append(RecordSeparator);
                builder.Append(entries[i].PlayerName)
                       .Append(FieldSeparator)
                       .Append(entries[i].Time.ToString("F3", CultureInfo.InvariantCulture));
            }

            PlayerPrefs.SetString(LevelKeyPrefix + levelKey, builder.ToString());
            PlayerPrefs.Save();
        }

        /// <summary>Wipes one level's board. Used by the editor tools below.</summary>
        public static void Clear(string levelKey)
        {
            PlayerPrefs.DeleteKey(LevelKeyPrefix + levelKey);
            PlayerPrefs.Save();
        }

        /// <summary>"1:23.45" — the display format for every time, on the HUD and on the board.</summary>
        public static string FormatTime(float seconds)
        {
            if (seconds < 0f) seconds = 0f;

            int minutes = (int)(seconds / 60f);
            float remainder = seconds - minutes * 60f;

            return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00.00}", minutes, remainder);
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Leaderboard/Clear Board For Open Scene")]
        private static void ClearOpenSceneBoard()
        {
            string levelKey = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Clear(levelKey);
            Debug.Log($"[Leaderboard] Cleared the board for '{levelKey}'.");
        }

        [UnityEditor.MenuItem("Tools/Leaderboard/Reset Player Names To player1")]
        private static void ResetPlayerNames()
        {
            PlayerPrefs.DeleteKey(NextPlayerNumberKey);
            PlayerPrefs.Save();
            sessionPlayerName = null;
            Debug.Log("[Leaderboard] Next session will be player1 again. Existing boards are untouched.");
        }
#endif
    }
}
