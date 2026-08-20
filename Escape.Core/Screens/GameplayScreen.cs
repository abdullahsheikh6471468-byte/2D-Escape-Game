using System;
using Escape.Core;
using Escape.Core.Inputs;
using Escape.Core.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

namespace Escape.Screens
{
    /// <summary>
    /// Hosts one playthrough of the top-down escape game. Owns the
    /// <see cref="TopDownLevelRuntime"/> for the current level and reacts to
    /// its state (level complete / game over / all levels complete) by
    /// popping up a <see cref="MessageBoxScreen"/> and then loading the
    /// next level, retrying, or returning to level select.
    /// </summary>
    internal class GameplayScreen : GameScreen
    {
        private ContentManager content;
        private SpriteBatch spriteBatch;
        private float pauseAlpha;

        private int currentLevelNumber;
        private TopDownLevelRuntime runtime;
        private bool endMessageShown;

        private SettingsManager<GameProgress> progressManager;

        public GameplayScreen(int startingLevel = 1)
        {
            currentLevelNumber = Math.Clamp(startingLevel, 1, 10);
            TransitionOnTime = TimeSpan.FromSeconds(1.0);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

        public override void LoadContent()
        {
            base.LoadContent();

            content ??= new ContentManager(ScreenManager.Game.Services, "Content");
            spriteBatch = ScreenManager.SpriteBatch;

            progressManager ??= ScreenManager.Game.Services.GetService<SettingsManager<GameProgress>>();

            try
            {
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Play(content.Load<Song>("Sounds/Music"));
            }
            catch
            {
                // Music is optional; some platforms/back-ends may not support it.
            }

            StartLevel(currentLevelNumber);

            ScreenManager.Game.ResetElapsedTime();
        }

        private void StartLevel(int levelNumber)
        {
            currentLevelNumber = levelNumber;
            runtime = new TopDownLevelRuntime();
            runtime.LoadContent(content, levelNumber);
            endMessageShown = false;
        }

        public override void UnloadContent()
        {
            content.Unload();
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, false);

            if (coveredByOtherScreen)
                pauseAlpha = Math.Min(pauseAlpha + 1f / 32, 1);
            else
                pauseAlpha = Math.Max(pauseAlpha - 1f / 32, 0);

            if (IsActive && runtime.State != RuntimeState.Playing && !endMessageShown)
            {
                endMessageShown = true;
                ShowEndOfLevelMessage();
            }
        }

        public override void HandleInput(GameTime gameTime, InputState inputState)
        {
            ArgumentNullException.ThrowIfNull(inputState);

            base.HandleInput(gameTime, inputState);

            int playerIndex = ControllingPlayer != null ? (int)ControllingPlayer.Value : (int)PlayerIndex.One;

            if (inputState.IsPauseGame(ControllingPlayer))
            {
                ScreenManager.AddScreen(new PauseScreen(), ControllingPlayer);
                return;
            }

            if (!IsActive) return;

            if (runtime.State == RuntimeState.Playing)
            {
                var keyboard = inputState.CurrentKeyboardStates[playerIndex];
                runtime.Update(gameTime, keyboard);
            }
        }

        private void ShowEndOfLevelMessage()
        {
            string message;

            switch (runtime.State)
            {
                case RuntimeState.LevelComplete:
                    SaveLevelResult(currentLevelNumber, runtime.ScoreTotal, runtime.TimeUsedSeconds, unlockNext: true);
                    message =
                        $"LEVEL COMPLETE!{Environment.NewLine}{Environment.NewLine}" +
                        $"Time Bonus: +{runtime.ScoreTimeBonus}   Health Bonus: +{runtime.ScoreHealthBonus}   Objective Bonus: +{runtime.ScoreObjectiveBonus}{Environment.NewLine}" +
                        $"Level Score: {runtime.ScoreTotal}{Environment.NewLine}{Environment.NewLine}" +
                        "Accept: Next Level     Cancel: Level Select";
                    break;

                case RuntimeState.GameCompleteAll:
                    SaveLevelResult(currentLevelNumber, runtime.ScoreTotal, runtime.TimeUsedSeconds, unlockNext: false);
                    message =
                        $"CONGRATULATIONS! YOU ESCAPED!{Environment.NewLine}" +
                        $"ALL 10 LEVELS COMPLETED{Environment.NewLine}{Environment.NewLine}" +
                        $"Final Level Score: {runtime.ScoreTotal}{Environment.NewLine}{Environment.NewLine}" +
                        "Accept: Play Again     Cancel: Main Menu";
                    break;

                default: // GameOver
                    message =
                        $"GAME OVER{Environment.NewLine}{runtime.GameOverReason}{Environment.NewLine}{Environment.NewLine}" +
                        "Accept: Retry Level     Cancel: Level Select";
                    break;
            }

            var box = new MessageBoxScreen(message, false, TimeSpan.Zero);
            var finishedState = runtime.State;

            box.Accepted += (sender, e) =>
            {
                if (finishedState == RuntimeState.LevelComplete)
                    StartLevel(Math.Min(10, currentLevelNumber + 1));
                else if (finishedState == RuntimeState.GameCompleteAll)
                    StartLevel(1);
                else
                    StartLevel(currentLevelNumber);
            };

            box.Cancelled += (sender, e) =>
            {
                ExitScreen();
                ScreenManager.AddScreen(new LevelSelectScreen(), ControllingPlayer);
            };

            ScreenManager.AddScreen(box, ControllingPlayer);
        }

        private void SaveLevelResult(int levelNumber, int score, int timeUsedSeconds, bool unlockNext)
        {
            if (progressManager == null) return;

            var progress = progressManager.Settings;

            if (!progress.CompletedLevels.Contains(levelNumber))
                progress.CompletedLevels.Add(levelNumber);

            if (!progress.BestScores.TryGetValue(levelNumber, out int bestScore) || score > bestScore)
                progress.BestScores[levelNumber] = score;

            if (!progress.BestTimesSeconds.TryGetValue(levelNumber, out int bestTime) || timeUsedSeconds < bestTime)
                progress.BestTimesSeconds[levelNumber] = timeUsedSeconds;

            progress.TotalScore += score;

            if (unlockNext && levelNumber + 1 > progress.HighestUnlockedLevel && levelNumber < 10)
                progress.HighestUnlockedLevel = levelNumber + 1;

            progressManager.Save();
        }

        public override void Draw(GameTime gameTime)
        {
            ScreenManager.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 0, 0);

            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, null, ScreenManager.GlobalTransformation);
            runtime.Draw(gameTime, spriteBatch, ScreenManager.Font);
            spriteBatch.End();

            base.Draw(gameTime);

            if (TransitionPosition > 0 || pauseAlpha > 0)
            {
                float alpha = MathHelper.Lerp(1f - TransitionAlpha, 1f, pauseAlpha / 2);
                ScreenManager.FadeBackBufferToBlack(alpha);
            }
        }
    }
}
