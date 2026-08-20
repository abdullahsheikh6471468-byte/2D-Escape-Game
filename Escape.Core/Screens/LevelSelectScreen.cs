using Escape.Core;
using Escape.Core.Settings;
using Microsoft.Xna.Framework;

namespace Escape.Screens
{
    /// <summary>
    /// Lets the player pick any unlocked level. Locked levels are shown
    /// disabled (grayed out, not selectable) until the previous level has
    /// been completed.
    /// </summary>
    internal class LevelSelectScreen : MenuScreen
    {
        private bool entriesBuilt;

        public LevelSelectScreen() : base("Select Level")
        {
        }

        public override void LoadContent()
        {
            base.LoadContent();

            if (entriesBuilt) return;
            entriesBuilt = true;

            var progressManager = ScreenManager.Game.Services.GetService<SettingsManager<GameProgress>>();
            var progress = progressManager?.Settings ?? new GameProgress();

            for (int i = 1; i <= 10; i++)
            {
                int levelNumber = i;
                bool unlocked = levelNumber <= progress.HighestUnlockedLevel;
                bool completed = progress.CompletedLevels.Contains(levelNumber);

                string label = $"Level {levelNumber}" + (completed ? "  [Completed]" : (unlocked ? "" : "  [Locked]"));
                var entry = new MenuEntry(label, unlocked);
                entry.Selected += (sender, e) =>
                {
                    if (!unlocked) return;
                    ExitScreen();
                    ScreenManager.AddScreen(new GameplayScreen(levelNumber), ControllingPlayer);
                };
                MenuEntries.Add(entry);
            }

            var back = new MenuEntry("Back");
            back.Selected += OnCancel;
            MenuEntries.Add(back);
        }

        protected override void OnCancel(PlayerIndex playerIndex)
        {
            ExitScreen();
        }
    }
}
