using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

namespace Escape.Core
{
    internal enum RuntimeState { Playing, LevelComplete, GameOver, GameCompleteAll }

    /// <summary>
    /// Drives one playthrough of a level: movement, collisions, keys/doors,
    /// traps, enemies, HUD, mini-map, camera, and the countdown timer.
    /// </summary>
    internal class TopDownLevelRuntime
    {
        private const int ViewportWidth = 800;
        private const int ViewportHeight = 480;
        private const int TileSize = 64;
        private const int HudHeight = 60;

        public RoomLevel Level { get; private set; }
        public PlayerCharacter Player { get; private set; }
        public RuntimeState State { get; private set; } = RuntimeState.Playing;
        public string GameOverReason { get; private set; } = "";

        public int TimeUsedSeconds { get; private set; }
        public int ScoreTotal { get; private set; }
        public int ScoreTimeBonus { get; private set; }
        public int ScoreHealthBonus { get; private set; }
        public int ScoreObjectiveBonus { get; private set; }

        private float timeRemainingSeconds;
        private string currentMessage = "";
        private float messageTimer;
        private float lockedSoundCooldown;

        private Texture2D floorTexture;
        private Texture2D wallTexture;
        private Texture2D doorNormalTex, doorLockedTex, doorExitTex;
        private Texture2D trapSpikeTex, trapMovingBlockTex, trapLaserTex, trapFireTex;
        private Texture2D heartIconTex, hourglassIconTex;
        private Texture2D blankTexture;
        private readonly Dictionary<string, Texture2D> itemTextures = new Dictionary<string, Texture2D>();

        private SoundEffect pickupSound, damageSound, levelCompleteSound;

        public void LoadContent(ContentManager content, int levelNumber)
        {
            Level = LevelBuilder.CreateLevel(levelNumber);
            Player = new PlayerCharacter(Level.PlayerStart);
            Player.LoadContent(content);

            foreach (var enemy in Level.Enemies)
                enemy.LoadContent(content);

            floorTexture = content.Load<Texture2D>(Level.FloorTexturePath);
            wallTexture = content.Load<Texture2D>(Level.WallTexturePath);
            doorNormalTex = content.Load<Texture2D>("Doors/door_normal");
            doorLockedTex = content.Load<Texture2D>("Doors/door_locked");
            doorExitTex = content.Load<Texture2D>("Doors/door_exit");
            trapSpikeTex = content.Load<Texture2D>("Traps/trap_spike");
            trapMovingBlockTex = content.Load<Texture2D>("Traps/trap_movingblock");
            trapLaserTex = content.Load<Texture2D>("Traps/trap_laser");
            trapFireTex = content.Load<Texture2D>("Traps/trap_fire");
            heartIconTex = content.Load<Texture2D>("UI/icon_heart");
            hourglassIconTex = content.Load<Texture2D>("UI/icon_hourglass");
            blankTexture = content.Load<Texture2D>("Sprites/blank");

            try
            {
                pickupSound = content.Load<SoundEffect>("Sounds/PlayerGemCollected");
                damageSound = content.Load<SoundEffect>("Sounds/PlayerKilled");
                levelCompleteSound = content.Load<SoundEffect>("Sounds/PlayerExitReached");
            }
            catch
            {
                // Sound is a nice-to-have; keep playing silently if effects fail to load.
            }

            foreach (var item in Level.Items)
            {
                if (!itemTextures.ContainsKey(item.SpritePath))
                    itemTextures[item.SpritePath] = content.Load<Texture2D>(item.SpritePath);
            }

            timeRemainingSeconds = Level.TimeLimitSeconds;
            State = RuntimeState.Playing;
            currentMessage = "";
            messageTimer = 0;
        }

        public void Update(GameTime gameTime, KeyboardState keyboard)
        {
            if (State != RuntimeState.Playing) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            HandleMovement(keyboard, dt);
            Player.Update(gameTime);
            HandleItemPickups();
            HandleDoors();
            HandleTraps(gameTime);
            HandleEnemies(gameTime);

            timeRemainingSeconds -= dt;
            if (messageTimer > 0) messageTimer -= dt;
            if (lockedSoundCooldown > 0) lockedSoundCooldown -= dt;

            if (timeRemainingSeconds <= 0)
            {
                GameOverReason = "You ran out of time!";
                State = RuntimeState.GameOver;
                return;
            }

            if (!Player.IsAlive)
            {
                GameOverReason = "You did not survive the escape.";
                State = RuntimeState.GameOver;
            }
        }

