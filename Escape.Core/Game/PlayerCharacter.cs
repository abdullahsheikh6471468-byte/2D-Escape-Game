using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Escape.Core
{
    /// <summary>
    /// The player-controlled character: top-down movement, health, and key inventory.
    /// </summary>
    internal class PlayerCharacter
    {
        public const float Width = 40f;
        public const float Height = 40f;
        public const float Speed = 160f; // pixels per second

        public Vector2 Position;
        public int MaxHealth = 100;
        public int Health = 100;
        public bool IsAlive = true;

        public HashSet<string> Keys = new HashSet<string>();

        public float InvulnerabilitySeconds = 0f;
        private const float HitInvulnerabilityDuration = 0.75f;

        public Direction Facing = Direction.Down;
        public bool IsMoving = false;

        private Animation animDown;
        private Animation animUp;
        private Animation animSide;
        private AnimationPlayer sprite;

        public PlayerCharacter(Vector2 spawnPosition)
        {
            Position = spawnPosition;
        }

        public void LoadContent(ContentManager content)
        {
            animDown = new Animation(content.Load<Texture2D>("Characters/Player/down"), 0.2f, false);
            animUp = new Animation(content.Load<Texture2D>("Characters/Player/up"), 0.2f, false);
            animSide = new Animation(content.Load<Texture2D>("Characters/Player/side"), 0.2f, false);
        }

        public Rectangle Bounds => new Rectangle(
            (int)(Position.X - Width / 2f), (int)(Position.Y - Height / 2f), (int)Width, (int)Height);

        public void ResetForLevel(Vector2 spawnPosition)
        {
            Position = spawnPosition;
            Health = MaxHealth;
            IsAlive = true;
            Keys.Clear();
            InvulnerabilitySeconds = 0f;
        }

        public void TakeDamage(int amount)
        {
            if (InvulnerabilitySeconds > 0 || !IsAlive) return;

            Health -= amount;
            InvulnerabilitySeconds = HitInvulnerabilityDuration;

            if (Health <= 0)
            {
                Health = 0;
                IsAlive = false;
            }
        }

        public void Heal(int amount)
        {
            Health = Math.Min(MaxHealth, Health + amount);
        }

        public bool HasKey(string color) => Keys.Contains(color);

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (InvulnerabilitySeconds > 0) InvulnerabilitySeconds -= dt;
        }

        /// <summary>
        /// Draws the player at the given screen-space position (camera offset already applied).
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Vector2 screenPosition)
        {
            Animation anim = Facing switch
            {
                Direction.Down => animDown,
                Direction.Up => animUp,
                _ => animSide
            };

            SpriteEffects fx = Facing == Direction.Right ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Color tint = Color.White;
            if (InvulnerabilitySeconds > 0 && (int)(InvulnerabilitySeconds * 12) % 2 == 0)
                tint = new Color(255, 255, 255, 110);

            sprite.PlayAnimation(anim);
            sprite.Draw(gameTime, spriteBatch, screenPosition, fx, tint);
        }
    }
}
