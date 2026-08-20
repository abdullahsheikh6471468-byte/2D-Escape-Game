using System.Collections.Generic;

namespace Escape.Core.Settings
{
    /// <summary>
    /// Persisted progress for the escape game: which levels are unlocked/completed,
    /// best score and best time per level, and the running total score.
    /// Saved/loaded through the existing <see cref="SettingsManager{T}"/> +
    /// <see cref="ISettingsStorage"/> infrastructure (JSON file in app data).
    /// </summary>
    public class GameProgress
    {
        public int HighestUnlockedLevel { get; set; } = 1;
        public List<int> CompletedLevels { get; set; } = new List<int>();
        public Dictionary<int, int> BestScores { get; set; } = new Dictionary<int, int>();
        public Dictionary<int, int> BestTimesSeconds { get; set; } = new Dictionary<int, int>();
        public int TotalScore { get; set; } = 0;
    }
}
