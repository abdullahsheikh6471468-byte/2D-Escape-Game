using Escape.Core.Localization;
using Microsoft.Xna.Framework;

namespace Escape.Screens
{
    /// <summary>
    /// The main menu screen. The dungeon-gate artwork behind it is drawn by
    /// <see cref="BackgroundScreen"/>; this screen just lists the entries.
    /// </summary>
    internal class MainMenuScreen : MenuScreen
    {
        public MainMenuScreen() : base("Escape: The Last Exit")
        {
            var playGameMenuEntry = new MenuEntry("Play");
            var levelSelectMenuEntry = new MenuEntry("Level Select");
            var settingsMenuEntry = new MenuEntry(Resources.Settings);
            var aboutMenuEntry = new MenuEntry(Resources.About);
            var exitMenuEntry = new MenuEntry(Resources.Exit);

            playGameMenuEntry.Selected += (sender, e) =>
            {
                ExitScreen();
                ScreenManager.AddScreen(new GameplayScreen(1), ControllingPlayer);
            };
            levelSelectMenuEntry.Selected += (sender, e) =>
            {
                ScreenManager.AddScreen(new LevelSelectScreen(), ControllingPlayer);
            };
            settingsMenuEntry.Selected += (sender, e) =>
            {
                ScreenManager.AddScreen(new SettingsScreen(), ControllingPlayer);
            };
            aboutMenuEntry.Selected += (sender, e) =>
            {
                ScreenManager.AddScreen(new AboutScreen(), ControllingPlayer);
            };
            exitMenuEntry.Selected += OnCancel;

            MenuEntries.Add(playGameMenuEntry);
            MenuEntries.Add(levelSelectMenuEntry);
            MenuEntries.Add(settingsMenuEntry);
            MenuEntries.Add(aboutMenuEntry);
            MenuEntries.Add(exitMenuEntry);
        }

        protected override void OnCancel(PlayerIndex playerIndex)
        {
            ScreenManager.Game.Exit();
        }
    }
}