        private void HandleMovement(KeyboardState keyboard, float dt)
        {
            float dx = 0, dy = 0;
            if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up)) dy -= 1;
            if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down)) dy += 1;
            if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left)) dx -= 1;
            if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right)) dx += 1;

            Player.IsMoving = dx != 0 || dy != 0;

            if (dx != 0 && dy != 0)
            {
                float len = (float)Math.Sqrt(dx * dx + dy * dy);
                dx /= len; dy /= len;
            }

            if (dy < 0) Player.Facing = Direction.Up;
            else if (dx < 0) Player.Facing = Direction.Left;
            else if (dy > 0) Player.Facing = Direction.Down;
            else if (dx > 0) Player.Facing = Direction.Right;

            float speed = PlayerCharacter.Speed;
            var tryX = new Vector2(Player.Position.X + dx * speed * dt, Player.Position.Y);
            if (!IsBlocked(BoundsAt(tryX, PlayerCharacter.Width, PlayerCharacter.Height))) Player.Position = tryX;

            var tryY = new Vector2(Player.Position.X, Player.Position.Y + dy * speed * dt);
            if (!IsBlocked(BoundsAt(tryY, PlayerCharacter.Width, PlayerCharacter.Height))) Player.Position = tryY;

            float clampedX = MathHelper.Clamp(Player.Position.X, 20, Level.WorldWidth - 20);
            float clampedY = MathHelper.Clamp(Player.Position.Y, 20, Level.WorldHeight - 20);
            Player.Position = new Vector2(clampedX, clampedY);
        }

        private static Rectangle BoundsAt(Vector2 center, float w, float h) => new Rectangle(
            (int)(center.X - w / 2f), (int)(center.Y - h / 2f), (int)w, (int)h);

        private bool IsBlocked(Rectangle bounds)
        {
            foreach (var wall in Level.Walls)
                if (wall.Intersects(bounds)) return true;

            foreach (var door in Level.Doors)
                if (!door.IsOpen && door.Bounds.Intersects(bounds)) return true;

            return false;
        }

        private void ShowMessage(string msg)
        {
            currentMessage = msg;
            messageTimer = 1.8f;
        }

        private void HandleItemPickups()
        {
            foreach (var item in Level.Items)
            {
                if (item.Collected) continue;
                if (!Player.Bounds.Intersects(item.Bounds)) continue;

                item.Collected = true;
                if (item.Kind == ItemKind.HealthPickup)
                {
                    Player.Heal(item.HealAmount);
                    ShowMessage("Health restored!");
                }
                else
                {
                    Player.Keys.Add(item.ColorName);
                    ShowMessage($"{item.ColorName} Key collected!");
                }
                pickupSound?.Play();
            }
        }

        private void HandleDoors()
        {
            DoorObj exitDoor = null;

            foreach (var door in Level.Doors)
            {
                if (door.Kind == DoorKind.Exit) { exitDoor = door; continue; }
                if (door.Kind != DoorKind.Locked || door.IsOpen) continue;

                var inflated = new Rectangle(door.Bounds.X - 6, door.Bounds.Y - 6, door.Bounds.Width + 12, door.Bounds.Height + 12);
                if (!Player.Bounds.Intersects(inflated)) continue;

                if (Player.HasKey(door.RequiredKeyColor))
                {
                    door.IsOpen = true;
                }
                else
                {
                    ShowMessage($"You need the {door.RequiredKeyColor} Key!");
                    lockedSoundCooldown = 0.75f;
                }
            }

            if (exitDoor == null) return;

            int have = Player.Keys.Count;
            if (!exitDoor.IsOpen && have >= Level.RequiredItemCount)
                exitDoor.IsOpen = true;

            if (exitDoor.IsOpen)
            {
                var inflated = new Rectangle(exitDoor.Bounds.X - 10, exitDoor.Bounds.Y - 10, exitDoor.Bounds.Width + 20, exitDoor.Bounds.Height + 20);
                if (Player.Bounds.Intersects(inflated))
                    CompleteLevel();
            }
        }

        private void HandleTraps(GameTime gameTime)
        {
            foreach (var trap in Level.Traps)
            {
                trap.Update(gameTime);
                if (!trap.IsDangerousNow || trap.HitCooldown > 0) continue;
                if (!trap.CurrentBounds.Intersects(Player.Bounds)) continue;

                Player.TakeDamage(trap.Damage);
                trap.RegisterHit();
                damageSound?.Play();
            }
        }

        private void HandleEnemies(GameTime gameTime)
        {
            foreach (var enemy in Level.Enemies)
            {
                enemy.Update(gameTime, Player.Position, Player.IsAlive, IsBlocked);

                if (enemy.Bounds.Intersects(Player.Bounds) && enemy.CanAttack())
                {
                    Player.TakeDamage(enemy.Damage);
                    enemy.RegisterAttack();
                }
            }
        }

        private void CompleteLevel()
        {
            int secondsRemaining = Math.Max(0, (int)timeRemainingSeconds);
            int damageTaken = Player.MaxHealth - Player.Health;

            ScoreTimeBonus = secondsRemaining * 4;
            ScoreHealthBonus = Player.Health * 3;
            ScoreObjectiveBonus = Level.RequiredItemCount > 0
                ? Player.Keys.Count * 300 / Math.Max(1, Level.RequiredItemCount)
                : 300;
            ScoreTotal = Math.Max(0, ScoreTimeBonus + ScoreHealthBonus + ScoreObjectiveBonus - damageTaken);

            TimeUsedSeconds = Level.TimeLimitSeconds - secondsRemaining;
            State = Level.Number >= 10 ? RuntimeState.GameCompleteAll : RuntimeState.LevelComplete;
            levelCompleteSound?.Play();
        }

        // ------------------------------------------------------------------
        // Rendering
        // ------------------------------------------------------------------

        private float CameraOffsetX() => MathHelper.Clamp(
            Player.Position.X - ViewportWidth / 2f, 0, Math.Max(0, Level.WorldWidth - ViewportWidth));

        private float CameraOffsetY() => MathHelper.Clamp(
            Player.Position.Y - ViewportHeight / 2f, 0, Math.Max(0, Level.WorldHeight - ViewportHeight));

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch, SpriteFont font)
        {
            float offsetX = CameraOffsetX();
            float offsetY = CameraOffsetY();
            var camera = new Vector2(offsetX, offsetY);

            DrawFloorAndWalls(spriteBatch, camera);
            DrawDoors(spriteBatch, camera);
            DrawItems(spriteBatch, camera);
            DrawTraps(spriteBatch, camera);

            foreach (var enemy in Level.Enemies)
                enemy.Draw(gameTime, spriteBatch, enemy.Position - camera);

            Player.Draw(gameTime, spriteBatch, Player.Position - camera);

            if (Level.DarkEnvironment) DrawFog(spriteBatch, camera);

            DrawHud(spriteBatch, font);
            DrawMiniMap(spriteBatch);

            if (!string.IsNullOrEmpty(currentMessage) && messageTimer > 0)
                DrawCenterMessage(spriteBatch, font, currentMessage);
        }

        private void DrawFloorAndWalls(SpriteBatch spriteBatch, Vector2 camera)
        {
            int startX = ((int)camera.X / TileSize) * TileSize;
            int startY = ((int)camera.Y / TileSize) * TileSize;

            for (int y = startY; y < camera.Y + ViewportHeight; y += TileSize)
            {
                for (int x = startX; x < camera.X + ViewportWidth; x += TileSize)
                {
                    var dest = new Rectangle((int)(x - camera.X), (int)(y - camera.Y), TileSize, TileSize);
                    spriteBatch.Draw(floorTexture, dest, Color.White);
                }
            }

            foreach (var wall in Level.Walls)
            {
                var dest = new Rectangle((int)(wall.X - camera.X), (int)(wall.Y - camera.Y), wall.Width, wall.Height);
                // Tile the wall texture along its length so long walls don't stretch a single tile.
                int tilesX = Math.Max(1, wall.Width / TileSize);
                int tilesY = Math.Max(1, wall.Height / TileSize);
                int tw = wall.Width / tilesX;
                int th = wall.Height / tilesY;
                for (int ty = 0; ty < tilesY; ty++)
                    for (int tx = 0; tx < tilesX; tx++)
                    {
                        var d = new Rectangle(dest.X + tx * tw, dest.Y + ty * th, tw, th);
                        spriteBatch.Draw(wallTexture, d, Color.White);
                    }
            }
        }

        private void DrawDoors(SpriteBatch spriteBatch, Vector2 camera)
        {
            foreach (var door in Level.Doors)
            {
                Texture2D tex = door.Kind switch
                {
                    DoorKind.Locked => doorLockedTex,
                    DoorKind.Exit => doorExitTex,
                    _ => doorNormalTex
                };
                var dest = new Rectangle((int)(door.Bounds.X - camera.X), (int)(door.Bounds.Y - camera.Y),
                    door.Bounds.Width, door.Bounds.Height);
                Color tint = door.IsOpen ? new Color(255, 255, 255, 150) : Color.White;
                spriteBatch.Draw(tex, dest, tint);
            }
        }

        private void DrawItems(SpriteBatch spriteBatch, Vector2 camera)
        {
            foreach (var item in Level.Items)
            {
                if (item.Collected) continue;
                var tex = itemTextures[item.SpritePath];
                var dest = new Rectangle((int)(item.Bounds.X - camera.X), (int)(item.Bounds.Y - camera.Y),
                    item.Bounds.Width, item.Bounds.Height);
                spriteBatch.Draw(tex, dest, Color.White);
            }
        }

        private void DrawTraps(SpriteBatch spriteBatch, Vector2 camera)
        {
            foreach (var trap in Level.Traps)
            {
                Texture2D tex = trap.Kind switch
                {
                    TrapKind.MovingBlock => trapMovingBlockTex,
                    TrapKind.Laser => trapLaserTex,
                    TrapKind.Fire => trapFireTex,
                    _ => trapSpikeTex
                };
                var dest = new Rectangle((int)(trap.CurrentBounds.X - camera.X), (int)(trap.CurrentBounds.Y - camera.Y),
                    trap.CurrentBounds.Width, trap.CurrentBounds.Height);

                Color tint = Color.White;
                if (trap.Kind == TrapKind.Laser && !trap.IsActive) tint = new Color(140, 140, 140, 160);

                spriteBatch.Draw(tex, dest, tint);
            }
        }

        private void DrawFog(SpriteBatch spriteBatch, Vector2 camera)
        {
            // A simple dim overlay everywhere except a lit box around the player,
            // approximated with four dark bars since the engine's SpriteBatch
            // pipeline here doesn't have a radial-gradient shader.
            Vector2 playerScreen = Player.Position - camera;

            int r = 150;
            int left = (int)playerScreen.X - r, right = (int)playerScreen.X + r;
            int top = (int)playerScreen.Y - r, bottom = (int)playerScreen.Y + r;

            Color dark = new Color(0, 0, 0, 235);

            spriteBatch.Draw(blankTexture, new Rectangle(0, 0, ViewportWidth, Math.Max(0, top)), dark);
            spriteBatch.Draw(blankTexture, new Rectangle(0, Math.Max(0, bottom), ViewportWidth, ViewportHeight - Math.Max(0, bottom)), dark);
            spriteBatch.Draw(blankTexture, new Rectangle(0, Math.Max(0, top), Math.Max(0, left), Math.Min(ViewportHeight, bottom) - Math.Max(0, top)), dark);
            spriteBatch.Draw(blankTexture, new Rectangle(Math.Min(ViewportWidth, right), Math.Max(0, top), Math.Max(0, ViewportWidth - right), Math.Min(ViewportHeight, bottom) - Math.Max(0, top)), dark);
        }

        private void DrawHud(SpriteBatch spriteBatch, SpriteFont font)
        {
            spriteBatch.Draw(blankTexture, new Rectangle(0, 0, ViewportWidth, HudHeight), new Color(10, 10, 15, 165));

            spriteBatch.DrawString(font, $"LEVEL {Level.Number}: {Level.Name}", new Vector2(12, 4), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            // Health bar
            int barX = 30, barY = 30, barW = 140, barH = 14;
            spriteBatch.Draw(blankTexture, new Rectangle(barX, barY, barW, barH), new Color(60, 0, 0, 200));
            float pct = Player.Health / (float)Player.MaxHealth;
            Color hc = pct > 0.5f ? Color.LimeGreen : (pct > 0.25f ? Color.Orange : Color.Red);
            spriteBatch.Draw(blankTexture, new Rectangle(barX + 1, barY + 1, (int)((barW - 2) * pct), barH - 2), hc);
            spriteBatch.Draw(heartIconTex, new Rectangle(barX - 22, barY - 4, 18, 18), Color.White);

            int have = Player.Keys.Count;
            spriteBatch.DrawString(font, $"KEYS: {have}/{Level.RequiredItemCount}   {Level.Objective}",
                new Vector2(230, 4), Color.White, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);

            int secondsLeft = Math.Max(0, (int)timeRemainingSeconds);
            string timeStr = $"{secondsLeft / 60:D2}:{secondsLeft % 60:D2}";
            spriteBatch.Draw(hourglassIconTex, new Rectangle(ViewportWidth - 100, 4, 18, 18), Color.White);
            spriteBatch.DrawString(font, timeStr, new Vector2(ViewportWidth - 76, 4), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }

        private void DrawMiniMap(SpriteBatch spriteBatch)
        {
            const int mmW = 140, mmH = 80;
            int mmX = ViewportWidth - mmW - 10, mmY = HudHeight + 8;

            spriteBatch.Draw(blankTexture, new Rectangle(mmX, mmY, mmW, mmH), new Color(10, 10, 15, 180));

            float scaleX = mmW / (float)Level.WorldWidth;
            float scaleY = mmH / (float)Level.WorldHeight;

            foreach (var item in Level.Items)
            {
                if (item.Collected) continue;
                var p = new Vector2(mmX + item.Position.X * scaleX, mmY + item.Position.Y * scaleY);
                spriteBatch.Draw(blankTexture, new Rectangle((int)p.X - 2, (int)p.Y - 2, 4, 4), Color.Gold);
            }

            foreach (var enemy in Level.Enemies)
            {
                var p = new Vector2(mmX + enemy.Position.X * scaleX, mmY + enemy.Position.Y * scaleY);
                spriteBatch.Draw(blankTexture, new Rectangle((int)p.X - 2, (int)p.Y - 2, 5, 5), Color.Red);
            }

            var exitDoor = Level.Doors.Find(d => d.Kind == DoorKind.Exit);
            if (exitDoor != null)
            {
                var p = new Vector2(mmX + exitDoor.Bounds.Center.X * scaleX, mmY + exitDoor.Bounds.Center.Y * scaleY);
                spriteBatch.Draw(blankTexture, new Rectangle((int)p.X - 3, (int)p.Y - 3, 6, 6), Color.LimeGreen);
            }

            var pp = new Vector2(mmX + Player.Position.X * scaleX, mmY + Player.Position.Y * scaleY);
            spriteBatch.Draw(blankTexture, new Rectangle((int)pp.X - 3, (int)pp.Y - 3, 7, 7), Color.DeepSkyBlue);
        }

        private void DrawCenterMessage(SpriteBatch spriteBatch, SpriteFont font, string msg)
        {
            var size = font.MeasureString(msg) * 0.6f;
            var rect = new Rectangle((int)(ViewportWidth / 2f - size.X / 2f - 12), ViewportHeight - 70, (int)size.X + 24, 30);
            spriteBatch.Draw(blankTexture, rect, new Color(0, 0, 0, 190));
            spriteBatch.DrawString(font, msg, new Vector2(rect.X + 12, rect.Y + 6), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        }
    }
}
