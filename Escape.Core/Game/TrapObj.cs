using Microsoft.Xna.Framework;

namespace Escape.Core
{
    internal enum TrapKind { Spike, MovingBlock, Laser, Fire }

    internal class TrapObj
    {
        public TrapKind Kind;
        public Rectangle HomeBounds;
        public Rectangle CurrentBounds;
        public int Damage;

        // Moving-block traps travel back and forth along one axis.
        public bool MovesHorizontally;
        public float TravelDistance;
        public float MoveSpeed; // pixels per second
        private float travelProgress = 0f;
        private int direction = 1;

        // Laser traps flip between active (dangerous) and inactive on a
        // fixed, learnable cycle.
        public int CycleLengthMs = 2200;
        public int ActiveLengthMs = 750;
        private int cycleTimerMs = 0;
        public bool IsActive = true;

        public float HitCooldown = 0f;
        private const float HitCooldownDuration = 0.8f;

        public TrapObj(TrapKind kind, Rectangle bounds, int damage)
        {
            Kind = kind;
            HomeBounds = bounds;
            CurrentBounds = bounds;
            Damage = damage;
            IsActive = kind != TrapKind.Laser; // lasers start on their timed cycle
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (HitCooldown > 0) HitCooldown -= dt;

            if (Kind == TrapKind.MovingBlock)
            {
                travelProgress += MoveSpeed * dt * direction;
                if (travelProgress >= TravelDistance || travelProgress <= 0)
                {
                    direction *= -1;
                    travelProgress = MathHelper.Clamp(travelProgress, 0, TravelDistance);
                }

                int offset = (int)travelProgress;
                CurrentBounds = MovesHorizontally
                    ? new Rectangle(HomeBounds.X + offset, HomeBounds.Y, HomeBounds.Width, HomeBounds.Height)
                    : new Rectangle(HomeBounds.X, HomeBounds.Y + offset, HomeBounds.Width, HomeBounds.Height);
            }
            else if (Kind == TrapKind.Laser)
            {
                cycleTimerMs = (cycleTimerMs + (int)gameTime.ElapsedGameTime.TotalMilliseconds) % CycleLengthMs;
                IsActive = cycleTimerMs < ActiveLengthMs;
            }
        }

        public bool IsDangerousNow => Kind == TrapKind.MovingBlock || Kind == TrapKind.Fire || IsActive;

        public void RegisterHit() => HitCooldown = HitCooldownDuration;
    }
}
