using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Escape.Core
{
    /// <summary>
    /// Which uploaded enemy art set this enemy uses.
    /// </summary>
    internal enum EnemyKind { GoblinGreen, GoblinRed }

    /// <summary>
    /// A patrol -> detect -> chase -> return top-down enemy.
    /// </summary>
    internal class EnemyCharacter
    {
        public const float Width = 40f;
        public const float Height = 40f;

        public EnemyKind Kind;
        public Vector2 Position;
        public List<Vector2> PatrolPoints;
        public int CurrentPatrolIndex = 0;
        public float PatrolSpeed;
        public float ChaseSpeed;
        public float DetectionRange;
        public int Damage;
        public bool IsChasing = false;
        public Direction Facing = Direction.Down;

        public float AttackCooldown = 0f;
        private const float AttackCooldownDuration = 0.7f;

        private Animation animDown, animUp, animSide;
        private AnimationPlayer sprite;

        public EnemyCharacter(EnemyKind kind, Vector2 start, List<Vector2> patrolPoints,
            float patrolSpeed, float chaseSpeed, float detectionRange, int damage)
        {
            Kind = kind;
            Position = start;
            PatrolPoints = (patrolPoints != null && patrolPoints.Count > 0) ? patrolPoints : new List<Vector2> { start };
            PatrolSpeed = patrolSpeed;
            ChaseSpeed = chaseSpeed;
            DetectionRange = detectionRange;
            Damage = damage;
        }

        public void LoadContent(ContentManager content)
        {
            string folder = Kind == EnemyKind.GoblinGreen ? "Characters/GoblinGreen" : "Characters/GoblinRed";
            animDown = new Animation(content.Load<Texture2D>($"{folder}/down"), 0.2f, false);
            animUp = new Animation(content.Load<Texture2D>($"{folder}/up"), 0.2f, false);
            animSide = new Animation(content.Load<Texture2D>($"{folder}/side"), 0.2f, false);
        }

        public Rectangle Bounds => new Rectangle(
            (int)(Position.X - Width / 2f), (int)(Position.Y - Height / 2f), (int)Width, (int)Height);

        public bool CanAttack() => AttackCooldown <= 0f;
        public void RegisterAttack() => AttackCooldown = AttackCooldownDuration;

        public void Update(GameTime gameTime, Vector2 playerPos, bool playerAlive, Func<Rectangle, bool> isBlocked)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (AttackCooldown > 0) AttackCooldown -= dt;

            float distToPlayer = Vector2.Distance(Position, playerPos);

            if (playerAlive && distToPlayer <= DetectionRange)
                IsChasing = true;
            else if (IsChasing && distToPlayer > DetectionRange * 1.5f)
                IsChasing = false;

            Vector2 target = IsChasing ? playerPos : PatrolPoints[CurrentPatrolIndex];
            float speed = IsChasing ? ChaseSpeed : PatrolSpeed;

            Vector2 toTarget = target - Position;
            float dist = toTarget.Length();

            if (dist > 1f)
            {
                Vector2 dir = toTarget / dist;
                Vector2 move = dir * speed * dt;

                UpdateFacing(dir);

                var tryX = new Vector2(Position.X + move.X, Position.Y);
                if (!isBlocked(BoundsAt(tryX))) Position = tryX;

                var tryY = new Vector2(Position.X, Position.Y + move.Y);
                if (!isBlocked(BoundsAt(tryY))) Position = tryY;
            }

            if (!IsChasing && dist < 4f && PatrolPoints.Count > 1)
                CurrentPatrolIndex = (CurrentPatrolIndex + 1) % PatrolPoints.Count;
        }

        private void UpdateFacing(Vector2 dir)
        {
            if (Math.Abs(dir.X) > Math.Abs(dir.Y))
                Facing = dir.X > 0 ? Direction.Right : Direction.Left;
            else
                Facing = dir.Y > 0 ? Direction.Down : Direction.Up;
        }

        private Rectangle BoundsAt(Vector2 pos) => new Rectangle(
            (int)(pos.X - Width / 2f), (int)(pos.Y - Height / 2f), (int)Width, (int)Height);

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Vector2 screenPosition)
        {
            Animation anim = Facing switch
            {
                Direction.Down => animDown,
                Direction.Up => animUp,
                _ => animSide
            };
            SpriteEffects fx = Facing == Direction.Right ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Color tint = IsChasing ? new Color(255, 210, 200) : Color.White;

            sprite.PlayAnimation(anim);
            sprite.Draw(gameTime, spriteBatch, screenPosition, fx, tint);
        }
    }
}
